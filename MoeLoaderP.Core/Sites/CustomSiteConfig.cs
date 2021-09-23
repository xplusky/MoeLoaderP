using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MoeLoaderP.Core.Sites
{
    public class CustomSiteConfig
    {
        public string ShortName { get; set; }
        public string DisplayName { get; set; }
        public string HomeUrl { get; set; }
        public string SiteIconUrl { get; set; }
        public CustomCategories Categories { get; set; } = new CustomCategories();
        public MoeSiteConfig Config { get; set; } = new MoeSiteConfig()
        {
            IsCustomSite = true
        };
        public CustomPagePara PagePara { get; set; } = new CustomPagePara();
        public string SearchApi { get; set; }
    }
    public class CustomSiteConfigList : ObservableCollection<CustomSiteConfig> { }

    public class CustomCategory : Category
    {
        public string FirstPageApi { get; set; }
        public string FollowUpPageApi { get; set; }
        public CustomPagePara OverridePagePara { get; set; }
        public string OverrideSearchApi { get; set; }
    }

    public class CustomCategories : List<CustomCategory>
    {
        public void AddRange(string first, string follow, params string[] content)
        {
            if (content.Length % 2 != 0) return;
            for (var i = 0; i < content.Length; i += 2)
            {
                var cat = new CustomCategory
                {
                    FirstPageApi = first.Replace("{name}", content[i]),
                    FollowUpPageApi = follow.Replace("{name}", content[i]),
                    Name = content[i + 1]
                };
                Add(cat);
            }
        }
    }
    
    public class CustomPagePara
    {
        // list
        public CustomXpath ImagesList { get; set; }

        // one item
        public CustomXpath ImageItemThumbnailUrl { get; set; }
        public CustomXpath ImageItemTitle { get; set; }
        public CustomXpath ImageItemDetailUrl { get; set; }
        public CustomXpath ImageItemDateTime { get; set; }
        public CustomXpath ImagesCount { get; set; }

        // detail page
        public CustomXpath DetailImagesList { get; set; }
        public CustomXpath DetailImageItemThumbnailUrl { get; set; }
        public CustomXpath DetailImageItemOriginUrl { get; set; }
        public CustomXpath DetailImageItemDetailUrl { get; set; }
        
        public CustomXpath DetailCurrentPageIndex { get; set; }
        public CustomXpath DetailNextPageIndex { get; set; }
        public CustomXpath DetailNextPageUrl { get; set; }
        public CustomXpath DetailMaxPageIndex { get; set; }
        public CustomXpath DetailImagesCount { get; set; }

        // detail lv2 page
        public CustomXpath DetailLv2ImageOriginUrl { get; set; }
        public CustomXpath DetailLv2ImagePreviewUrl { get; set; }
        public CustomXpath DetailLv2ImageDetailUrl { get; set; }

        // detail lv3 page
        public CustomXpath DetailLv3ImageOriginUrl { get; set; }
    }

    public class CustomXpath
    {
        public string Path { get; set; }
        public string PathR2 { get; set; }
        public CustomXpathMode Mode { get; set; }
        public string Attribute { get; set; }
        public string Pre { get; set; }
        public bool IsMultiValues { get; set; }
        public string RegexPattern { get; set; }
        public string Replace { get; set; }
        public string ReplaceTo { get; set; }

        public CustomXpath(string path, CustomXpathMode mode, string attribute = null, string pre = null,
            bool mul = false, string regex = null, string replace = null, string replaceTo = null, string pathR2 = null)
        {
            Path = path;
            Mode = mode;
            if (attribute != null) Attribute = attribute;
            if (pre != null) Pre = pre;
            if (mul) IsMultiValues = true;
            if (regex != null) RegexPattern = regex;
            if (replace != null) Replace = replace;
            if (replaceTo != null) ReplaceTo = replaceTo;
            if (pathR2 != null) PathR2 = pathR2;
        }
    }

    public enum CustomXpathMode
    {
        Attribute,InnerText, Node
    }
}