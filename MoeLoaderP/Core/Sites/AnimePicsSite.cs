using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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

        public override string ShortName => "mjv-art";

        private readonly string[] _user = { "mjvuser1" };
        private readonly string[] _pass = { "mjvpass" };
        private bool IsLogon { get; set; }

        public AnimePicsSite()
        {
            SurpportState.IsSupportScore = false;
        }

        public async Task LoginAsync()
        {
            
            Net = new NetSwap(Settings, HomeUrl);
            Net.SetTimeOut(20);
            var index = new Random().Next(0, _user.Length);
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"login",_user[index] },
                {"password",_pass[index] }
            });
            var respose = await Net.Client.PostAsync($"{HomeUrl}/login/submit", content); // http://mjv-art.org/login/submit
            if (respose.IsSuccessStatusCode)
            {
                IsLogon = true;
            }
            else
            {
                App.Log("https://anime-pictures.net 登陆失败");
                throw new Exception("网站登陆失败");
            }
        }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para)
        {
            if (!IsLogon)
            {
                await LoginAsync();
            }

            // pages source
            //http://mjv-art.org/pictures/view_posts/0?lang=en
            var url = $"{HomeUrl}/pictures/view_posts/{para.PageIndex - 1}?lang=en";

            if (para.Keyword.Length > 0)
            {
                //http://mjv-art.org/pictures/view_posts/0?search_tag=suzumiya%20haruhi&order_by=date&ldate=0&lang=en
                url = $"{HomeUrl}/pictures/view_posts/{para.PageIndex - 1}?search_tag={para.Keyword}&order_by=date&ldate=0&lang=en";
            }
            
            var pageString = await Net.Client.GetStringAsync(url);

            // images
            var  imgs = new ImageItems();

            var doc = new HtmlDocument();
            doc.LoadHtml(pageString);
            var pre = "https:";
            var listnode = doc.DocumentNode.SelectNodes("//*[@id='posts']/div[@class='posts_block']/span[@class='img_block_big']");
            if (listnode == null) return imgs;
            foreach (var node in listnode)
            {
                var img = new ImageItem();
                //img.Net = Net;
                img.Site = this;
                var imgnode = node.SelectSingleNode("a/picture/img");
                var idattr = imgnode.GetAttributeValue("id", "0");
                int.TryParse(Regex.Match(idattr, @"[^0-9]+").Value, out var id);
                img.Id = id;
                var src = imgnode.GetAttributeValue("src", "");
                if (!string.IsNullOrWhiteSpace(src)) img.ThumbnailUrl = $"{pre}{src}";
                var resstrs = node.SelectSingleNode("div[@class='img_block_text']/a")?.InnerText.Trim().Split('x');
                int.TryParse(resstrs[0], out var width);
                int.TryParse(resstrs[1], out var height);
                img.Width = width;
                img.Height = height;
                img.ThumbnailReferer = "https://anime-pictures.net/pictures/view_posts/";
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
                                img.FileUrl = $"{HomeUrl}{fileurl}";
                                img.FileReferer = detialurl;
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

            return imgs;
        }

        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            if (!IsLogon)
            {
                await LoginAsync();
            }
            //http://mjv-art.org/pictures/autocomplete_tag POST
            var re = new AutoHintItems();
            //no result with length less than 3
            if (para.Keyword.Length < 3) return re;

            var url = HomeUrl + "/pictures/autocomplete_tag";
            var txt = await Net.Client.GetStringAsync(url);

            //JSON format response
            //{"tags_list": [{"c": 3, "t": "suzumiya <b>haruhi</b> no yuutsu"}, {"c": 1, "t": "suzumiya <b>haruhi</b>"}]}
            var tagList = ((new System.Web.Script.Serialization.JavaScriptSerializer()).DeserializeObject(txt) as Dictionary<string, object>)?["tags_list"] as object[];
            for (var i = 0; i < tagList.Length && i < 8; i++)
            {
                var tag = tagList[i] as Dictionary<string, object>;
                if (tag["t"].ToString().Trim().Length > 0)
                    re.Add(new AutoHintItem() { Word = tag["t"].ToString().Trim().Replace("<b>", "").Replace("</b>", ""), Count = "N/A" });
            }

            return re;
        }

        private ImageItem GenerateImg(string detailUrl, string sampleUrl, string dimension, string tags, string id)
        {
            var intId = int.Parse(id);

            int width = 0, height = 0;
            try
            {
                //706x1000
                width = int.Parse(dimension.Substring(0, dimension.IndexOf('x')));
                height = int.Parse(dimension.Substring(dimension.IndexOf('x') + 1));
            }
            catch { }

            //convert relative url to absolute
            detailUrl = FormattedImgUrl(HomeUrl, detailUrl);
            sampleUrl = FormattedImgUrl(HomeUrl, sampleUrl);

            var img = new ImageItem()
            {
                //Date = "N/A",
                //FileSize = file_size.ToUpper(),
                //Desc = tags,
                Id = intId,
                //JpegUrl = preview_url,
                //OriginalUrl = preview_url,
                //PreviewUrl = preview_url,
                ThumbnailUrl = sampleUrl,
                //Score = 0,
                Width = width,
                Height = height,
                //Tags = tags,
                DetailUrl = detailUrl,
            };

            img.GetDetailAction = async () =>
            {
                var i = img;
                var p = Settings.Proxy;
                //retrieve details
                
                var response = await Net.Client.GetAsync(i.DetailUrl);
                if(!response.IsSuccessStatusCode) return;
                var page = await response.Content.ReadAsStringAsync();
                //<b>Size:</b> 326.0KB<br>
                var index = page.IndexOf("<b>Size");
                var fileSize = page.Substring(index + 12, page.IndexOf('<', index + 12) - index - 12).Trim();
                //<b>Date Published:</b> 2/24/12 4:57 PM
                index = page.IndexOf("<b>Date Published");
                var date = page.Substring(index + 22, page.IndexOf('<', index + 22) - index - 22).Trim();

                var doc = new HtmlDocument();
                doc.LoadHtml(page);
                //retrieve rating node
                var ratnode = doc.DocumentNode.SelectSingleNode("//span[@id='rating']");
                int.TryParse(ratnode?.SelectSingleNode("//*[@id='score_n']")?.InnerText, out var score);
                i.Score = score;

                i.FileUrl = FormattedImgUrl(HomeUrl, ratnode?.SelectSingleNode("a")?.Attributes["href"].Value);

                //retrieve img node
                var imgnode = doc.DocumentNode.SelectSingleNode("//div[@id='big_preview_cont']");
                var jpgUrl = FormattedImgUrl(HomeUrl, imgnode.SelectSingleNode("a").Attributes["href"].Value);
                var previewUrl = FormattedImgUrl(HomeUrl, imgnode.SelectSingleNode("a/picture/source/img")?.Attributes["src"].Value);

                // todo i.TagsText = imgnode.SelectSingleNode("a/picture/source/img")?.Attributes["alt"].Value;
                //var sb = new StringBuilder(i.TagsText);
                //sb.Replace("\n", " ");
                //sb.Replace("\t", " ");
                //var rx = new Regex("Anime.*with");
                //if (rx.IsMatch(sb.ToString()))
                //    i.TagsText = rx.Replace(sb.ToString(), "").Trim();

                try
                {
                    i.Author = doc.DocumentNode.SelectSingleNode("//div[@id='cont']/div[2]/div[1]/div[1]/a/span")?.InnerText;
                }
                catch
                {
                    try
                    {
                        i.Author = doc.DocumentNode.SelectSingleNode("//div[@id='cont']/div[2]/div[1]/a[1]")?.InnerText;
                    }
                    catch { }
                }

                // i.Description = i.TagsText;
                i.Date = date;
                i.FileSize = fileSize;
                i.JpegUrl = jpgUrl;
                i.PreviewUrl = previewUrl;
            };
            img.Site = this;
            img.FileReferer = HomeUrl;
            return img;
        }

        /// <summary>
        /// 图片地址格式化
        /// 2016年12月对带域名型地址格式化
        /// by YIU
        /// </summary>
        /// <param name="prHost">图站域名</param>
        /// <param name="prUrl">预处理的URL</param>
        /// <returns>处理后的图片URL</returns>
        private static string FormattedImgUrl(string prHost, string prUrl)
        {
            try
            {
                var po = prHost.IndexOf("//");
                var phh = prHost.Substring(0, prHost.IndexOf(':') + 1);
                var phu = prHost.Substring(po, prHost.Length - po);

                //地址中有主域名 去掉主域名
                if (prUrl.StartsWith(phu))
                    return prHost + prUrl.Replace(phu, "");

                //地址中有子域名 补完子域名
                else if (prUrl.StartsWith("//"))
                    return phh + prUrl;

                //地址没有域名 补完地址
                else if (prUrl.StartsWith("/"))
                    return prHost + prUrl;

                return prUrl;
            }
            catch
            {
                return prUrl;
            }
        }
    }
}
