using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeLoader.Core.Sites
{
    public class WCosplaySite : MoeSite
    {
        public override string HomeUrl => "https://worldcosplay.net";
        public override string DisplayName => "worldcosplay.net";
        public override string ShortName => "worldcosplay";

        public WCosplaySite()
        {
            SurpportState.IsSupportAutoHint = false;
        }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para)
        {
            if (Net == null)
            {
                Net = new NetSwap();
            }

            //http://worldcosplay.net/api/photo/list?page=3&limit=2&sort=created_at&direction=descend
            var url = HomeUrl + "/api/photo/list?page=" + para.PageIndex + "&limit=" + para.Count + "&sort=created_at&direction=descend";


            if (para.Keyword.Length > 0)
            {
                //http://worldcosplay.net/api/photo/search?page=2&rows=48&q=%E5%90%8A%E5%B8%A6%E8%A2%9C%E5%A4%A9%E4%BD%BF
                url = HomeUrl + "/api/photo/search?page=" + para.PageIndex + "&rows=" + para.Count + "&q=" + para.Keyword;
            }

            var pageString = await Net.Client.GetStringAsync(url);

            // images

            var imgs = new ImageItems();

            //JSON format response
            //{"pager":{"next_page":4,"previous_page":2,"current_page":"3","indexes":[1,2,"3",4,5]},"has_error":0,"list":[{"character":{"name":"Mami Tomoe"},"member":
            //{"national_flag_url":"http://worldcosplay.net/img/flags/tw.gif","url":"http://worldcosplay.net/member/reizuki/","global_name":"Okuda Lily"},"photo":
            //{"monthly_good_cnt":"0","weekly_good_cnt":"0","rank_display":null,"orientation":"portrait","thumbnail_width":"117","thumbnail_url_display":
            //"http://image.worldcosplay.net/uploads/26450/8b6438c21db2b1402f63427d0ef8983a85969d0a-175.jpg","is_small":0,"created_at":"2012-04-16 21:03",
            //"thumbnail_height":"175","good_cnt":"0","monthly_view_cnt":"0","url":"http://worldcosplay.net/photo/279556/","id":"279556","weekly_view_cnt":"0"}}]}
            var imgList = ((new System.Web.Script.Serialization.JavaScriptSerializer()).DeserializeObject(pageString) as Dictionary<string, object>)?["list"] as object[];
            if (imgList != null)
                foreach (var t in imgList)
                {
                    var tag = t as Dictionary<string, object>;
                    var chara = tag["character"] as Dictionary<string, object>;
                    var member = tag["member"] as Dictionary<string, object>;
                    var photo = tag["photo"] as Dictionary<string, object>;

                    var re = GenerateImg(photo["thumbnail_url_display"].ToString(), chara["name"].ToString(), member["global_name"].ToString(), photo["thumbnail_width"].ToString()
                        , photo["thumbnail_height"].ToString(), photo["created_at"].ToString(), photo["good_cnt"].ToString(), photo["id"].ToString(), photo["url"].ToString());

                    re.Site = this;
                    imgs.Add(re);
                }

            return imgs;
        }


        private ImageItem GenerateImg(string preview_url, string chara, string member, string twidth, string theight, string date, string sscore, string id, string detailUrl)
        {
            var intId = int.Parse(id);
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
            var fileUrl = preview_url.Replace("-175", "-740");

            var img = new ImageItem()
            {
                Date = date,
                FileSize = "",
                Description = member + " | " + chara,
                Id = intId,
                JpegUrl = fileUrl,
                FileUrl = fileUrl,
                PreviewUrl = fileUrl,
                ThumbnailUrl = preview_url,
                Score = score,
                Width = width,
                Height = height,
                //TagsText = member + " | " + chara,
                DetailUrl = HomeUrl + detailUrl
            };

            return img;
        }
    }
}
