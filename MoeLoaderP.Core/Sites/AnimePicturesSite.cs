using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     anime-pictures.net
/// </summary>
public class AnimePicturesSite : MoeSite
{
    private readonly string[] _pass = {"mjvpass"};

    private readonly string[] _user = {"mjvuser1"};

    public AnimePicturesSite()
    {
        DownloadTypes.Add("原图", DownloadTypeEnum.Origin);

        Config.IsSupportKeyword = true;
        Config.IsSupportRating = true;
        Config.IsSupportScore = true;
    }

    public override string HomeUrl => "https://anime-pictures.net";
    public override string DisplayName => "Anime-Pictures";

    public override string ShortName => "anime-pictures";
    private bool IsLogon { get; set; }

    public NetOperator AutoHintNet { get; set; }

    public async Task LoginAsync(CancellationToken token)
    {
        Net = new NetOperator(Settings,this);
        Net.SetTimeOut(20);
        var index = new Random().Next(0, _user.Length);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"login", _user[index]},
            {"password", _pass[index]}
        });
        var respose =
            await Net.Client.PostAsync($"{HomeUrl}/login/submit", content, token); // http://mjv-art.org/login/submit
        if (respose.IsSuccessStatusCode)
            IsLogon = true;
        else
            Ex.Log("https://anime-pictures.net 网站登陆失败");
    }

    public override async Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
    {
        if (!IsLogon) await LoginAsync(token);

        // pages source
        //http://mjv-art.org/pictures/view_posts/0?lang=en
        var url = $"{HomeUrl}/pictures/view_posts/{para.PageIndex - 1}?lang=en";

        if (para.Keyword.Length > 0)
            //http://mjv-art.org/pictures/view_posts/0?search_tag=suzumiya%20haruhi&order_by=date&ldate=0&lang=en
            url =
                $"{HomeUrl}/pictures/view_posts/{para.PageIndex - 1}?search_tag={para.Keyword}&order_by=date&ldate=0&lang=en";

        var imgs = new SearchedPage();

        var doc = await Net.GetHtmlAsync(url, token: token);
        if (doc == null) return null;
        var pre = "https:";
        var listnode =
            doc.DocumentNode.SelectNodes("//*[@id='posts']/div[@class='posts_block']/span[@class='img_block_big']");
        if (listnode == null) return new SearchedPage {Message = "读取HTML失败"};
        foreach (var node in listnode)
        {
            var img = new MoeItem(this, para);
            var imgnode = node.SelectSingleNode("a/picture/img");
            var idattr = imgnode.GetAttributeValue("id", "0");
            var reg = Regex.Replace(idattr, @"[^0-9]+", "");
            img.Id = reg.ToInt();
            var src = imgnode.GetAttributeValue("src", "");
            if (!src.IsEmpty()) img.Urls.Add(new UrlInfo(DownloadTypeEnum.Thumbnail, $"{pre}{src}", url));
            var resstrs = node.GetInnerText("div[@class = 'img_block_text'] / a").Split('x');
            if (resstrs.Length == 2)
            {
                img.Width = resstrs[0].ToInt();
                img.Height = resstrs[1].ToInt();
            }

            var scorestr = node.GetInnerText("div[@class='img_block_text']/span");
            var match = Regex.Replace(scorestr ?? "0", @"[^0-9]+", "");
            img.Score = match.ToInt();
            var detail = node.SelectSingleNode("a").GetAttributeValue("href", "");
            if (!detail.IsEmpty())
            {
                img.DetailUrl = $"{HomeUrl}{detail}";
                img.GetDetailTaskFunc = async t => await GetDetailTask(img, t);
            }

            imgs.Add(img);
        }

        token.ThrowIfCancellationRequested();
        return imgs;
    }

    public async Task GetDetailTask(MoeItem img, CancellationToken token)
    {
        var detialurl = img.DetailUrl;
        var net = GetCloneNet(timeout: 30);
        try
        {
            var subdoc = await net.GetHtmlAsync(detialurl, null, false, token);
            var docnodes = subdoc.DocumentNode;
            if (docnodes == null) return;
            var downnode = docnodes.SelectSingleNode("//*[@id='rating']/a[@class='download_icon']");
            var fileurl = downnode?.GetAttributeValue("href", "");
            if (!fileurl.IsEmpty()) img.Urls.Add(DownloadTypeEnum.Origin, $"{HomeUrl}{fileurl}", detialurl);
            var tagnodes = docnodes.SelectNodes("*//div[@id='post_tags']//a");
            if (tagnodes != null)
                foreach (var tagnode in tagnodes)
                    if (!tagnode.InnerText.IsEmpty())
                        img.Tags.Add(tagnode.InnerText);
        }
        catch (Exception e)
        {
            Ex.Log(e, e.StackTrace);
        }
    }

    public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
    {
        if (AutoHintNet == null)
        {
            AutoHintNet = new NetOperator(Settings, this);
            AutoHintNet.SetReferer($"{HomeUrl}");
        }

        var re = new AutoHintItems();
        var pairs = new Pairs
        {
            {"tag", para.Keyword.Trim()}
        };
        var content = new FormUrlEncodedContent(pairs);
        var url = $"{HomeUrl}/pictures/autocomplete_tag";
        var response = await AutoHintNet.Client.PostAsync(url, content, token);

        if (!response.IsSuccessStatusCode) return new AutoHintItems();
        var txt = await response.Content.ReadAsStringAsync(token);
        dynamic json = JsonConvert.DeserializeObject(txt);
        var list = json?.tags_list;
        foreach (var item in Ex.GetList(list))
        {
            var i = new AutoHintItem
            {
                Word = $"{item.t}".Delete("<b>", "</b>"),
                Count = $"{item.c}"
            };

            re.Add(i);
        }

        return re;
    }
}