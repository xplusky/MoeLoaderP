using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     danbooru.donmai.us fixed 2022.1.26
/// </summary>
public class DonmaiSite : BooruSite
{
    public override string HomeUrl => "https://danbooru.donmai.us";
    public override string DisplayName => "Donmai";
    public override string ShortName => "donmai";

    public override SiteTypeEnum SiteType => SiteTypeEnum.Json;

    public DonmaiSite()
    {
        Config.IsSupportAccount = true;
        Config.ImageOrders.Add("最热", ImageOrderBy.Popular);
        LoginPageUrl = "https://danbooru.donmai.us/login?url=%2F";
    }
    public override bool VerifyCookieAndSave(CookieCollection ccol)
    {
        return ccol.Any(cookie => cookie.Name.Equals("cf_clearance", StringComparison.OrdinalIgnoreCase));
    }

    public override string GetHintQuery(SearchPara para)
    {
        return
            $"{HomeUrl}/autocomplete.json?search%5Bquery%5D={para.Keyword.ToEncodedUrl()}&search%5Btype%5D=tag_query&limit=10";
    }

    public override string GetPageQuery(SearchPara para)
    {
        return
            $"{HomeUrl}/posts.json?page={para.PageIndex}&limit={para.CountLimit}&tags={para.Keyword.ToEncodedUrl()}";
    }

    public override string GetDetailPageUrl(MoeItem item)
    {
        return $"{HomeUrl}/posts/{item.Id}";
    }

    public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
    {
        var list = new AutoHintItems();
        var net = new NetOperator(Settings, this);
        var json = await net.GetJsonAsync(GetHintQuery(para), token: token);
        foreach (var item in Ex.GetList(json))
            list.Add(new AutoHintItem
            {
                Word = $"{item.value}",
                Count = $"{item.post_count}"
            });
        return list;
    }
}