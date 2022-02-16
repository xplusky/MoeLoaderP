namespace MoeLoaderP.Core.Sites;

/// <summary>
///     atfbooru.ninja fixed 2021.2.8
/// </summary>
public class AtfbooruSite : BooruSite
{
    public override string HomeUrl => "https://booru.allthefallen.moe/";
    public override string DisplayName => "Atfbooru";
    public override string ShortName => "atfbooru";

    public override SiteTypeEnum SiteType => SiteTypeEnum.Json;

    public override string GetHintQuery(SearchPara para)
    {
        return $"{HomeUrl}/tags/autocomplete.json?search%5Bname_matches%5D={para.Keyword.ToEncodedUrl()}";
    }

    public override string GetPageQuery(SearchPara para)
    {
        return
            $"{HomeUrl}/posts.json?page={para.PageIndex}&limit={para.CountLimit}&tags={para.Keyword.ToEncodedUrl()}";
    }
}