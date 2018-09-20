using System.Collections.Generic;
using System.Text;
using MoeLoader.Core.Sites;

namespace MoeLoader.Core.Site
{
    public class SiteWCosplay : MoeSite
    {
        public override string HomeUrl => "https://worldcosplay.net";
        public override string DisplayName => "worldcosplay.net";
        public override string ShortName => "worldcosplay";

        //public override System.Drawing.Point LargeImgSize { get { return new System.Drawing.Point(175, 175); } }

        /// <summary>
        /// worldcosplay.net site
        /// </summary>
        public SiteWCosplay()
        {
            SurpportState.IsSupportAutoHint = false;
        }

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            //http://worldcosplay.net/api/photo/list?page=3&limit=2&sort=created_at&direction=descend
            string url = HomeUrl + "/api/photo/list?page=" + page + "&limit=" + count + "&sort=created_at&direction=descend";

            MyWebClient web = new MyWebClient();
            web.Proxy = proxy;
            web.Encoding = Encoding.UTF8;

            if (keyWord.Length > 0)
            {
                //http://worldcosplay.net/api/photo/search?page=2&rows=48&q=%E5%90%8A%E5%B8%A6%E8%A2%9C%E5%A4%A9%E4%BD%BF
                url = HomeUrl + "/api/photo/search?page=" + page + "&rows=" + count + "&q=" + keyWord;
            }

            string pageString = web.DownloadString(url);
            web.Dispose();

            return pageString;
        }

        public override List<ImageItem> GetImages(string pageString, System.Net.IWebProxy proxy)
        {
            List<ImageItem> imgs = new List<ImageItem>();

            //JSON format response
            //{"pager":{"next_page":4,"previous_page":2,"current_page":"3","indexes":[1,2,"3",4,5]},"has_error":0,"list":[{"character":{"name":"Mami Tomoe"},"member":
            //{"national_flag_url":"http://worldcosplay.net/img/flags/tw.gif","url":"http://worldcosplay.net/member/reizuki/","global_name":"Okuda Lily"},"photo":
            //{"monthly_good_cnt":"0","weekly_good_cnt":"0","rank_display":null,"orientation":"portrait","thumbnail_width":"117","thumbnail_url_display":
            //"http://image.worldcosplay.net/uploads/26450/8b6438c21db2b1402f63427d0ef8983a85969d0a-175.jpg","is_small":0,"created_at":"2012-04-16 21:03",
            //"thumbnail_height":"175","good_cnt":"0","monthly_view_cnt":"0","url":"http://worldcosplay.net/photo/279556/","id":"279556","weekly_view_cnt":"0"}}]}
            object[] imgList = ((new System.Web.Script.Serialization.JavaScriptSerializer()).DeserializeObject(pageString) as Dictionary<string, object>)["list"] as object[];
            for (int i = 0; i < imgList.Length; i++)
            {
                Dictionary<string, object> tag = imgList[i] as Dictionary<string, object>;
                Dictionary<string, object> chara = tag["character"] as Dictionary<string, object>;
                Dictionary<string, object> member = tag["member"] as Dictionary<string, object>;
                Dictionary<string, object> photo = tag["photo"] as Dictionary<string, object>;

                ImageItem re = GenerateImg(photo["thumbnail_url_display"].ToString(), chara["name"].ToString(), member["global_name"].ToString(), photo["thumbnail_width"].ToString()
                    , photo["thumbnail_height"].ToString(), photo["created_at"].ToString(), photo["good_cnt"].ToString(), photo["id"].ToString(), photo["url"].ToString());
                imgs.Add(re);
            }

            return imgs;
        }
        
        //public override List<TagItem> GetTags(string word, System.Net.IWebProxy proxy)
        //{
        //    List<TagItem> re = new List<TagItem>();
        //    return re;
        //}

        private ImageItem GenerateImg(string preview_url, string chara, string member, string twidth, string theight, string date, string sscore, string id, string detailUrl)
        {
            int intId = int.Parse(id);
            int score;
            int.TryParse(sscore, out score);

            int width = 0, height = 0;
            try
            {
                //缩略图的尺寸 175级别 大图 740级别
                width = int.Parse(twidth);
                height = int.Parse(theight);
                if (width > height)
                {
                    //width 175
                    height = 740 * height / width;
                    width = 740;
                }
                else
                {
                    width = 740 * width / height;
                    height = 740;
                }
            }
            catch { }

            //convert relative url to absolute
            if (preview_url.StartsWith("/"))
                preview_url = HomeUrl + preview_url;

            //http://image.worldcosplay.net/uploads/26450/8b6438c21db2b1402f63427d0ef8983a85969d0a-175.jpg
            string fileUrl = preview_url.Replace("-175", "-740");

            ImageItem img = new ImageItem()
            {
                Date = date,
                FileSize = "",
                Description = member + " | " + chara,
                Id = intId,
                JpegUrl = fileUrl,
                OriginalUrl = fileUrl,
                PreviewUrl = fileUrl,
                ThumbnailUrl = preview_url,
                Score = score,
                Width = width,
                Height = height,
                TagsText = member + " | " + chara,
                DetailUrl = HomeUrl + detailUrl
            };

            return img;
        }
    }
}
