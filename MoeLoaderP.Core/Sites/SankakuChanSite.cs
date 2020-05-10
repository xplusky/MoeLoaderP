using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// chan.sankakucomplex.com fixed 20200316
    /// </summary>
    public class SankakuChanSite : MoeSite
    {
        public override string HomeUrl => "https://chan.sankakucomplex.com";

        public override string DisplayName => "SankakuComplex[Chan]";

        public override string ShortName => "sankakucomplex-chan";

        public SankakuChanSite()
        {
            DownloadTypes.Add("原图", 4);
        }

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            var imgs = new MoeItems();
            const string api = "https://capi-v2.sankakucomplex.com";
            const string beta = "https://beta.sankakucomplex.com";
            Net = Net == null ? new NetDocker(Settings, api) : Net.CloneWithOldCookie();

            Net.SetReferer(beta);
            var pairs = new Pairs
            {
                {"lang", "en"},
                {"page", $"{para.PageIndex}"},
                {"limit", $"{para.Count}"},
                {"hide_posts_in_books", "in-larger-tags"},
                {"default_threshold", "1"},
                {"tags",para.Keyword.ToEncodedUrl() }
            };
            var json = await Net.GetJsonAsync($"{api}/posts", token, pairs);
            foreach (var jitem in Extend.CheckListNull(json))
            {
                var img = new MoeItem(this, para)
                {
                    Net = Net.CloneWithOldCookie(),
                    Id = $"{jitem.id}".ToInt(),
                    Width = $"{jitem.width}".ToInt(),
                    Height = $"{jitem.height}".ToInt(),
                    Score = $"{jitem.total_score}".ToInt()
                };
                img.Urls.Add(1, $"{jitem.preview_url}",beta);
                img.Urls.Add(2, $"{jitem.sample_url}",beta);
                img.Urls.Add(4, $"{jitem.file_url}",$"{beta}/post/show/{img.Id}");
                img.IsExplicit = $"{jitem.rating}" != "s";
                img.Date = $"{jitem.created_at?.s}".ToDateTime();
                img.Uploader = $"{jitem.author?.name}";
                img.DetailUrl = $"{beta}/post/show/{img.Id}";
                foreach (var tag in Extend.CheckListNull(jitem.tags))
                {
                    img.Tags.Add($"{tag.name_en}");
                }

                img.OriginString = $"{jitem}";
                imgs.Add(img);
            }

            return imgs;
        }

        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            var ahitems = new AutoHintItems();
            const string api = "https://capi-v2.sankakucomplex.com";
            Net = Net == null ? new NetDocker(Settings, api) : Net.CloneWithOldCookie();
            var pairs = new Pairs
            {
                {"lang", "en"},
                {"tag", para.Keyword.ToEncodedUrl()},
                {"target", "post"},
                {"show_meta", "1"}
            };
            var json = await Net.GetJsonAsync($"{api}/tags/autosuggestCreating",token, pairs);
            foreach (var jitem in Extend.CheckListNull(json))
            {
                var ahitem = new AutoHintItem();
                ahitem.Word = $"{jitem.name}";
                ahitem.Count = $"{jitem.count}";
                ahitems.Add(ahitem);
            }

            return ahitems;

        }
    }

}
