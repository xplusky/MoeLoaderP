using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// 自定义站点
    /// </summary>
    public class CustomSite : MoeSite
    {
        public override string HomeUrl => Set.HomeUrl;
        public override string DisplayName => Set.DisplayName;
        public override string ShortName => Set.ShortName;
        public override Uri Icon => Set.SiteIconUrl != null ? new Uri(Set.SiteIconUrl) : new Uri("/MoeLoaderP;component/Assets/SiteIcon/default.png", UriKind.RelativeOrAbsolute);
        public IndividualCustomSiteSettings Set { get; set; }

        public CustomSite(IndividualCustomSiteSettings set)
        {
            Set = set;
            SupportState = set.SupportState;
            foreach (var cat in set.Categories)
            {
                SubCategories.Add(new Category(cat.Name));
            }
            DownloadTypes.Add("原图", 4);
        }

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            Net ??= new NetOperator(Settings, HomeUrl);
            var net = Net.CreateNewWithOldCookie();
            var cat = Set.Categories[para.SubMenuIndex];
            var api = para.PageIndex <= 1 ? cat.FirstPageApi : cat.FollowUpPageApi;
            var rapi = api.Replace("{pagenum}", $"{para.PageIndex}").Replace("{pagenum-1}", $"{para.PageIndex-1}");
            var html = await net.GetHtmlAsync(rapi, token);
            if (html == null) return null;
            var pa = cat.OverridePagePara ?? Set.PagePara;
            var list = (HtmlNodeCollection)html.DocumentNode.GetValue(pa.ImagesList);
            var moes = new MoeItems();
            foreach (var item in list)
            {
                var moe = new MoeItem(this, para);
                var url = item.GetValue(pa.ImageItemThumbnailUrl);
                if(url == null)continue;
                moe.Urls.Add( DownloadTypeEnum.Thumbnail , url);
                moe.Title = item.GetValue(pa.ImageItemTitle);
                var detail = item.GetValue(pa.ImageItemDetailUrl);
                moe.DetailUrl = detail;
                moe.GetDetailTaskFunc += () => GetDetail(moe.DetailUrl,moe,pa,true, token);
                moe.Net = Net.CreateNewWithOldCookie();
                moe.Net.SetTimeOut(30);
                if (pa.ImageItemDateTime != null)
                {
                    var date = item.GetValue(pa.ImageItemDateTime);
                    moe.DateString = $"{date}";
                }

                moe.OriginString = item.InnerHtml;
                moes.Add(moe);
            }
            
            return moes;
        }

        public async Task GetDetail(string url,MoeItem father,CustomPagePara pa, bool isFirst,CancellationToken token)
        {
            var items = await GetNewItems( url,  father,  pa,  isFirst,  token);

            foreach (var moeItem in items)
            {
                father.ChildrenItems.Add(moeItem);
            }
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
            var net = Net.CreateNewWithOldCookie();
            var html = await net.GetHtmlAsync(url, token);
            if (html == null) return newItems;
            var root = html.DocumentNode;
            var imgOrImgs = root.GetValue(pa.DetailImagesList);
            if (pa.DetailImagesList.IsMultiValues)
            {
                var imgs = (HtmlNodeCollection)imgOrImgs;
                if (imgs?.Count > 0)
                {
                    foreach (var img in imgs)
                    {
                        var newm = GetChildrenItem(img, pa, father, isFirst);
                        newItems.Add(newm);
                    }
                }
                else return newItems;
            }
            else
            {
                var img = (HtmlNode)imgOrImgs;
                var newm = GetChildrenItem(img, pa, father, isFirst);
                newItems.Add(newm);
            }


            var currentPageIndex = root.GetValue(pa.DetailCurrentPageIndex);
            var currentIndex = $"{currentPageIndex}".ToInt();
            var nextpageIndex = $"{root.GetValue(pa.DetailNextPageIndex)}".ToInt();
            var maxPageIndex = $"{root.GetValue(pa.DetailMaxPageIndex)}".ToInt();
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
                    last.GetNextItemsTaskFunc += async (t) => await GetNewItems(nextUrl, father, pa, false, t);
                }


            }

            return newItems;

        }

        public MoeItem GetChildrenItem(HtmlNode img,CustomPagePara pa,MoeItem father,bool isFirst)
        {
            var newMoeitem = new MoeItem(this, father.Para);
            if (pa.DetailImageItemOriginUrl != null)
            {
                var imgurl = img.GetValue(pa.DetailImageItemOriginUrl);
                if (imgurl != null) newMoeitem.Urls.Add(DownloadTypeEnum.Origin, imgurl);
            }

            if (pa.DetailImageItemThumbnailUrl != null)
            {
                var imgurl = img.GetValue(pa.DetailImageItemThumbnailUrl);
                if (imgurl != null) newMoeitem.Urls.Add(DownloadTypeEnum.Thumbnail, imgurl);
            }

            if (pa.DetailImageItemDetailUrl != null)
            {
                var imgurl = img.GetValue(pa.DetailImageItemDetailUrl);
                if (imgurl != null)
                {
                    newMoeitem.DetailUrl = $"{imgurl}";
                    var url = new UrlInfo(DownloadTypeEnum.Origin, "",
                        resolveUrlFunc: (item,url, token) => GetDetailLv2(pa, item, url,token));

                    newMoeitem.Urls.Add(url);
                }
            }

            return newMoeitem;

        }

        public async Task GetDetailLv2(CustomPagePara pa,MoeItem currentItem,UrlInfo urlinfo, CancellationToken token)
        {
            var url = currentItem.DetailUrl;
            var net = Net.CreateNewWithOldCookie();
            var html = await net.GetHtmlAsync(url, token);
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
}
