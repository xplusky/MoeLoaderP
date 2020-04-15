using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// worldcosplay.net fixed 20180925
    /// </summary>
    public class WCosplaySite : MoeSite
    {
        public override string HomeUrl => "https://worldcosplay.net";
        public override string DisplayName => "WorldCosplay";
        public override string ShortName => "worldcosplay";

        public WCosplaySite()
        {
            SupportState.IsSupportAutoHint = false;
            DownloadTypes.Add("大图", 3);
        }

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            if (Net == null) Net = new NetDocker();

            //http://worldcosplay.net/api/photo/list?page=3&limit=2&sort=created_at&direction=descend
            var url = $"{HomeUrl}/api/photo/list";
            var pairs = new Pairs
            {
                {"page", $"{para.PageIndex}"},
                {"limit",$"{para.Count}" },
                {"sort", "created_at"},
                {"direction","descend" }
            };
            
            if (para.HasKeyword)
            {
                //http://worldcosplay.net/api/photo/search?page=2&rows=48&q=%E5%90%8A%E5%B8%A6%E8%A2%9C%E5%A4%A9%E4%BD%BF
                url =  $"{HomeUrl}/api/photo/search";
                pairs = new Pairs
                {
                    {"page", $"{para.PageIndex}"},
                    {"rows",$"{para.Count}" },
                    {"q", para.Keyword.ToEncodedUrl()}
                };
            }
            
            // images

            var imgs = new MoeItems();
            var json = await Net.GetJsonAsync(url, token, pairs);
            if (json?.list == null) return imgs;
            foreach (var jitem in json.list)
            {
                var img = new MoeItem(this, para)
                {
                    Uploader = $"{jitem.member?.nickname}", 
                    DetailUrl = $"{HomeUrl}{jitem.photo?.url}", 
                    Id = (int) (jitem.photo?.id ?? 0d)
                };
                img.Urls.Add(1, $"{jitem.photo?.thumbnail_url_display}", HomeUrl);
                img.Urls.Add(3, $"{jitem.photo?.large_url}", img.DetailUrl);
                img.Score = $"{jitem.photo?.good_cnt}".ToInt();
                img.Date = $"{jitem.photo?.created_at}".ToDateTime();
                var twidth = (int)(jitem.photo?.thumbnail_width ?? 0d);
                var theight = (int)(jitem.photo?.thumbnail_height ?? 0d);
                if (twidth > 0 && theight > 0) //缩略图的尺寸 175级别 大图 740级别
                {
                    if (twidth > theight)
                    {
                        img.Height = 740 * theight / twidth;
                        img.Width = 740;
                    }
                    else
                    {
                        img.Width = 740 * twidth / theight;
                        img.Height = 740;
                    }
                }
                img.Title = $"{jitem.photo?.subject}";
                img.IsExplicit = jitem.photo?.viewable ?? false;
                img.OriginString = $"{jitem}";
                imgs.Add(img);
            }

            return imgs;
        }
    }
}
