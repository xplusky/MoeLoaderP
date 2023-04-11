using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     danbooru.donmai.us fixed 2022.1.26
/// </summary>
public class DanbooruSite : BooruSite
{
    public override string HomeUrl => "https://danbooru.donmai.us";
    public override string DisplayName => "Danbooru";
    public override string ShortName => "danbooru";

    public override SiteTypeEnum SiteType => SiteTypeEnum.Json;

    public DanbooruSite()
    {
        Config.IsSupportAccount = true;
        Config.ImageOrders.Add("最热", ImageOrderBy.Popular);
        LoginPageUrl = "https://danbooru.donmai.us/login";
    }
    public override bool VerifyCookie(CookieCollection ccol)
    {
        foreach (Cookie cookie in ccol)
        {
            if (cookie.Name.Equals("_danbooru2_session", StringComparison.OrdinalIgnoreCase))
            {
                SiteSettings.SetSetting("_danbooru2_session", cookie.Value);
                return true;
            }
        }

        return false;
    }

    public override string GetHintQuery(SearchPara para)
    {
        return
            $"{HomeUrl}/autocomplete?search%5Bquery%5D={para.Keyword.ToEncodedUrl()}&search%5Btype%5D=tag_query&version=1&limit=20";
    }

    public override string GetPageQuery(SearchPara para)
    {
        var r18 = para.IsShowExplicit
            ? (para.IsShowExplicitOnly ? "%20rating:explicit" : "")
            : "%20rating:safe";
        return
            $"{HomeUrl}/posts.json?page={para.PageIndex}&limit={para.CountLimit}&tags={para.Keyword.ToEncodedUrl()}{r18}";
    }

    public override string GetDetailPageUrl(MoeItem item)
    {
        return $"{HomeUrl}/posts/{item.Id}";
    }

    public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
    {
        var list = new AutoHintItems();
        if (Net == null) Login();
        if (Net == null) return null;
        var net = Net.CloneWithCookie();
        var json = await net.GetJsonAsync(GetHintQuery(para), token: token);
        foreach (var item in Ex.GetList(json))
        {
            list.Add(new AutoHintItem
            {
                Word = $"{item.value}",
                Count = $"{item.post_count}"
            });
        }
        return list;
    }
}