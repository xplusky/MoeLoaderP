using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using HtmlAgilityPack;
using MoeLoader.Core.Sites;

namespace MoeLoader.Core.Site
{
    public class SiteEshuu : MoeSite
    {
        public override string HomeUrl => "http://e-shuushuu.net";

        public override string DisplayName => "e-shuushuu.net";
            
        public override string ShortName => "e-shu";
        
        /// <summary>
        /// e-shuushuu.net site
        /// </summary>
        public SiteEshuu()
        {
            SurpportState.IsSupportScore = false;

            SubMenu.Add("标签");
            SubMenu.Add("来源");
            SubMenu.Add("画师");
            SubMenu.Add("角色");
        }

        

        /// <summary>
        /// get images sync
        /// </summary>
        //public List<Img> GetImages(int page, int count, string keyWord, int maskScore, int maskRes, ViewedID lastViewed, bool maskViewed, System.Net.IWebProxy proxy, bool showExplicit)
        //{
        //    return GetImages(GetPageString(page, count, keyWord, proxy), maskScore, maskRes, lastViewed, maskViewed, proxy, showExplicit);
        //}

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            string url = HomeUrl + "/?page=" + page;

            MyWebClient web = new MyWebClient();
            web.Proxy = proxy;
            web.Encoding = Encoding.UTF8;

            if (keyWord.Length > 0)
            {
                url = HomeUrl + "/search/process/";
                //multi search
                string data = "tags=" + keyWord + "&source=&char=&artist=&postcontent=&txtposter=";
                if (SubListIndex == 1)
                {
                    data = "tags=&source=" + keyWord + "&char=&artist=&postcontent=&txtposter=";
                }
                else if (SubListIndex == 2)
                {
                    data = "tags=&source=&char=&artist=" + keyWord + "&postcontent=&txtposter=";
                }
                else if (SubListIndex == 3)
                {
                    data = "tags=&source=&char=" + keyWord + "&artist=&postcontent=&txtposter=";
                }

                //e-shuushuu需要将关键词转换为tag id，然后进行搜索
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                req.UserAgent = MoeSession.DefUA;
                req.Proxy = proxy;
                req.Timeout = 8000;
                req.Method = "POST";
                //prevent 303
                req.AllowAutoRedirect = false;
                byte[] buf = Encoding.UTF8.GetBytes(data);
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = buf.Length;
                System.IO.Stream str = req.GetRequestStream();
                str.Write(buf, 0, buf.Length);
                str.Close();
                System.Net.WebResponse rsp = req.GetResponse();
                //http://e-shuushuu.net/search/results/?tags=2
                //HTTP 303然后返回实际地址
                string location = rsp.Headers["Location"];
                rsp.Close();
                if (location != null && location.Length > 0)
                {
                    //非完整地址，需要前缀
                    url = rsp.Headers["Location"] + "&page=" + page;
                }
                else
                {
                    throw new Exception("没有搜索到关键词相关的图片（每个关键词前后需要加双引号如 \"sakura\"））");
                }
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
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//div[@class='image_thread display']");
            if (nodes == null)
            {
                return imgs;
            }
            foreach (HtmlNode imgNode in nodes)
            {
                string id = imgNode.Attributes["id"].Value;
                HtmlNode imgHref = imgNode.SelectSingleNode(".//a[@class='thumb_image']");
                string fileUrl = imgHref.Attributes["href"].Value;
                string previewUrl = imgHref.SelectSingleNode("img").Attributes["src"].Value;
                HtmlNode meta = imgNode.SelectSingleNode(".//div[@class='meta']");
                string date = meta.SelectSingleNode(".//dd[2]").InnerText;
                string fileSize = meta.SelectSingleNode(".//dd[3]").InnerText;
                string dimension = meta.SelectSingleNode(".//dd[4]").InnerText;
                string tags = "";
                try
                {
                    tags = meta.SelectSingleNode(".//dd[5]").InnerText;
                }
                catch { }

                ImageItem img = GenerateImg(fileUrl, previewUrl, dimension, date, tags, fileSize, id);
                if (img != null) imgs.Add(img);
            }

            return imgs;
        }
        

        public override List<AutoHintItem> GetAutoHintItems(string word, System.Net.IWebProxy proxy)
        {
            //type 1 tag 2 source 3 artist | chara no type
            List<AutoHintItem> re = new List<AutoHintItem>();

            //chara without hint
            if (SubListIndex == 3) return re;

            string url = $"{HomeUrl}/httpreq.php?mode=tag_search&tags={word}&type={SubListIndex + 1}";
            MyWebClient web = new MyWebClient();
            web.Timeout = 8;
            web.Proxy = proxy;
            web.Encoding = Encoding.UTF8;

            string txt = web.DownloadString(url);

            string[] lines = txt.Split(new char[] { '\n' });
            for (int i = 0; i < lines.Length && i < 8; i++)
            {
                if (lines[i].Trim().Length > 0)
                    re.Add(new AutoHintItem() { Word = lines[i].Trim(), Count = "N/A" });
            }

            return re;
        }

        private ImageItem GenerateImg(string file_url, string preview_url, string dimension, string created_at, string tags, string file_size, string id)
        {
            int intId = int.Parse(id.Substring(1));

            int width = 0, height = 0;
            try
            {
                //706x1000 (0.706 MPixel)
                dimension = dimension.Substring(0, dimension.IndexOf('(')).Trim();
                width = int.Parse(dimension.Substring(0, dimension.IndexOf('x')));
                height = int.Parse(dimension.Substring(dimension.IndexOf('x') + 1));
            }
            catch { }

            //convert relative url to absolute
            if (file_url.StartsWith("/"))
                file_url = HomeUrl + file_url;
            if (preview_url.StartsWith("/"))
                preview_url = HomeUrl + preview_url;

            ImageItem img = new ImageItem()
            {
                Date = created_at.Replace("\t", "").Replace("\n", ""),
                FileSize = file_size.Replace("\t", "").Replace("\n", "").ToUpper(),
                Description = tags.Replace("\t", "").Replace("\n", ""),
                Id = intId,
                JpegUrl = file_url,
                OriginalUrl = file_url,
                PreviewUrl = file_url,
                ThumbnailUrl = preview_url,
                Width = width,
                Height = height,
                TagsText = tags.Replace("\t", "").Replace("\n", ""),
                DetailUrl = HomeUrl + "/image/" + intId,
            };
            return img;
        }
    }
}
