using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     yuriimg.com
/// </summary>
public class YuriimgSite : MoeSite
{
    public YuriimgSite()
    {
        DownloadTypes.Add("预览图", DownloadTypeEnum.Medium);
        DownloadTypes.Add("原图", DownloadTypeEnum.Origin);
        Config = new MoeSiteConfig
        {
            IsSupportKeyword = true,
            IsSupportRating = true,
            IsSupportResolution = true,
            IsSupportScore = true
        };
    }

    public override string HomeUrl => "http://yuriimg.com";
    public override string ShortName => "yuriimg";
    public override string DisplayName => "Yuriimg";

    /// <summary>
    ///     图片地址拼接  图片类型 0缩略图 1预览图 2原图
    /// </summary>
    private string TranslateImageUrl(dynamic json, int type = 0)
    {
        var fileExt = $"{json.file_ext}";
        var sb = new StringBuilder();
        sb.Append($"https://i.yuriimg.com/{json.src}/");
        sb.Append("yuriimg.com ");
        sb.Append($"{json.id} ");
        if (!fileExt.IsEmpty())
            switch (type)
            {
                case 1:
                    sb.Append("thumb");
                    break;
                case 2:
                    sb.Append($"{fileExt}");
                    break;
                default:
                    sb.Append("contain");
                    break;
            }
        else
            sb.Append("contain");

        sb.Append($"{(json.page == null ? string.Empty : $" p{$"{json.page}".ToInt() + 1}")}");
        sb.Append($".{(fileExt.IsEmpty() ? "webp" : fileExt)}");
        return sb.ToString();
    }

    public async Task GetDetailTask(MoeItem img, string id, CancellationToken token = new())
    {
        var api = $"https://api.yuriimg.com/post/{id}";
        var json = await Net.GetJsonAsync(api, token: token, showSearchMessage: false);
        if (json == null) return;
        img.Score = $"{json.praise}".ToInt();
        img.Date = $"{json.format_date}".ToDateTime();
        img.Urls.Add(DownloadTypeEnum.Medium, TranslateImageUrl(json, 1));
        img.Urls.Add(DownloadTypeEnum.Origin, TranslateImageUrl(json, 2));
        img.Artist = $"{json.artist?.name}";
        img.Uploader = $"{json.user?.name}";
        img.UploaderId = $"{json.user?.id}";

        foreach (var tag in Ex.GetList(json.tags.general))
        {
            var t = $"{tag.tags?.cn}";
            if (!t.IsEmpty()) img.Tags.Add(t);
        }

        if ($"{json.page_count}".ToInt() > 1)
        {
            var q = $"{api}/multi";
            var json2 = await Net.GetJsonAsync(q, token: token, showSearchMessage: false);

            var child1 = new MoeItem(this, img.Para);
            child1.Width = img.Width;
            child1.Height = img.Height;
            foreach (var urlInfo in img.Urls) child1.Urls.Add(urlInfo);
            img.ChildrenItems.Add(child1);

            foreach (var jitem in Ex.GetList(json2))
            {
                var childImg = new MoeItem(this, img.Para);
                childImg.Width = $"{jitem.width}".ToInt();
                childImg.Height = $"{jitem.height}".ToInt();
                //childImg.Urls.Add(4, $"https://i.yuriimg.com/{post.src}/yuriimg.com%20{post.id}%20contain.jpg");
                //childImg.Urls.Add(4,null,null,null, ResolveUrlFunc);
                img.ChildrenItems.Add(childImg);
            }
        }
    }


    public override async Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
    {
        Net ??= new NetOperator(Settings, this);
        const string api = "https://api.yuriimg.com/posts";
        var pairs = new Pairs
        {
            {"page", $"{para.PageIndex}"},
            {"limit", $"{para.CountLimit}"},
            {"tags", para.Keyword.ToEncodedUrl()}
        };
        var json = await Net.GetJsonAsync(api, pairs, token: token);
        if (json?.posts == null) return null;
        var imgs = new SearchedPage();
        foreach (var post in json.posts)
        {
            var img = new MoeItem(this, para);
            img.IsNsfw = $"{post.rating}" == "e";
            if (SiteSettings.GetCookieContainer() == null && img.IsNsfw) continue;
            img.Id = $"{post.pid}".ToInt();
            img.Sid = $"{post.id}";
            img.Width = $"{post.width}".ToInt();
            img.Height = $"{post.height}".ToInt();
            img.Urls.Add(DownloadTypeEnum.Thumbnail,
                $"https://i.yuriimg.com/{post.src}/yuriimg.com%20{post.id}%20contain.jpg");
            img.DetailUrl = $"{HomeUrl}/show/{post.id}";
            img.GetDetailTaskFunc = async t => await GetDetailTask(img, $"{post.id}", t);
            img.OriginString = $"{post}";

            imgs.Add(img);
        }

        return imgs;
    }
}