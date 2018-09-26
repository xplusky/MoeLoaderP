using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MoeLoader.Core.Sites
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
            SurpportState.IsSupportAutoHint = false;
        }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            if (Net == null)
            {
                Net = new NetSwap();
            }

            //http://worldcosplay.net/api/photo/list?page=3&limit=2&sort=created_at&direction=descend
            var url = $"{HomeUrl}/api/photo/list?page={para.PageIndex}&limit={para.Count}&sort=created_at&direction=descend";
            
            if (para.Keyword.Length > 0)
            {
                //http://worldcosplay.net/api/photo/search?page=2&rows=48&q=%E5%90%8A%E5%B8%A6%E8%A2%9C%E5%A4%A9%E4%BD%BF
                url =  $"{HomeUrl}/api/photo/search?page={para.PageIndex}&rows={para.Count}&q={para.Keyword}";
            }
            
            // images

            var imgs = new ImageItems();
            dynamic json = JsonConvert.DeserializeObject(await Net.Client.GetStringAsync(url));
            if (json?.list == null) return imgs;
            foreach (var jitem in json.list)
            {
                var img = new ImageItem();
                img.Site = this;
                img.Author = $"{jitem.member?.nickname}";
                img.ThumbnailReferer = HomeUrl;
                img.DetailUrl = $"{HomeUrl}{jitem.photo?.url}";
                img.Id = (int) (jitem.photo?.id ?? 0d);
                img.ThumbnailUrl = $"{jitem.photo?.thumbnail_url_display}";
                img.FileReferer = img.DetailUrl;
                img.FileUrl = $"{jitem.photo?.large_url}";
                int.TryParse($"{jitem.photo?.good_cnt}", out var score);
                img.Score = score;
                DateTime.TryParse($"{jitem.photo?.created_at}", out var date);
                img.CreatTime = date;
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
                imgs.Add(img);
            }

            return imgs;
        }
    }
}
