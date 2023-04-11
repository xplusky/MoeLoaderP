namespace MoeLoaderP.Core.Sites;

/// <summary>
///     lolibooru.moe fixed 2021.2.8
/// </summary>
public class LolibooruSite : BooruSite
{
    public override string HomeUrl => "https://lolibooru.moe";
    public override string DisplayName => "Lolibooru";
    public override string ShortName => "lolibooru";

    public override string GetHintQuery(SearchPara para)
    {
        return $"{HomeUrl}/tag.xml?limit=8&order=count&name={para.Keyword}";
    }

    public override string GetPageQuery(SearchPara para)
    {
        var r18 = para.IsShowExplicit
            ? (para.IsShowExplicitOnly ? "%20rating:explicit" : "")
            : "%20rating:safe";
        return
            $"{HomeUrl}/post.xml?page={para.PageIndex}&limit={para.CountLimit}&tags={para.Keyword.ToEncodedUrl()}{r18}";
    }
}