using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MoeLoader.Core.Sites;

namespace MoeLoader.Core.Site
{
    public class SiteMjvArt : MoeSite
    {
        public override string HomeUrl => "https://anime-pictures.net";
        public override string DisplayName => "anime-pictures.net";

        public override string ShortName => "mjv-art";
        //public string Referer { get { return null; } }
        
        //public bool IsSupportRes { get { return true; } }
        //public bool IsSupportPreview { get { return true; } }
        //public bool IsSupportTag { get { return true; } }
        
        private string[] user = { "mjvuser1" };
        private string[] pass = { "mjvpass" };
        private string sessionId;
        private Random rand = new Random();

        /// <summary>
        /// mjv-art.org site
        /// </summary>
        public SiteMjvArt()
        {
            SurpportState.IsSupportScore = false;
        }

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            Login(proxy);

            //http://mjv-art.org/pictures/view_posts/0?lang=en
            string url = HomeUrl + "/pictures/view_posts/" + (page - 1) + "?lang=en";

            MyWebClient web = new MyWebClient();
            web.Proxy = proxy;
            web.Headers["Cookie"] = sessionId;
            web.Encoding = Encoding.UTF8;

            if (keyWord.Length > 0)
            {
                //http://mjv-art.org/pictures/view_posts/0?search_tag=suzumiya%20haruhi&order_by=date&ldate=0&lang=en
                url = HomeUrl + "/pictures/view_posts/" + (page - 1) + "?search_tag=" + keyWord + "&order_by=date&ldate=0&lang=en";
            }

            string pageString = web.DownloadString(url);
            web.Dispose();

            return pageString;
        }

        public override List<ImageItem> GetImages(string pageString, System.Net.IWebProxy proxy)
        {
            List<ImageItem> imgs = new List<ImageItem>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageString);
            //retrieve all elements via xpath
            HtmlNodeCollection nodes = doc.DocumentNode.SelectSingleNode("//div[@id='posts']").SelectNodes(".//span[@class='img_block_big']");
            if (nodes == null)
            {
                return imgs;
            }

            foreach (HtmlNode imgNode in nodes)
            {
                HtmlNode anode = imgNode.SelectSingleNode("a");
                //details will be extracted from here
                //eg. http://mjv-art.org/pictures/view_post/181876?lang=en
                string detailUrl = anode.Attributes["href"].Value;
                //eg. Anime picture 2000x3246 withblack hair,brown eyes
                string title = anode.Attributes["title"].Value;
                string sampleUrl = anode.SelectSingleNode("picture/source/img").Attributes["src"].Value;

                //extract id from detail url
                string id = Regex.Match(detailUrl.Substring(detailUrl.LastIndexOf('/') + 1), @"\d+").Value;
                int index = Regex.Match(title, @"\d+").Index;

                string dimension = title.Substring(index);
                string tags = "";
                //if (title.IndexOf(' ', index) > -1)
                //{
                //dimension = title.Substring(index, title.IndexOf(' ', index) - index);
                //tags = title.Substring(title.IndexOf(' ', index) + 1);
                //}

                ImageItem img = GenerateImg(detailUrl, sampleUrl, dimension, tags.Trim(), id);
                if (img != null) imgs.Add(img);
            }

