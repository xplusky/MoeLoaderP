using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// anime-pictures.net fixed 20200316
    /// </summary>
    public class AnimePicsSite : MoeSite
    {
        public override string HomeUrl => "https://anime-pictures.net";
        public override string DisplayName => "Anime-Pictures";

        public override string ShortName => "anime-pictures";

        private readonly string[] _user = { "mjvuser1" };
        private readonly string[] _pass = { "mjvpass" };
        private bool IsLogon { get; set; }

        public AnimePicsSite()
        {
            SupportState.IsSupportScore = true;
            SupportState.IsSupportRating = false;
            DownloadTypes.Add("原图", 4);
        }

        public async Task LoginAsync(CancellationToken token)
        {

            Net = new NetDocker(Settings, HomeUrl);
            Net.SetTimeOut(20);
            var index = new Random().Next(0, _user.Length);
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"login",_user[index] },
                {"password",_pass[index] }
            });
            var respose = await Net.Client.PostAsync($"{HomeUrl}/login/submit", content, token); // http://mjv-art.org/login/submit
            if (respose.IsSuccessStatusCode)
            {
                IsLogon = true;
            }
            else
            {
                Extend.Log("https://anime-pictures.net 网站登陆失败");
                return;
            }
        }

        public NetDocker AutoHintNet { get; set; }

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            if (!IsLogon) await LoginAsync(token);

            // pages source
            //http://mjv-art.org/pictures/view_posts/0?lang=en
            var url = $"{HomeUrl}/pictures/view_posts/{para.PageIndex - 1}?lang=en";

            if (para.Keyword.Length > 0)
            {
                //http://mjv-art.org/pictures/view_posts/0?search_tag=suzumiya%20haruhi&order_by=date&ldate=0&lang=en
                url = $"{HomeUrl}/pictures/view_posts/{para.PageIndex - 1}?search_tag={para.Keyword}&order_by=date&ldate=0&lang=en";
            }

            var imgs = new MoeItems();

            var doc = await Net.GetHtmlAsync(url, token);
            if (doc == null) return null;
            var pre = "https:";
            var listnode = doc.DocumentNode.SelectNodes("//*[@id='posts']/div[@class='posts_block']/span[@class='img_block_big']");
            if (listnode == null) return new MoeItems{Message = "读取HTML失败"};
            foreach (var node in listnode)
            {
                var img = new MoeItem(this, para) {Site = this};
                var imgnode = node.SelectSingleNode("a/picture/img");
                var idattr = imgnode.GetAttributeValue("id", "0");
                var reg = Regex.Replace(idattr, @"[^0-9]+", "");
                img.Id = reg.ToInt();
                var src = imgnode.GetAttributeValue("src", "");
                if (!src.IsNaN()) img.Urls.Add(new UrlInfo( 1, $"{pre}{src}", url));
                var resstrs = node.SelectSingleNode("div[@class='img_block_text']/a")?.InnerText.Trim().Split('x');
                if (resstrs?.Length == 2)
                {
                    img.Width = resstrs[0].ToInt();
                    img.Height = resstrs[1].ToInt();
                }
                var scorestr = node.SelectSingleNode("div[@class='img_block_text']/span")?.InnerText.Trim();
                var match = Regex.Replace(scorestr ?? "0", @"[^0-9]+", "");
                img.Score = match.ToInt();
                var detail = node.SelectSingleNode("a").GetAttributeValue("href", "");
                if (!detail.IsNaN())
                {
                    img.DetailUrl = $"{HomeUrl}{detail}";
                    img.GetDetailTaskFunc = async () => await GetDetailTask(img);
                }
                imgs.Add(img);
            }
            token.ThrowIfCancellationRequested();
            return imgs;
        }

        public async Task GetDetailTask(MoeItem img)
        {
            var detialurl = img.DetailUrl;
            var net = Net.CloneWithOldCookie();
            net.SetTimeOut(30);
            try
            {
                var subdoc = await net.GetHtmlAsync(detialurl);
                var docnodes = subdoc.DocumentNode;
                if (docnodes == null) return;
                var downnode = docnodes.SelectSingleNode("//*[@id='rating']/a[@class='download_icon']");
                var fileurl = downnode?.GetAttributeValue("href", "");
                if (!fileurl.IsNaN()) img.Urls.Add( 4, $"{HomeUrl}{fileurl}", detialurl);
                var tagnodes = docnodes.SelectNodes("*//div[@id='post_tags']//a");
                if (tagnodes != null)
                {
                    foreach (var tagnode in tagnodes)
                    {
                        if (!tagnode.InnerText.IsNaN()) img.Tags.Add(tagnode.InnerText);
                    }
                }
            }
            catch (Exception e)
            {
                Extend.Log(e, e.StackTrace);
            }
        }

        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            if (AutoHintNet == null) AutoHintNet = new NetDocker(Settings, HomeUrl);
            AutoHintNet.SetReferer($"{HomeUrl}/?lang=zh_CN");
            //AutoHintNet.Client.DefaultRequestHeaders.Add("content-type", "multipart/form-data; boundary=----WebKitFormBoundaryzFqgWZTqudUG0vBb");
            var re = new AutoHintItems();
            var mulform = new MultipartFormDataContent("----WebKitFormBoundaryzFqgWZTqudUG0vBb");
            var content = new FormUrlEncodedContent(new Pairs
            {
                {"tag",para.Keyword.Trim() }
            });
            mulform.Add(content);
            var url = $"{HomeUrl}/pictures/autocomplete_tag";
            var response = await AutoHintNet.Client.PostAsync(url, mulform, token);

            // todo 这里post数据获取失败，希望有大神能够解决
            if (!response.IsSuccessStatusCode) return new AutoHintItems();
            var txt = await response.Content.ReadAsStringAsync();
            //JSON format response

            dynamic json = JsonConvert.DeserializeObject(txt);
            dynamic list = ((JProperty)json).Value;
            foreach (var item in list)
            {
                re.Add(new AutoHintItem
                {
                    Word = $"{item.t}".Replace("<br>", "").Replace("</br>", ""),
                    Count = $"{item.c}"
                });
            }

            return re;
        }
    }
}
