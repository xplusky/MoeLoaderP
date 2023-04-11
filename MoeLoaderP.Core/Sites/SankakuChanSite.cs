using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     chan.sankakucomplex.com
/// </summary>
public class SankakuChanSite : MoeSite
{
    public string Api = "https://capi-v2.sankakucomplex.com";
    public string BetaApi = "https://sankaku.app";

    public SankakuChanSite()
    {
        DownloadTypes.Add("原图", DownloadTypeEnum.Origin);
        LoginPageUrl = "https://login.sankakucomplex.com/login";


        Config.IsSupportKeyword = true;
        Config.IsSupportRating = true;
        Config.IsSupportResolution = true;
        Config.IsSupportScore = true;
        Config.IsSupportAccount = true;
        Config.IsSupportStarButton = true;
        Config.IsSupportMultiKeywords = true;
        Config.ImageOrders.Add(new ImageOrder {Name = "最新", Order = ImageOrderBy.Date});
        Config.ImageOrders.Add(new ImageOrder {Name = "最受欢迎", Order = ImageOrderBy.Popular});

        Lv2Cat = new Categories(Config);
        Lv2Cat.Adds("最新/搜索", "收藏");
    }

    public override string HomeUrl => "https://sankaku.app";

    public override string DisplayName => "SankakuComplex[Chan]";

    public override string ShortName => "sankakucomplex-chan";
    private string AccessToken => SiteSettings.GetSetting("accessToken");

    public override string[] GetCookieUrls()
    {
        return new[] {"https://sankaku.app", "https://beta.sankakucomplex.com"};
    }

    public override void Logout()
    {
        SiteSettings.Items.Remove("accessToken");
        base.Logout();
    }

    public override async Task<bool> StarAsync(MoeItem moe, CancellationToken token)
    {
        var net = CloneNet();
        net.SetReferer(BetaApi);
        var res = await net.PostAsync($"https://capi-v2.sankakucomplex.com/posts/{moe.Id}/favorite?lang=en",
            token: token);
        var job = res as JObject;
        var b = job?["success"]?.Value<bool?>();

        if (b == true)
        {
            moe.IsFav = true;
            var count = job["score"]?.Value<int>();
            if (count != null) moe.FavCount = count.Value;
            return true;
        }

        return false;
    }

    public void Login()
    {
        Net = new NetOperator(Settings, this);
        var cc = SiteSettings.GetCookieContainer();
        if (cc != null) Net.SetCookie(cc);
        IsUserLogin = AccessToken != null;
    }

    public NetOperator CloneNet()
    {
        var net = Net.CloneWithCookie();
        if (!AccessToken.IsEmpty()) net.Client.DefaultRequestHeaders.Add("authorization", $"Bearer {AccessToken}");

        return net;
    }

    public override bool VerifyCookie(CookieCollection ccol)
    {
        foreach (Cookie cookie in ccol)
            if (cookie.Name.Equals("accessToken", StringComparison.OrdinalIgnoreCase))
            {
                SiteSettings.SetSetting("accessToken", cookie.Value);
                return true;
            }

        return false;
    }

    public string ConbimeMultiKeywords(params string[] kws)
    {
        var s = "";
        foreach (var kw in kws)
            if (!kw.IsEmpty())
                s += kw + "+";

        return s[..^1];
    }


    public override async Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
    {
        return await GetNewAndTagAsync(para, token);
    }

    public async Task<SearchedPage> GetNewAndTagAsync(SearchPara para, CancellationToken token)
    {
        var page = new SearchedPage();
        if (Net == null) Login();
        var net = CloneNet();
        net.SetReferer(BetaApi);

        var safekw = ConbimeMultiKeywords("rating:safe", para.Keyword.Trim().Replace(" ", "_"));
        var explicitkw = para.Keyword.Trim().Replace(" ", "_");

        var kw = para.IsShowExplicit == false 
            ?  safekw
            : (para.IsShowExplicitOnly ? ConbimeMultiKeywords("rating:e", para.Keyword.Trim().Replace(" ", "_")) : explicitkw);

        var pairs = new Pairs
        {
            {"lang", "en"},
            {"next", $"{para.PageIndexCursor}"},
            {"limit", $"{para.CountLimit}"},
            {"hide_posts_in_books", "in-larger-tags"},
            {"default_threshold", "1"}
        };

        if (para.Lv2MenuIndex == 1)
        {
            var meapi = "https://capi-v2.sankakucomplex.com/users/me?lang=en";
            var menet = CloneNet();
            var mejson = await menet.GetJsonAsync(meapi, token: token);
            var username = mejson?.user?.name;
            if (username != null)
            {
                pairs.Add("tags", $"Fav:{username}");
            }
            else
            {
                Ex.ShowMessage("无法获取Username");
                return null;
            }
        }
        else
        {
            string kw2;
            if (para.MultiKeywords.Count > 0)
            {
                var list = new List<string>();
                if (!kw.IsEmpty()) list.Add(kw);

                foreach (var k in para.MultiKeywords) list.Add(k.Replace(" ", "_"));
                kw2 = ConbimeMultiKeywords(list.ToArray());
            }
            else
            {
                kw2 = kw;
            }

            pairs.Add("tags", kw2);
        }

        var json = await net.GetJsonAsync($"{Api}/posts/keyset", pairs, token: token);
        if (json == null) return null;
        //if($"{json.suce}")
        page.NextPageIndexCursor = $"{json.meta?.next}";
        foreach (var jitem in Ex.GetList(json.data))
        {
            var img = new MoeItem(this, para)
            {
                Id = $"{jitem.id}".ToInt(),
                Width = $"{jitem.width}".ToInt(),
                Height = $"{jitem.height}".ToInt(),
                Score = $"{jitem.total_score}".ToInt()
            };
            img.Urls.Add(1, $"{jitem.preview_url}", BetaApi);
            img.Urls.Add(2, $"{jitem.sample_url}", BetaApi);
            img.Urls.Add(4, $"{jitem.file_url}", $"{BetaApi}/post/show/{img.Id}");
            img.IsNsfw = $"{jitem.rating}" != "s";
            img.Date = $"{jitem.created_at?.s}".ToDateTime();
            img.Uploader = $"{jitem.author?.name}";
            img.DetailUrl = $"{BetaApi}/post/show/{img.Id}";
            img.IsFav = $"{jitem.is_favorited}".ToLower() == "true";
            img.FavCount = $"{jitem.fav_count}".ToInt();
            if ($"{jitem.redirect_to_signup}".ToLower() == "true") img.Tip = "此图片需要登录查看";

            foreach (var tag in Ex.GetList(jitem.tags)) img.Tags.Add($"{tag.name_en}");

            img.OriginString = $"{jitem}";

            page.Add(img);
        }

        return page;
    }

    public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
    {
        var ahitems = new AutoHintItems();
        const string api = "https://capi-v2.sankakucomplex.com";
        if (Net == null) Login();
        var net = CloneNet();
        var pairs = new Pairs
        {
            {"lang", "en"},
            {"tag", para.Keyword.Trim().ToEncodedUrl()},
            {"target", "post"},
            {"show_meta", "1"}
        };
        var json = await net.GetJsonAsync($"{api}/tags/autosuggestCreating", pairs, token: token);
        foreach (var jitem in Ex.GetList(json))
        {
            var ahitem = new AutoHintItem();
            ahitem.Word = $"{jitem.name}";
            ahitem.Count = $"{jitem.count}";
            ahitems.Add(ahitem);
        }

        return ahitems;
    }
}