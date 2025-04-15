using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    private readonly string[] _pass = ["mjvpass"];

    private readonly string[] _user = ["mjvuser1"];

    public AnimePicturesSite()
    {
        DownloadTypes.Add("原图", DownloadTypeEnum.Origin);

        Config.IsSupportKeyword = true;
        Config.IsSupportRating = true;
        Config.IsSupportScore = true;
        Config.IsSupportAccount = true;
        LoginPageUrl = "https://anime-pictures.net/login";
    }
    public override bool VerifyCookie(CookieCollection ccol)
    {
        return ccol.Any(cookie => cookie.Name.Equals("anime_pictures_jwt", StringComparison.OrdinalIgnoreCase));
    }
    public override string HomeUrl => "https://anime-pictures.net";

    public string Api => "https://api.anime-pictures.net/api/v3";
    public override string DisplayName => "Anime-Pictures";

    public override string ShortName => "anime-pictures";
    private bool IsLogon { get; set; }

    public NetOperator AutoHintNet { get; set; }

    //public async Task LoginAsync(CancellationToken token)
    //{
    //    Net = new NetOperator(Settings,this);
    //    Net.SetTimeOut(20);

    //    Ex.ShowMessage("Anime-Pictures 正在自动登录中……", null, Ex.MessagePos.Searching);
    //    var index = new Random().Next(0, _user.Length);
    //    var content = new FormUrlEncodedContent(new Dictionary<string, string>
    //    {
    //        {"login", _user[index]},
    //        {"password", _pass[index]}
    //    });
    //    var respose = await Net.Client.PostAsync($"{HomeUrl}/login/submit", content, token);
    //    if (respose.IsSuccessStatusCode) IsLogon = true;
    //    else Ex.Log("https://anime-pictures.net 网站登陆失败");
    //    await Task.Delay(1000, token);
    //}

    public override async Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
    {
        if (Net == null)
        {
            Net = new NetOperator(Settings, this);
            Net.SetCookie(SiteSettings.GetCookieContainer());
        }

        var net = Net.CloneWithCookie();
        net.SetTimeOut(40);
        // pages source

        var url = "";
        if (para.Keyword.Length > 0)
        {

        }
        else
        {
            url = $"https://api.anime-pictures.net/api/v3/posts?page={para.PageIndex - 1}&order_by=date&ldate=0&lang=zh-cn";

        }

        var imgs = new SearchedPage();

        var jsonlist = await Net.GetJsonAsync(url, token: token);
        if (jsonlist == null) return null;
        
        foreach (var jitem in jsonlist.posts)
        {
            var img = new MoeItem(this, para);
            img.OriginString = jitem.ToString();

            var md5 = $"{jitem.md5}";
            // id
            img.Id= $"{jitem.id}".ToInt();

            // thumb
            var md5q3 = md5.Substring(0, 3);
            var thumbName = $"https://opreviews.anime-pictures.net/{md5q3}/{md5}_cp{jitem.ext}";
            img.Urls.Add(new UrlInfo(DownloadTypeEnum.Thumbnail, thumbName, url));

            // resolution
            img.Width = $"{jitem.width}".ToInt();
            img.Height = $"{jitem.height}".ToInt();


            // score
            img.Score = $"{jitem.score}".ToInt();

            img.Date = $"{jitem.datetime}".ToDateTime();


            // detailurl
            //var detail = node.SelectSingleNode("a").GetAttributeValue("href", "");
            //if (!detail.IsEmpty())
            //{
            //    img.DetailUrl = $"{HomeUrl}{detail}";
            //    img.GetDetailTaskFunc = async t => await GetDetailTask(img, t);
            //}

            // dl
            var detail = $"https://anime-pictures.net/posts/{jitem.id}";
            var pmd5 = $"{jitem.md5_pixels}";
            var pmd5q3 = pmd5.Substring(0, 3);
            img.Urls.Add(DownloadTypeEnum.Origin, $"https://oimages.anime-pictures.net/{md5q3}/{md5}{jitem.ext}", detail);

            imgs.Add(img);
        }




        //? $"{HomeUrl}/pictures/view_posts/{para.PageIndex - 1}?search_tag={para.Keyword}&order_by=date&ldate=0&lang=en" 
        //: $"{HomeUrl}/posts?page={para.PageIndex - 1}&order_by=date&ldate=0&lang=zh-cn";


        //var imgs = new SearchedPage();

        //var doc = await Net.GetHtmlAsync(url, token: token);
        //if (doc == null) return null;
        //var pre = "https:";
        //var listnode = doc.DocumentNode.SelectNodes("//span[contains(@class,'img-block')]");
        //if (listnode == null)
        //{
        //    return new SearchedPage {Message = "读取HTML失败"};
        //}
        //foreach (var node in listnode)
        //{
        //    var img = new MoeItem(this, para);
        //    // id
        //    var idnode = node.SelectSingleNode("a");
        //    var idstrorg = idnode.GetAttributeValue("href", string.Empty);
        //    var idstrMatch = Regex.Match(idstrorg, @"\d+");
        //    int.TryParse(idstrMatch.Value, out var id);
        //    if(id!=0)  img.Id = id;

        //    // thumb
        //    var thumbnode = node.SelectSingleNode("a/picture/img");
        //    var thumb = thumbnode.GetAttributeValue("src", string.Empty);
        //    if (!thumb.IsEmpty()) img.Urls.Add(new UrlInfo(DownloadTypeEnum.Thumbnail, $"{thumb}", url));

        //    // resolution
        //    var resstrs = node.GetInnerText("div/a").Split('x');
        //    if (resstrs.Length == 2)
        //    {
        //        img.Width = resstrs[0].ToInt();
        //        img.Height = resstrs[1].ToInt();
        //    }


        //    // score
        //    var scorestr = node.GetInnerText("div/span");
        //    var match = Regex.Replace(scorestr ?? "0", "[^0-9]+", "");
        //    img.Score = match.ToInt();

        //    // detailurl
        //    var detail = node.SelectSingleNode("a").GetAttributeValue("href", "");
        //    if (!detail.IsEmpty())
        //    {
        //        img.DetailUrl = $"{HomeUrl}{detail}";
        //        img.GetDetailTaskFunc = async t => await GetDetailTask(img, t);
        //    }

        //    imgs.Add(img);
        //}

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

            img.OriginString = subdoc.DocumentNode.OuterHtml;
            var docnodes = subdoc.DocumentNode;
            if (docnodes == null) return;
            var downnode = docnodes.SelectSingleNode("//a[contains(@class,'svelte-11znzur')]");
            var fileurl = downnode?.GetAttributeValue("href", "");
            string finalUrl = "";
            if (!fileurl.IsEmpty())
            {
                try
                {
                    // 创建一个自定义的 HttpClientHandler
                    HttpClientHandler handler = new HttpClientHandler
                    {
                        // 设置最大重定向次数
                        MaxAutomaticRedirections = 2,
                        // 允许自动重定向
                        AllowAutoRedirect = true
                    };

                    var net2 = Net.CloneWithCookie();
                    net2.HttpClientHandler.AllowAutoRedirect = true;
                    net2.SetReferer(fileurl);
                    net2.HttpClientHandler.MaxAutomaticRedirections = 2;
                    
                    // 发送 GET 请求
                    HttpResponseMessage response = await net2.GetAsync(fileurl, token: token);

                    // 获取最终的 URL
                    finalUrl = response.RequestMessage.RequestUri.AbsoluteUri;

                    Console.WriteLine($"原始 URL: {fileurl}");
                    Console.WriteLine($"最终 URL: {finalUrl}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发生错误: {ex.Message}");
                }

                if (!finalUrl.IsEmpty())
                {
                    img.Urls.Add(DownloadTypeEnum.Origin, $"{finalUrl}", detialurl);
                }
                
            }
            var tagnodes = docnodes.SelectNodes("*//div[@id='post_tags']//a");
            if (tagnodes != null)
            {
                foreach (var tagnode in tagnodes)
                {
                    if (!tagnode.InnerText.IsEmpty())
                    {
                        img.Tags.Add(tagnode.InnerText);
                    }

                }
            }
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