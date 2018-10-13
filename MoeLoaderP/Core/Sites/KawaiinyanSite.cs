using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// kawaiinyan.com Fixed 20180922
    /// </summary>
    public class KawaiinyanSite : MoeSite
    {
        public override string HomeUrl => "https://kawaiinyan.com";

        public override string DisplayName => "Kawaiinyan";

        public override string ShortName => "kawaiinyan";

        public KawaiinyanSite()
        {
            SurpportState.IsSupportRating = false;
            SurpportState.IsSupportAutoHint = false;
            DownloadTypes.Add("原图", 4);
        }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            var net = new NetSwap(Settings);
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
            var jsonres =await client.GetAsync(query, token);
            var jsonstr = await jsonres.Content.ReadAsStringAsync();
            var imageitems = new ImageItems();
            dynamic json = JsonConvert.DeserializeObject(jsonstr);
            if (json?.images == null) return imageitems;
            foreach (var image in json.images)
            {
                var img = new ImageItem(this,para);
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
                img.Urls.Add(new UrlInfo("缩略图", 1, $"{sub}{UrlInner($"{id}")}/small.{small}"));
                var orig = $"{image.orig}";
                var big = $"{image.big}";
                if (!string.IsNullOrWhiteSpace(orig))
                {
                    img.Urls.Add(new UrlInfo("原图", 4, $"{sub}{UrlInner($"{id}")}/orig.{orig}"));
                }
                else if (!string.IsNullOrWhiteSpace(big))
                {
                    img.Urls.Add(new UrlInfo("原图", 4, $"{sub}{UrlInner($"{id}")}/big.{big}"));
                }
                img.DetailUrl = $"{HomeUrl}/image?id={id}";
                
                img.Site = this;

                imageitems.Add(img);
            }
            token.ThrowIfCancellationRequested();
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