            return imgs;
        }
        

        public override List<AutoHintItem> GetAutoHintItems(string word, System.Net.IWebProxy proxy)
        {
            //http://mjv-art.org/pictures/autocomplete_tag POST
            List<AutoHintItem> re = new List<AutoHintItem>();
            //no result with length less than 3
            if (word.Length < 3) return re;

            string url = HomeUrl + "/pictures/autocomplete_tag";
            System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            req.UserAgent = MoeSession.DefUA;
            req.Proxy = proxy;
            req.Headers["Cookie"] = sessionId;
            req.Timeout = 8000;
            req.Method = "POST";

            byte[] buf = Encoding.UTF8.GetBytes("tag=" + word);
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = buf.Length;
            System.IO.Stream str = req.GetRequestStream();
            str.Write(buf, 0, buf.Length);
            str.Close();
            System.Net.WebResponse rsp = req.GetResponse();

            string txt = new System.IO.StreamReader(rsp.GetResponseStream()).ReadToEnd();
            rsp.Close();

            //JSON format response
            //{"tags_list": [{"c": 3, "t": "suzumiya <b>haruhi</b> no yuutsu"}, {"c": 1, "t": "suzumiya <b>haruhi</b>"}]}
            object[] tagList = ((new System.Web.Script.Serialization.JavaScriptSerializer()).DeserializeObject(txt) as Dictionary<string, object>)["tags_list"] as object[];
            for (int i = 0; i < tagList.Length && i < 8; i++)
            {
                Dictionary<string, object> tag = tagList[i] as Dictionary<string, object>;
                if (tag["t"].ToString().Trim().Length > 0)
                    re.Add(new AutoHintItem() { Word = tag["t"].ToString().Trim().Replace("<b>", "").Replace("</b>", ""), Count = "N/A" });
            }

            return re;
        }

        private ImageItem GenerateImg(string detailUrl, string sample_url, string dimension, string tags, string id)
        {
            int intId = int.Parse(id);

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
            sample_url = FormattedImgUrl(HomeUrl, sample_url);

            ImageItem img = new ImageItem()
            {
                //Date = "N/A",
                //FileSize = file_size.ToUpper(),
                //Desc = tags,
                Id = intId,
                //JpegUrl = preview_url,
                //OriginalUrl = preview_url,
                //PreviewUrl = preview_url,
                ThumbnailUrl = sample_url,
                //Score = 0,
                Width = width,
                Height = height,
                //Tags = tags,
                DetailUrl = detailUrl,
            };

            img.GetDetailAction = () =>
            {
                var i = img;
                var p = Settings.Proxy;
                //retrieve details
                MyWebClient web = new MyWebClient();
                web.Proxy = p;
                web.Headers["Cookie"] = sessionId;
                web.Encoding = Encoding.UTF8;
                string page = web.DownloadString(i.DetailUrl);

                //<b>Size:</b> 326.0KB<br>
                int index = page.IndexOf("<b>Size");
                string fileSize = page.Substring(index + 12, page.IndexOf('<', index + 12) - index - 12).Trim();
                //<b>Date Published:</b> 2/24/12 4:57 PM
                index = page.IndexOf("<b>Date Published");
                string date = page.Substring(index + 22, page.IndexOf('<', index + 22) - index - 22).Trim();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(page);
                //retrieve rating node
                HtmlNode ratnode = doc.DocumentNode.SelectSingleNode("//span[@id='rating']");
                try
                {
                    i.Score = int.Parse(ratnode.SelectSingleNode("//*[@id='score_n']").InnerText);
                }
                catch { }

                i.OriginalUrl = FormattedImgUrl(HomeUrl, ratnode.SelectSingleNode("a").Attributes["href"].Value);

                //retrieve img node
                HtmlNode imgnode = doc.DocumentNode.SelectSingleNode("//div[@id='big_preview_cont']");
                string jpgUrl = FormattedImgUrl(HomeUrl, imgnode.SelectSingleNode("a").Attributes["href"].Value);
                string previewUrl = FormattedImgUrl(HomeUrl, imgnode.SelectSingleNode("a/picture/source/img").Attributes["src"].Value);

                i.TagsText = imgnode.SelectSingleNode("a/picture/source/img").Attributes["alt"].Value;
                StringBuilder sb = new StringBuilder(i.TagsText);
                sb.Replace("\n", " ");
                sb.Replace("\t", " ");
                Regex rx = new Regex("Anime.*with");
                if (rx.IsMatch(sb.ToString()))
                    i.TagsText = rx.Replace(sb.ToString(), "").Trim();

                try
                {
                    i.Author = doc.DocumentNode.SelectSingleNode("//div[@id='cont']/div[2]/div[1]/div[1]/a/span").InnerText;
                }
                catch
                {
                    try
                    {
                        i.Author = doc.DocumentNode.SelectSingleNode("//div[@id='cont']/div[2]/div[1]/a[1]").InnerText;
                    }
                    catch { }
                }

                i.Description = i.TagsText;
                i.Date = date;
                i.FileSize = fileSize;
                i.JpegUrl = jpgUrl;
                i.PreviewUrl = previewUrl;
            };

            return img;
        }

        private void Login(System.Net.IWebProxy proxy)
        {
            if (sessionId != null) return;
            try
            {
                int index = rand.Next(0, user.Length);
                //http://mjv-art.org/login/submit
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(HomeUrl + "/login/submit");
                req.UserAgent = MoeSession.DefUA;
                req.Proxy = proxy;
                req.Timeout = 8000;
                req.Method = "POST";
                req.AllowAutoRedirect = false;

                byte[] buf = Encoding.UTF8.GetBytes("login=" + user[index] + "&password=" + pass[index]);
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = buf.Length;
                System.IO.Stream str = req.GetRequestStream();
                str.Write(buf, 0, buf.Length);
                str.Close();
                System.Net.WebResponse rsp = req.GetResponse();

                sessionId = rsp.Headers.Get("Set-Cookie");
                //sitelang=en; Max-Age=31104000; Path=/; expires=Sun, 14-Jul-2013 04:15:44 GMT, asian_server=86227259c6ca143cca28b4ffffa1347e73405154e374afaf48434505985a4cca70fd30c4; expires=Tue, 19-Jan-2038 03:14:07 GMT; Path=/
                if (sessionId == null || !sessionId.Contains("asian_server"))
                {
                    throw new Exception("自动登录失败");
                }
                //sitelang=en; asian_server=86227259c6ca143cca28b4ffffa1347e73405154e374afaf48434505985a4cca70fd30c4
                int idIndex = sessionId.IndexOf("sitelang");
                string idstr = sessionId.Substring(idIndex, sessionId.IndexOf(';', idIndex) + 2 - idIndex);
                idIndex = sessionId.IndexOf("asian_server");
                string hashstr = sessionId.Substring(idIndex, sessionId.IndexOf(';', idIndex) - idIndex);
                sessionId = idstr + hashstr;
                rsp.Close();
            }
            catch (System.Net.WebException)
            {
                //throw new Exception("自动登录失败");
                sessionId = "";
            }
        }

        /// <summary>
        /// 图片地址格式化
        /// 2016年12月对带域名型地址格式化
        /// by YIU
        /// </summary>
        /// <param name="pr_host">图站域名</param>
        /// <param name="pr_url">预处理的URL</param>
        /// <returns>处理后的图片URL</returns>
        private static string FormattedImgUrl(string pr_host, string pr_url)
        {
            try
            {
                int po = pr_host.IndexOf("//");
                string phh = pr_host.Substring(0, pr_host.IndexOf(':') + 1);
                string phu = pr_host.Substring(po, pr_host.Length - po);

                //地址中有主域名 去掉主域名
                if (pr_url.StartsWith(phu))
                    return pr_host + pr_url.Replace(phu, "");

                //地址中有子域名 补完子域名
                else if (pr_url.StartsWith("//"))
                    return phh + pr_url;

                //地址没有域名 补完地址
                else if (pr_url.StartsWith("/"))
                    return pr_host + pr_url;

                return pr_url;
            }
            catch
            {
                return pr_url;
            }
        }
    }
}
