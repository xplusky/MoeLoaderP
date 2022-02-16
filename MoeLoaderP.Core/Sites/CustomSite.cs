using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     自定义站点
/// </summary>
public class CustomSite : MoeSite
{
    public CustomSite(CustomSiteConfig config)
    {
        CustomConfig = config;
        Config = config.Config;
        LoginPageUrl = config.LoginUrl;


        DownloadTypes.Add("原图", DownloadTypeEnum.Origin);
        
        Lv2Cat = new Categories(Config);
        if (CustomConfig.Config.IsSupportAccount)
        {
            if (CustomConfig.LoginUrl == null) LoginPageUrl = config.HomeUrl;
        }
    }


    public override bool VerifyCookieAndSave(CookieCollection ccol)
    {
        return CustomConfig.CookieLoginAuthKey == null || ccol.Any(cookie => cookie.Name.Equals(CustomConfig.CookieLoginAuthKey, StringComparison.OrdinalIgnoreCase));
    }

    public override void AfterInit()
    {
        Net ??= new NetOperator(Settings, this);
        _ = AddCat();
    }

    

    public void Login()
    {
        Net ??= new NetOperator(Settings, this);
        if (SiteSettings.LoginCookies != null)
        {
            Net.SetCookie(SiteSettings.GetCookieContainer());
        }

        IsUserLogin = true;
    }

    public async Task AddCat()
    {
        if (CustomConfig.CustomLv2MenuItems?.Count > 0)
        //if (false)
        {
            CustomConfig.Categories.Clear();
            //CustomConfig.Categories.Add("加载中..",null,null);
            //CatChange();
            //CustomConfig.Categories.Clear();
            //Lv2Cat.Add("加载中..");
            //CatChange();
            foreach (var item in CustomConfig.CustomLv2MenuItems)
            {
                var net = Net.CloneWithCookie();
                var url = item.PageUrl ?? CustomConfig.HomeUrl;
                var html = await net.GetHtmlAsync(url);
                var nodes = html.DocumentNode.SelectNodes(item.Menus.Path);
                foreach (var node in nodes)
                {
                    var cat = new CustomCategory();

                    try
                    {
                        var name = node.GetValue(item.MenuTitleFromMenus);
                        var url2 = node.GetValue(item.MenuUrlFromMenus);
                        if($"{url2}".Equals(HomeUrl, StringComparison.OrdinalIgnoreCase)) continue;
                        cat.Name = name;
                        if(cat.Name.Equals("首页")) continue;
                        cat.FirstPageApi = $"{url2}{item.FirstApi}";
                        var follow = $"{url2}{item.FollowApi}";
                        if (item.FollowApiReplaceFrom != null)
                        {
                            follow = follow.Replace(item.FollowApiReplaceFrom, item.FollowApiReplaceTo);
                        }
                        cat.FollowUpPageApi = $"{follow}";
                        if(item.OverridePagePara !=null) cat.OverridePagePara = item.OverridePagePara;
                        CustomConfig.Categories.Add(cat);
                    }
                    catch (Exception e)
                    {
                        Ex.Log(e);
                    }
                }

            }
        }
        //Lv2Cat.Clear();

        foreach (var cat in CustomConfig.Categories) Lv2Cat.Add(cat.Name);
        CatChange();
    }

    public override string HomeUrl => CustomConfig.HomeUrl;
    public override string DisplayName => CustomConfig.DisplayName;
    public override string ShortName => CustomConfig.ShortName;

    public override Uri Icon => CustomConfig.SiteIconUrl != null
        ? new Uri(CustomConfig.SiteIconUrl)
        : new Uri("/MoeLoaderP;component/Assets/SiteIcon/default.png", UriKind.RelativeOrAbsolute);

    public CustomSiteConfig CustomConfig { get; set; }

    public override async Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
    {
        if (!IsUserLogin) Login();
        var net = Net.CloneWithCookie();
        var cat = CustomConfig.Categories[para.Lv2MenuIndex];
        var api = para.PageIndex <= 1 ? cat.FirstPageApi : cat.FollowUpPageApi;
        if (!para.Keyword.IsEmpty() && cat.OverrideSearchApi == null)
        {
            api = CustomConfig.SearchApi.Replace("{keyword}", para.Keyword.ToEncodedUrl());
        }
        var rapi = api.Replace("{pagenum}", $"{para.PageIndex}").Replace("{pagenum-1}", $"{para.PageIndex - 1}");
        var html = await net.GetHtmlAsync(rapi, token: token);
        if (html == null) return null;
        var moes = new SearchedPage();
        moes.OriginString = new StringBuilder(html.Text);
        var pa = cat.OverridePagePara ?? CustomConfig.PagePara;
        var list = (HtmlNodeCollection) html.DocumentNode.GetValue(pa.ImagesList);
        
        if (!(list?.Count > 0)) return moes;
        foreach (var item in list)
        {
            var moe = new MoeItem(this, para);
            var url = item.GetValue(pa.ImageItemThumbnailUrlFromImagesList);
            if (url == null) continue;
            var refer = pa.ImageItemThumbnailUrlFromImagesList.Referer;
            moe.Urls.Add(DownloadTypeEnum.Thumbnail, url, refer);
            moe.Title = item.GetValue(pa.ImageItemTitleFromImagesList);
            var detail = item.GetValue(pa.ImageItemDetailUrlFromImagesList);
            moe.DetailUrl = detail;
            moe.GetDetailTaskFunc += t => GetDetail(moe.DetailUrl, moe, pa, true, t);
            if (pa.ImageItemDateTimeFromImagesList != null)
            {
                var date = item.GetValue(pa.ImageItemDateTimeFromImagesList);
                moe.DateString = $"{date}";
            }

            moe.OriginString = item.InnerHtml;
            moes.Add(moe);
        }

        return moes;
    }

