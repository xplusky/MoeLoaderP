using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     safebooru.org fixed 2021.2.8
/// </summary>
public class SafebooruSite : BooruSite
{
    public override string HomeUrl => "https://safebooru.org";
    public override string DisplayName => "Safebooru";
    public override string ShortName => "safebooru";

    public override string UrlPre => "";

    public SafebooruSite()
    {
        Config.IsSupportRating = false;
    }

    public override string GetHintQuery(SearchPara para)
    {
        return $"{HomeUrl}/index.php?page=dapi&s=tag&q=index&order=name&limit=8&name={para.Keyword.ToEncodedUrl()}";
    }

    public override string GetPageQuery(SearchPara para)
    {
        return
            $"{HomeUrl}/index.php?page=dapi&s=post&q=index&pid={para.PageIndex - 1}&limit={para.CountLimit}&tags={para.Keyword.ToEncodedUrl()}";
    }

    public override async Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
    {
        var r = await base.GetRealPageAsync(para, token);

        foreach (var item in r) item.Urls[0].Url = item.Urls[0].Url.Replace(".png", ".jpg").Replace(".jpeg", ".jpg");

        return r;
    }

    public override string GetDetailPageUrl(MoeItem item)
    {
        return $"{HomeUrl}/index.php?page=post&s=view&id={item.Id}";
    }
}