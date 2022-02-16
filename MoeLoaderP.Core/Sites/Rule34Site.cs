namespace MoeLoaderP.Core.Sites;

/// <summary>
///     rule34.xxx fixed 2021/2/8
/// </summary>
public class Rule34Site : BooruSite
{
    public override string HomeUrl => "https://rule34.xxx";
    public override string DisplayName => "Rule34";
    public override string ShortName => "rule34";

    public override string GetHintQuery(SearchPara para)
    {
        return $"{HomeUrl}/autocomplete.php?q={para.Keyword}";
    }

    public override string GetPageQuery(SearchPara para)
    {
        return
            $"{HomeUrl}/index.php?page=dapi&s=post&q=index&pid={para.PageIndex - 1}&limit={para.CountLimit}&tags={para.Keyword.ToEncodedUrl()}";
    }
}