    public async Task GetDetail(string url, MoeItem father, CustomPagePara pa, bool isFirst, CancellationToken token)
    {
        var items = await GetNewItems(url, father, pa, isFirst, token);

        foreach (var moeItem in items) father.ChildrenItems.Add(moeItem);
        if (isFirst)
        {
            var urlinfo = father.ChildrenItems[0]?.DownloadUrlInfo;
            if (father.ChildrenItems.Count > 0 && urlinfo != null) father.Urls.Add(urlinfo);
        }
    }

    public async Task<MoeItems> GetNewItems(string url, MoeItem father, CustomPagePara pa, bool isFirst,
        CancellationToken token)
    {
        var newItems = new MoeItems();
        var net = Net.CloneWithCookie();
        var html = await net.GetHtmlAsync(url, token: token, showSearchMessage: false);
        if (html == null) return newItems;
        var root = html.DocumentNode;
        var imgOrImgs = root.GetValue(pa.DetailImagesList);
        if (pa.DetailImagesList.IsMultiValues)
        {
            var imgs = (HtmlNodeCollection) imgOrImgs;
            if (imgs?.Count > 0)
                foreach (var img in imgs)
                {
                    var newm = GetChildrenItem(img, pa, father);
                    newItems.Add(newm);
                }
            else return newItems;
        }
        else
        {
            var img = (HtmlNode) imgOrImgs;
            var newm = GetChildrenItem(img, pa, father);
            newItems.Add(newm);
        }


        var currentPageIndex = root.GetValue(pa.DetailCurrentPageIndex);
        var currentIndex = $"{currentPageIndex}".ToInt();
        var nextpageIndex = $"{root.GetValue(pa.DetailNextPageIndex)}".ToInt();
        //var maxPageIndex = $"{root.GetValue(pa.DetailMaxPageIndex)}".ToInt();
        if (isFirst)
        {
            var imageCount = $"{root.GetValue(pa.DetailImagesCount)}".ToInt();
            if (imageCount != 0) father.ChildrenItemsCount = imageCount;
        }

        if (nextpageIndex == currentIndex + 1)
        {
            var nextUrl = root.GetValue(pa.DetailNextPageUrl);
            var last = newItems.LastOrDefault();
            if (last != null)
            {
                last.IsResolveAndDownloadNextItem = true;
                last.GetNextItemsTaskFunc += async t => await GetNewItems(nextUrl, father, pa, false, t);
            }
        }

        return newItems;
    }

    public MoeItem GetChildrenItem(HtmlNode img, CustomPagePara pa, MoeItem father)
    {
        var newMoeitem = new MoeItem(this, father.Para);
        if (pa.DetailImageItemOriginUrlFromDetailImagesList != null)
        {
            var imgurl = img.GetValue(pa.DetailImageItemOriginUrlFromDetailImagesList);
            if (imgurl != null)
                newMoeitem.Urls.Add(DownloadTypeEnum.Origin, imgurl, referer: pa.DetailImageItemOriginUrlFromDetailImagesList.Referer);
        }

        if (pa.DetailImageItemThumbnailUrlFromDetailImagesList != null)
        {
            var imgurl = img.GetValue(pa.DetailImageItemThumbnailUrlFromDetailImagesList);
            if (imgurl != null)
                newMoeitem.Urls.Add(DownloadTypeEnum.Thumbnail, imgurl,
                    referer: pa.DetailImageItemThumbnailUrlFromDetailImagesList.Referer);
        }

        if (pa.DetailImageItemDetailUrlFromDetailImagesList != null)
        {
            var imgurl = img.GetValue(pa.DetailImageItemDetailUrlFromDetailImagesList);
            if (imgurl != null)
            {
                newMoeitem.DetailUrl = $"{imgurl}";
                var url = new UrlInfo(DownloadTypeEnum.Origin, "",
                    resolveUrlFunc: (item, url, token) => GetDetailLv2(pa, item, url, token));

                newMoeitem.Urls.Add(url);
            }
        }

        return newMoeitem;
    }

    public async Task GetDetailLv2(CustomPagePara pa, MoeItem currentItem, UrlInfo urlinfo, CancellationToken token)
    {
        var url = currentItem.DetailUrl;
        var net = Net.CloneWithCookie();
        var html = await net.GetHtmlAsync(url, null, false, token);
        if (pa.DetailLv2ImageOriginUrl != null)
        {
            var originUrl = html.DocumentNode.GetValue(pa.DetailLv2ImageOriginUrl);
            urlinfo.Url = $"{originUrl}";
        }

        if (pa.DetailLv2ImagePreviewUrl != null)
        {
            var prevUrl = html.DocumentNode.GetValue(pa.DetailLv2ImagePreviewUrl);
            currentItem.Urls.Add(DownloadTypeEnum.Medium, $"{prevUrl}");
        }
    }
}