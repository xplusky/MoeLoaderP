namespace MoeLoaderP.Core.Sites;

/// <summary>
///     behoimi.org fixed 2021.2.8
/// </summary>
public class BehoimiSite : BooruSite
{
    public override string HomeUrl => "http://behoimi.org";
    public override string DisplayName => "3dBooru";
    public override string ShortName => "3dBooru";

    public override string GetThumbnailReferer(MoeItem item)
    {
        return "http://behoimi.org/post";
    }

    public override string GetHintQuery(SearchPara para)
    {
        return $"{HomeUrl}/tag/index.xml?limit=8&order=count&name={para.Keyword}";
    }

    public override string GetPageQuery(SearchPara para)
    {
        return
            $"{HomeUrl}/post/index.xml?page={para.PageIndex}&limit={para.CountLimit}&tags={para.Keyword.ToEncodedUrl()}";
    }
}