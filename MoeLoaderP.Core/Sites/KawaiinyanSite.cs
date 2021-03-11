using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
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
            SupportState.IsSupportRating = false;
            SupportState.IsSupportAutoHint = false;
            DownloadTypes.Add("原图", 4);
        }
        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            if (Net == null)
            {
                Net = new NetOperator(Settings);
                Net.SetReferer(HomeUrl);
                await Net.Client.GetAsync(HomeUrl, token);
            }
            var size = "";
            if (para.IsFilterResolution)
            {
                var s = para.MinWidth > para.MinHeight ? para.MinWidth : para.MinHeight;
                size = $"{s}";
            }
            var orient = "";
            switch (para.Orientation)
            {
                case ImageOrientation.Portrait:
                    orient = "p";
                    break;
                case ImageOrientation.Landscape:
                    orient = "l";
                    break;
            }
            var pair = new Pairs
            {
                {"tags", para.Keyword.ToEncodedUrl()},
                {"size",size },
                {"orient",orient },
                {"page", $"{para.PageIndex}"}
            };
            var imageItems = new MoeItems();
            var json = await  Net.GetJsonAsync($"{HomeUrl}/index.json", token, pair);
            if (json?.images == null) return imageItems;
            foreach (var image in json.images)
            {
                var img = new MoeItem(this, para);
                var id = (int)image.id;
                img.Id = id;
                var sub = $"https://{id % 10}.s.kawaiinyan.com/i";
                img.Uploader = $"{image.user_name}";
                img.Source = $"{image.adv_link}";
                img.Score = (int)image.yes;
                var tags = $"{image.tags}";
                foreach (var s in tags.Split(','))
                {
                    if (s.IsEmpty()) continue;
                    img.Tags.Add(s);
                }
                var small = $"{image.small}";
                img.Urls.Add(1, $"{sub}{UrlInner($"{id}")}/small.{small}");
                var orig = $"{image.orig}";
                var big = $"{image.big}";
                if (!orig.IsEmpty())
                {
                    img.Urls.Add(4, $"{sub}{UrlInner($"{id}")}/orig.{orig}");
                }
                else if (!big.IsEmpty())
                {
                    img.Urls.Add(4, $"{sub}{UrlInner($"{id}")}/big.{big}");
                }
                img.DetailUrl = $"{HomeUrl}/image?id={id}";

                img.Site = this;
                img.OriginString = $"{image}";

                imageItems.Add(img);
            }
            token.ThrowIfCancellationRequested();
            return imageItems;
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
