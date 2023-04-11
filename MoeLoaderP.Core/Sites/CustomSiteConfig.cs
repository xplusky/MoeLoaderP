using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     自定义站点配置
/// </summary>
public class CustomSiteConfig
{
    public string ShortName { get; set; }
    public string DisplayName { get; set; }
    public string HomeUrl { get; set; }
    public string SiteIconUrl { get; set; }

    public string LoginUrl { get; set; }
    public string CookieLoginAuthKey { get; set; }
    public CustomCategories Categories { get; set; } = new();

    public MoeSiteConfig Config { get; set; } = new()
    {
        IsCustomSite = true
    };

    public CustomPagePara PagePara { get; set; } = new();
    public string SearchApi { get; set; }

    public ObservableCollection<CustomLv2MenuItem> CustomLv2MenuItems { get; set; }
}

public class CustomLv2MenuItem
{
    public CustomXpath Menus { get; set; }
    public CustomXpath MenuTitleFromMenus { get; set; }
    public CustomXpath MenuUrlFromMenus { get; set; }
    public string FirstApi { get; set; }
    public string FollowApi { get; set; }
    public string FollowApiReplaceFrom { get; set; }
    public string FollowApiReplaceTo { get; set; }
    public string PageUrl { get; set; }

    public CustomPagePara OverridePagePara { get; set; }
}

public class CustomSiteConfigList : ObservableCollection<CustomSiteConfig>
{
    public void Adds( CustomSiteConfig[] items)
    {
        foreach (var item in items)
        {
            Add(item);
        }
    }
}

public class CustomCategory : Category
{
    public string FirstPageApi { get; set; }
    public string FollowUpPageApi { get; set; }
    public CustomPagePara OverridePagePara { get; set; }
    public string OverrideSearchApi { get; set; }

    public CustomCategory() : base(null, null)
    {
        // todo
        
    }
}

public class CustomCategories : List<CustomCategory>
{
    
    public void Add(string name, string firstPageApi,string followUpPageApi, CustomPagePara overridePara = null)
    {
        Add(new CustomCategory()
        {
            Name = name,
            FirstPageApi = firstPageApi,
            FollowUpPageApi = followUpPageApi,
            OverridePagePara = overridePara,
            
        });
    }  
    public void AddRange(string first, string follow, Pairs pairs, CustomPagePara para = null)
    {
        foreach (var pair in pairs)
        {
            var cat = new CustomCategory()
            {
                Name = pair.Value,
                FirstPageApi = first.Replace("{name}", pair.Key),
                FollowUpPageApi = follow.Replace("{name}", pair.Key)
            };
            if (para != null) cat.OverridePagePara = para;
            Add(cat);
        }
    }
}

public class CustomPagePara
{
    // list
    public CustomXpath ImagesList { get; set; }

    // one item
    public CustomXpath ImageItemThumbnailUrlFromImagesList { get; set; }
    public CustomXpath ImageItemTitleFromImagesList { get; set; }
    public CustomXpath ImageItemDetailUrlFromImagesList { get; set; }
    public CustomXpath ImageItemDateTimeFromImagesList { get; set; }
    public CustomXpath ImagesCountFromImagesList { get; set; }

    // detail page
    public CustomXpath DetailImagesList { get; set; }
    public CustomXpath DetailImageItemThumbnailUrlFromDetailImagesList { get; set; }
    public CustomXpath DetailImageItemOriginUrlFromDetailImagesList { get; set; }
    public CustomXpath DetailImageItemDetailUrlFromDetailImagesList { get; set; }

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
    public CustomXpath(string path, CustomXpathMode mode, string attribute = null, string pre = null,
        bool mul = false, string regex = null, string replace = null, string replaceTo = null, string pathR2 = null,
        string referer = null,string after = null)
    {
        Path = path;
        Mode = mode.ToString();
        if (attribute != null) Attribute = attribute;
        if (pre != null) Pre = pre;
        if (mul) IsMultiValues = true;
        if (regex != null) RegexPattern = regex;
        if (replace != null) Replace = replace;
        if (replaceTo != null) ReplaceTo = replaceTo;
        if (pathR2 != null) PathR2 = pathR2;
        if (referer != null) Referer = referer;
        if(after != null) After = after;
    }

    public string Path { get; set; }
    public string PathR2 { get; set; }
    public string Mode { get; set; }
    public string Attribute { get; set; }
    public string Pre { get; set; }
    public string After { get; set; }
    public bool IsMultiValues { get; set; }
    public string RegexPattern { get; set; }
    public string Replace { get; set; }
    public string ReplaceTo { get; set; }
    public string Referer { get; set; }
}


public enum CustomXpathMode
{
    Attribute,
    InnerText,
    Node
}

public enum CustomSiteType
{
    Html,
    Booru,
}