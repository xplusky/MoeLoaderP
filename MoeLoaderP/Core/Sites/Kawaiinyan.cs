using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// kawaiinyan.com Fixed 20180922
    /// </summary>
    public class Kawaiinyan : MoeSite
    {
        public override string HomeUrl => "https://kawaiinyan.com";

        public override string DisplayName => "Kawaiinyan";

        public override string ShortName => "kawaiinyan";

        public Kawaiinyan()
        {
            SurpportState.IsSupportRating = false;
            SurpportState.IsSupportAutoHint = false;
        }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para)
        {
            var net = new MoeNet(Settings);
            var client = net.Client;
            var size = "";
            if (para.IsFilterResolution)
            {
                var s = para.MinWidth > para.MinHeight ? para.MinWidth : para.MinHeight;
                size = $"{s}";
            }
            var orient = "";
            switch (para.Orientation)
            {
                default:
                case ImageOrientation.None:
                    break;
                case ImageOrientation.Portrait:
                    orient = "p";
                    break;
                case ImageOrientation.Landscape:
                    orient = "l";
                    break;
            }
            var query = $"{HomeUrl}/new.json?tags={para.Keyword.ToEncodedUrl()}&size={size}&orient={orient}&page={para.PageIndex}";
            var jsonstr =await client.GetStringAsync(query);
            var imageitems = new ImageItems();
            dynamic json = JsonConvert.DeserializeObject(jsonstr);
            if (json?.images == null) return imageitems;
            foreach (var image in json.images)
            {
                var img = new ImageItem();
                var id = (int) image.id;
                img.Id = id;
                var sub = $"https://{id % 10}.s.kawaiinyan.com/i";
                img.Author = $"{image.user_name}";
                img.Source = $"{image.adv_link}";
                img.Score = (int) image.yes;
                var tags = $"{image.tags}";
                foreach (var s in tags.Split(','))
                {
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    img.Tags.Add(s);
                }
                var small = $"{image.small}";
                img.ThumbnailUrl = $"{sub}{UrlInner($"{id}")}/small.{small}";
                var orig = $"{image.orig}";
                var big = $"{image.big}";
                if (!string.IsNullOrWhiteSpace(orig))
                {
                    img.OriginalUrl = $"{sub}{UrlInner($"{id}")}/orig.{orig}";
                }else if (!string.IsNullOrWhiteSpace(big))
                {
                    img.OriginalUrl = $"{sub}{UrlInner($"{id}")}/big.{big}";
                }
                img.DetailUrl = $"{HomeUrl}/image?id={id}";
                
                img.Site = this;

                imageitems.Add(img);
            }

            return imageitems;
        }

        private static string UrlInner(string id)
        {
            int len;
            if (id.Length % 2 == 0)
                len = id.Length - 1;
            else
                len = id.Length;
            for (var a = 0; a <= len / 2; a++)
                id = id.Insert(a + 2 * a, "/");
            return id;
        }
    }
}
