using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// yuriimg.com Last change 20200414
    /// </summary>
    public class YuriimgSite : MoeSite
    {
        public override string HomeUrl => "http://yuriimg.com";
        public override string ShortName => "yuriimg";
        public override string DisplayName => "Yuriimg（百合居）";

        public YuriimgSite()
        {
            SupportState.IsSupportAutoHint = false;
            SupportState.IsSupportRating = false;

            DownloadTypes.Add("原图", 4);
        }

        public async Task GetDetailTask(MoeItem img, string id, CancellationToken token = new CancellationToken())
        {
            var api = $"https://api.yuriimg.com/post/{id}";
            var json = await Net.GetJsonAsync(api, token);
            if (json == null) return;
            img.Score = $"{json.praise}".ToInt();
            img.Date = $"{json.format_date}".ToDateTime();
            img.Urls.Add(new UrlInfo("原图", 4, $"https://i.yuriimg.com/{json.src}"));
            img.Artist = $"{json.artist?.name}";
            img.Uploader = $"{json.user?.name}";
            img.UploaderId = $"{json.user?.id}";
            foreach (var tag in json.tags.general)
            {
                img.Tags.Add($"{tag.tags?.jp}");
            }
        }

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            if (Net == null) Net = new NetDocker(Settings, HomeUrl);
            var api = "https://api.yuriimg.com/posts";
            var pairs = new Pairs
            {
                {"page", $"{para.PageIndex}"},
                {"tags",para.Keyword.ToEncodedUrl() }
            };
            var json = await Net.GetJsonAsync(api, token, pairs);
            if (json?.posts == null) return null;
            var imgs = new MoeItems();
            foreach (var post in json.posts)
            {
                var img = new MoeItem(this, para);
                var rating = post.rating;
                if ($"{rating}" == "e") img.IsExplicit = true;
                img.Id = $"{post.pid}".ToInt();
                img.Width = $"{post.width}".ToInt();
                img.Height = $"{post.height}".ToInt();
                img.Urls.Add(new UrlInfo("缩略图", 1, $"https://i.yuriimg.com/{post.src}"));
                img.DetailUrl = $"{HomeUrl}/show/{post.id}";
                img.GetDetailTaskFunc = async () => await GetDetailTask(img, $"{post.id}", token);
                img.OriginString = $"{post}";
                imgs.Add(img);
            }

            return imgs;
        }
    }
}
