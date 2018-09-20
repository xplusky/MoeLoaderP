using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using MoeLoader.Core.Sites;
using Newtonsoft.Json.Linq;

namespace MoeLoader.Core.Site
{
    /// <summary>
    /// kawaiinyan.com by ulrevenge ver 1.0
    /// last change 180417
    /// </summary>
    class SiteKawaiinyan : MoeSite
    {
        //Tags|Min size,px Orientation Portrait Landscape
        public enum KawaiiSrcType { TagPxO, TagPxP, TagPxL }
        public override string HomeUrl => "https://kawaiinyan.com";
        public override string ShortName => "kawaiinyan";

        public override string DisplayName => "kawaiinyan.com";
        
        public virtual bool IsShowRes => false;

        public SiteKawaiinyan()
        {
            SurpportState.IsSupportAutoHint = false;
            SurpportState.IsSupportResolution = false;

            SubMenu.Add("Portrait"); // 0 "标签|最小分辨率(单值)\r\nPortrait 立绘图"
            SubMenu.Add("Landscape"); // 1 "标签|最小分辨率(单值)\r\nLandscape 有风景的图"
            SubMenu.Add("Orientation"); // 2 "标签|最小分辨率(单值)\r\nOrientation"
        }

        private KawaiiSrcType srcType
        {
            get
            {
                switch (SubListIndex)
                {
                    case 0: return KawaiiSrcType.TagPxO;
                    case 1: return KawaiiSrcType.TagPxP;
                    case 2: return KawaiiSrcType.TagPxL;
                }
                return KawaiiSrcType.TagPxO;
            }
        }

        private MoeSession Sweb = new MoeSession();
        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            //https://kawaiinyan.com/new.json?tags=&size=&orient=
            //https://kawaiinyan.com/new.json?tags=&size=&orient=l
            //https://kawaiinyan.com/new.json?tags=&size=&orient=p
            //https://kawaiinyan.com/new.json?tags=&size=&orient=l&page=2
            string tag = null, px = null, url = null;
            if (keyWord.Contains("|"))
            {
                tag = keyWord.Split('|')[0];
                px = keyWord.Split('|')[1];
            }
            else if (Regex.IsMatch(keyWord, @"\d+"))
                px = keyWord;
            else
                tag = keyWord;

            url = HomeUrl + "/new.json?tags=" + tag + "&size=" + px;
            if (srcType == KawaiiSrcType.TagPxO)
                url += "&orient=&page=" + page;
            else if (srcType == KawaiiSrcType.TagPxP)
                url += "&orient=p&page=" + page;
            else if (srcType == KawaiiSrcType.TagPxL)
                url += "&orient=l&page=" + page;
            string pageString = Sweb.Get(url, proxy);
            return pageString;
        }

        public override List<ImageItem> GetImages(string pageString, IWebProxy proxy)
        {
            List<ImageItem> imgs = new List<ImageItem>();
            string imagesJson = null;
            if (string.IsNullOrWhiteSpace(pageString)) return imgs;
            //外层Json
            JObject jsonObj = JObject.Parse(pageString);
            //取得images Json 字符串
            if (jsonObj["images"].ToString() != null)
                imagesJson = jsonObj["images"].ToString();
            if (string.IsNullOrWhiteSpace(imagesJson)) return imgs;
            //解析images Json
            object[] array = (new JavaScriptSerializer()).DeserializeObject(imagesJson) as object[];
            foreach (object o in array)
            {
                Dictionary<string, object> obj = o as Dictionary<string, object>;

                string
                id = "0",
                subUrl = "",
                tags = "",
                score = "N/A",
                source = "0",
                sample = "",
                jpeg_url = "",
                file_url = "",
                preview_url = "",
                author = "",
                detailUrl = "";
                //图片ID
                if (obj.ContainsKey("id") && obj["id"] != null)
                {
                    id = obj["id"].ToString();
                    //图片子站
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("https://");
                    sb.Append((Convert.ToInt32(id) % 10).ToString());
                    sb.Append(".s.");
                    sb.Append(ShortName);
                    sb.Append(".com/i");
                    subUrl = sb.ToString();
                }
                //投稿者
                if (obj.ContainsKey("user_name") && obj["user_name"] != null)
                    author = obj["user_name"].ToString();
                //图片来源
                if (obj.ContainsKey("adv_link") && obj["adv_link"] != null)
                    source = obj["adv_link"].ToString();
                //评级和评分
                if (obj.ContainsKey("yes") && obj["yes"] != null)
                    if (obj.ContainsKey("no") && obj["no"] != null)
                        score = (Convert.ToInt32(obj["yes"].ToString())
                        - Convert.ToInt32(obj["no"].ToString())).ToString();
                //标签
                if (obj.ContainsKey("tags") && obj["tags"] != null)
                    tags = obj["tags"].ToString();
                //缩略图 small 显示不全
                if (obj.ContainsKey("small") && obj["small"] != null)
                {
                    sample = subUrl + StringJoinString(id) + "/" + "small." + obj["small"].ToString();
                    //https://kawaiinyan.com/new#i27963.jpg
                    detailUrl = HomeUrl + "/new#i" + id + "." + obj["small"].ToString();
                }
                //jpg 预览图
                if (obj.ContainsKey("big") && obj["big"] != null)
                    preview_url = jpeg_url = file_url = subUrl + StringJoinString(id) + "/" + "big." + obj["big"].ToString();

                //原图
                if (obj.ContainsKey("orig") && obj["orig"] != null)
                    file_url = subUrl + StringJoinString(id) + "/" + "orig." + obj["orig"].ToString();

                ImageItem img = new ImageItem
                {
                    Description = tags,
                    Id = Convert.ToInt32(id),
                    Author = author,
                    JpegUrl = jpeg_url,
                    OriginalUrl = file_url,
                    PreviewUrl = preview_url,
                    ThumbnailUrl = sample,
                    Score = Convert.ToInt32(score),
                    Source = source,
                    TagsText = tags,
                    DetailUrl = detailUrl,
                };
                imgs.Add(img);
            }
            return imgs;

        }

        private string StringJoinString(string id)
        {
            int len;
            if (id.Length % 2 == 0)
                len = id.Length - 1;
            else
                len = id.Length;
            for (int a = 0; a <= len / 2; a++)
                id = id.Insert(a + 2 * a, "/");
            return id;
        }
    }
}
