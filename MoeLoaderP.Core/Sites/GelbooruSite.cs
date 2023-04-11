using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     fixed 2021.5.13
/// </summary>
public class GelbooruSite : BooruSite
{
    public override string HomeUrl => "https://gelbooru.com";
    public override string DisplayName => "Gelbooru";
    public override string ShortName => "gelbooru";

    public GelbooruSite()
    {
        Config.IsSupportAccount = true;
        LoginPageUrl = "https://gelbooru.com/index.php?page=account&s=login&code=00";
    }
    public override bool VerifyCookie(CookieCollection ccol)
    {
        foreach (Cookie cookie in ccol)
        {
            if (cookie.Name.Equals("user_id", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
    public override SiteTypeEnum SiteType => SiteTypeEnum.Xml2;

    public override Func<MoeItem, SearchPara, CancellationToken, Task> GetDetailTaskFunc { get; set; } = GetDetailTask;

    public override string GetDetailPageUrl(MoeItem item)
    {
        return $"{HomeUrl}/index.php?page=post&s=view&id={item.Id}";
    }

    public override string GetHintQuery(SearchPara para)
    {
        return $"{HomeUrl}/index.php?page=autocomplete2&term={para.Keyword}&type=tag_query&limit=10";
    }

    public override string GetPageQuery(SearchPara para)
    {
        var r18 = para.IsShowExplicit
            ? (para.IsShowExplicitOnly ? "%20rating%3Aexplicit" : "")
            : "%20rating%3Ageneral";
        return
            $"{HomeUrl}/index.php?page=dapi&s=post&q=index&pid={para.PageIndex - 1}&limit={para.CountLimit}&tags={para.Keyword.ToEncodedUrl()}{r18}";
    }

    public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
    {
        var ahis = new AutoHintItems();
        var jsonlist = await new NetOperator(Settings, this).GetJsonAsync(GetHintQuery(para), token: token);
        foreach (var item in Ex.GetList(jsonlist))
            ahis.Add(new AutoHintItem
            {
                Word = $"{item.value}",
                Count = $"{item.post_count}"
            });
        return ahis;
    }

    public static async Task GetDetailTask(MoeItem img, SearchPara para, CancellationToken token)
    {
        var url = img.DetailUrl;
        var net = new NetOperator(img.Site.Settings, img.Site);
        var html = await net.GetHtmlAsync(url, null, false, token);
        if (html == null) return;
        var nodes = html.DocumentNode;
        img.Artist = nodes.SelectSingleNode("*//li[@class='tag-type-artist']/a[2]")?.InnerText.Trim();
        img.Character = nodes.SelectSingleNode("*//li[@class='tag-type-character']/a[2]")?.InnerText.Trim();
        img.Copyright = nodes.SelectSingleNode("*//li[@class='tag-type-copyright']/a[2]")?.InnerText.Trim();
    }
}