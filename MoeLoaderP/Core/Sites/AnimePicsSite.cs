using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// anime-pictures.net fixed 20180923
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
            SurpportState.IsSupportScore = false;
            SurpportState.IsSupportRating = false;
            DownloadTypes.Add("原图", 4);
        }

        public async Task LoginAsync(CancellationToken token)
        {
            
            Net = new NetSwap(Settings, HomeUrl);
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
                App.Log("https://anime-pictures.net 网站登陆失败");
                return;
            }
        }

        public NetSwap AutoHintNet { get; set; }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            if (!IsLogon)
            {
                await LoginAsync(token);
            }

            // pages source
            //http://mjv-art.org/pictures/view_posts/0?lang=en
            var url = $"{HomeUrl}/pictures/view_posts/{para.PageIndex - 1}?lang=en";

            if (para.Keyword.Length > 0)
            {
                //http://mjv-art.org/pictures/view_posts/0?search_tag=suzumiya%20haruhi&order_by=date&ldate=0&lang=en
                url = $"{HomeUrl}/pictures/view_posts/{para.PageIndex - 1}?search_tag={para.Keyword}&order_by=date&ldate=0&lang=en";
            }
            var pageres = await Net.Client.GetAsync(url,token);
            var pageString = await pageres.Content.ReadAsStringAsync();

            // images
            var  imgs = new ImageItems();

            var doc = new HtmlDocument();
            doc.LoadHtml(pageString);
            var pre = "https:";
            var listnode = doc.DocumentNode.SelectNodes("//*[@id='posts']/div[@class='posts_block']/span[@class='img_block_big']");
            if (listnode == null) return imgs;
            foreach (var node in listnode)
            {
                var img = new ImageItem(this,para);
                //img.Net = Net;
                img.Site = this;
                var imgnode = node.SelectSingleNode("a/picture/img");
                var idattr = imgnode.GetAttributeValue("id", "0");
                var reg = Regex.Replace(idattr, @"[^0-9]+", "");
                int.TryParse(reg, out var id);
                img.Id = id;
                var src = imgnode.GetAttributeValue("src", "");
                if (!string.IsNullOrWhiteSpace(src))
                {
                    img.Urls.Add(new UrlInfo("缩略图",1, $"{pre}{src}", $"{HomeUrl}/pictures/view_posts/"));
                }
                var resstrs = node.SelectSingleNode("div[@class='img_block_text']/a")?.InnerText.Trim().Split('x');
                int.TryParse(resstrs[0], out var width);
                int.TryParse(resstrs[1], out var height);
                img.Width = width;
                img.Height = height;
                var scorestr = node.SelectSingleNode("div[@class='img_block_text']/span")?.InnerText.Trim();
                int.TryParse(Regex.Match(scorestr??"0", @"[^0-9]+").Value, out var score);
                img.Score = score;
                var detail = node.SelectSingleNode("a").GetAttributeValue("href", "");
                if (!string.IsNullOrWhiteSpace(detail))
                {
                    img.DetailUrl = $"{HomeUrl}{detail}";
                    img.GetDetailAction = async () =>
                    {
                        var detialurl = img.DetailUrl;
                        var net = new NetSwap(Settings);
                        net.SetTimeOut(25);
                        try
                        {
                            var detailPageStr = await net.Client.GetStringAsync(detialurl);
                            var subdoc = new HtmlDocument();
                            subdoc.LoadHtml(detailPageStr);
                            var downnode = subdoc.DocumentNode?.SelectSingleNode("//*[@id='rating']/a[@class='download_icon']");
                            var fileurl = downnode?.GetAttributeValue("href", "");
                            if (!string.IsNullOrWhiteSpace(fileurl))
                            {
                                img.Urls.Add(new UrlInfo("原图", 4, $"{HomeUrl}{fileurl}", detialurl));
                            }
                        }
                        catch (Exception e)
                        {
                            App.Log(e);
                        }
                    };
                }

                
                imgs.Add(img);
            }
            token.ThrowIfCancellationRequested();
            return imgs;
        }

        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            if(AutoHintNet==null)AutoHintNet = new NetSwap(Settings,HomeUrl);
            AutoHintNet.SetReferer(HomeUrl);
            //http://mjv-art.org/pictures/autocomplete_tag POST
            var re = new AutoHintItems();
            //no result with length less than 3
            if (para.Keyword.Length < 3) return re;
            var mcontent = new MultipartFormDataContent("---------------" + DateTime.Now.Ticks.ToString("x"));
            mcontent.Add(new StringContent($"Content-Disposition: form-data; name=\"tag\"\r\n\r\n{para.Keyword}"));
            var url = $"{HomeUrl}/pictures/autocomplete_tag";
            var response = await AutoHintNet.Client.PostAsync(url, mcontent, token);
            // todo 这里post数据失败，希望有大神能够解决
            if (!response.IsSuccessStatusCode) return new AutoHintItems();
            
            var txt = await response.Content.ReadAsStringAsync();
            //JSON format response

            //{"tags_list": [{"c": 3, "t": "suzumiya <b>haruhi</b> no yuutsu"}, {"c": 1, "t": "suzumiya <b>haruhi</b>"}]}
            var tagList = ((new System.Web.Script.Serialization.JavaScriptSerializer()).DeserializeObject(txt) as Dictionary<string, object>)?["tags_list"] as object[];
            for (var i = 0; i < tagList.Length && i < 8; i++)
            {
                var tag = tagList[i] as Dictionary<string, object>;
                if (tag["t"].ToString().Trim().Length > 0)
                {
                    re.Add(new AutoHintItem
                    {
                        Word = tag["t"].ToString().Trim().Replace("<b>", "").Replace("</b>", ""),
                        Count = "N/A"
                    });
                }
            }

            return re;
        }
    }
}
