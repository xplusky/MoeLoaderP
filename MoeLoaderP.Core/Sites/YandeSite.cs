using System;
using System.Linq;
using System.Net;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     yande.re fixed 2021.1.1
/// </summary>
public class YandeSite : BooruSite
{
    public override string HomeUrl => "https://yande.re";
    public override string DisplayName => "Yande";
    public override string ShortName => "yande";

    public YandeSite()
    {
        Config.IsSupportAccount = true;
        LoginPageUrl = "https://yande.re/user/login";
    }
    public override bool VerifyCookie(CookieCollection ccol)
    {
        return ccol.Any(cookie => cookie.Name.Equals("user_id", StringComparison.OrdinalIgnoreCase));
    }
    public override string GetHintQuery(SearchPara para)
    {
        var pairs = new Pairs
        {
            {"limit", "15"},
            {"order", "count"},
            {"name", $"{para.Keyword.ToEncodedUrl()}"}
        };
        return $"{HomeUrl}/tag.xml{pairs.ToPairsString()}";
    }

    public override string GetPageQuery(SearchPara para)
    {
        var r18 = "";
        if (!para.IsShowExplicit)
        {
            r18 = "%20rating%3As";

        }
        var pairs = new Pairs
        {
            {"page", $"{para.PageIndex}"},
            {"limit", $"{para.CountLimit}"},
            {"tags", $"{para.Keyword.ToEncodedUrl()}{r18}" }
        };

        return $"{HomeUrl}/post.xml{pairs.ToPairsString()}";
    }
}