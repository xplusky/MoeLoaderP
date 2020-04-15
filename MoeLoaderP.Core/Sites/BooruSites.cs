using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    // Booru 类型通用站点集合
    public class KonachanSite : BooruSite
    {
        public override string HomeUrl => "https://konachan.com";
        public override string DisplayName => "Konachan";
        public override string ShortName => "konachan";

        public override string GetHintQuery(SearchPara para)
        {
            var pairs = new Pairs
            {
                {"limit", "15"},
                {"order", "count"},
                {"name", para.Keyword}
            };
            return $"{HomeUrl}/tag.xml{pairs.ToPairsString()}";
        }

        public override string GetPageQuery(SearchPara para)
        {
            var pairs = new Pairs
            {
                {"page",$"{para.PageIndex}" },
                {"limit",$"{para.Count}" },
                {"tags",para.Keyword.ToEncodedUrl() }
            };
            return $"{HomeUrl}/post.xml{pairs.ToPairsString()}";
        }
    }

    public class YandeSite : BooruSite
    {
        public override string HomeUrl => "https://yande.re";
        public override string DisplayName => "Yande";
        public override string ShortName => "yande";

        public override string GetHintQuery(SearchPara para)
        {
            var pairs = new Pairs
            {
                {"limit","15" },
                {"order","count" },
                {"name",$"{para.Keyword.ToEncodedUrl()}" }
            };
            return $"{HomeUrl}/tag.xml{pairs.ToPairsString()}";
        }

        public override string GetPageQuery(SearchPara para)
        {
            var pairs = new Pairs
            {
                {"page", $"{para.PageIndex}"},
                {"limit", $"{para.Count}"},
                {"tags", para.Keyword.ToEncodedUrl()}
            };
            return $"{HomeUrl}/post.xml{pairs.ToPairsString()}";
        }
    }

    public class BehoimiSite : BooruSite
    {
        public override string HomeUrl => "http://behoimi.org";
        public override string DisplayName => "Behoimi";
        public override string ShortName => "behoimi";
        public override string GetThumbnailReferer(MoeItem item) => "http://behoimi.org/post";

        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/tag/index.xml?limit=8&order=count&name={para.Keyword}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/post/index.xml?page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";
    }

    public class SafebooruSite : BooruSite
    {
        public override string HomeUrl => "https://safebooru.org";
        public override string DisplayName => "Safebooru";
        public override string ShortName => "safebooru";
        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/index.php?page=dapi&s=tag&q=index&order=name&limit=8&name={para.Keyword.ToEncodedUrl()}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/index.php?page=dapi&s=post&q=index&pid={para.PageIndex - 1}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";

        public override string UrlPre => "";

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            var r = await base.GetRealPageImagesAsync(para, token);

            foreach (var item in r)
            {
                item.Urls[0].Url = item.Urls[0].Url.Replace(".png", ".jpg").Replace(".jpeg", ".jpg");
            }

            return r;
        }

        public override string GetDetailPageUrl(MoeItem item) => $"{HomeUrl}/index.php?page=post&s=view&id={item.Id}";
    }

    public class DonmaiSite : BooruSite
    {
        public override string HomeUrl => "https://danbooru.donmai.us";
        public override string DisplayName => "Donmai";
        public override string ShortName => "donmai";

        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/tags/autocomplete.json?search%5Bname_matches%5D={para.Keyword.ToEncodedUrl()}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/posts.json?page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";

        public override SiteTypeEnum SiteType => SiteTypeEnum.Json;

        public override string GetDetailPageUrl(MoeItem item)
            => $"{HomeUrl}/posts/{item.Id}";
    }

    public class LolibooruSite : BooruSite
    {
        public override string HomeUrl => "https://lolibooru.moe";
        public override string DisplayName => "Lolibooru";
        public override string ShortName => "lolibooru";

        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/tag.xml?limit=8&order=count&name={para.Keyword}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/post.xml?page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";

    }

    public class AtfbooruSite : BooruSite
    {
        public override string HomeUrl => "https://atfbooru.ninja";
        public override string DisplayName => "Atfbooru";
        public override string ShortName => "atfbooru";

        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/tags/autocomplete.json?search%5Bname_matches%5D={para.Keyword.ToEncodedUrl()}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/posts.json?page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";

        public override SiteTypeEnum SiteType => SiteTypeEnum.Json;
    }

    public class Rule34Site : BooruSite
    {
        public override string HomeUrl => "https://rule34.xxx";
        public override string DisplayName => "Rule34";
        public override string ShortName => "rule34";

        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/autocomplete.php?q={para.Keyword}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/index.php?page=dapi&s=post&q=index&pid={para.PageIndex - 1}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";
    }

    public class GelbooruSite : BooruSite
    {
        public override string HomeUrl => "https://gelbooru.com";
        public override string DisplayName => "Gelbooru";
        public override string ShortName => "gelbooru";

        public override string GetDetailPageUrl(MoeItem item) => $"{HomeUrl}/index.php?page=post&s=view&id={item.Id}";

        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/index.php?page=autocomplete&term={para.Keyword}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/index.php?page=dapi&s=post&q=index&pid={para.PageIndex - 1}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";
        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            var ahis = new AutoHintItems();
            var jsonlist = await new NetDocker(Settings).GetJsonAsync(GetHintQuery(para),token);
            foreach (var item in jsonlist)
            {
                ahis.Add(new AutoHintItem
                {
                    Word = $"{item}".Replace("\"",""),
                });
            }
            return ahis;
        }
    }
}
