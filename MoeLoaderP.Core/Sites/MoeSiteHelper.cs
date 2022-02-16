using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MoeLoaderP.Core.Sites;

public class MoeSiteConfig
{
    /// <summary>
    ///     是否支持评分，若为false则不可按分数过滤图片
    /// </summary>
    public bool IsSupportScore { get; set; }

    /// <summary>
    ///     是否支持图片年龄分级
    /// </summary>
    public bool IsSupportRating { get; set; }

    /// <summary>
    ///     是否支持分辨率，若为false则不可按分辨率过滤图片
    /// </summary>
    public bool IsSupportResolution { get; set; }

    /// <summary>
    ///     是否支持关键字搜索. 不支持则不显示搜索框
    /// </summary>
    public bool IsSupportKeyword { get; set; }

    /// <summary>
    ///     是否支持按图片id搜索,支持的话搜索参数里面可以设置
    /// </summary>
    public bool IsSupportSearchByImageLastId { get; set; }

    /// <summary>
    ///     是否支持用户登录，支持的话会显示登录按钮
    /// </summary>
    public bool IsSupportAccount { get; set; }

    public bool IsSupportThumbButton { get; set; }
    public bool IsSupportStarButton { get; set; }
    public bool IsSupportMultiKeywords { get; set; }
    public bool IsSupportDatePicker { get; set; }
    public bool IsR18Site { get; set; } = false;
    public bool IsCustomSite { get; set; }
    public ImageOrders ImageOrders { get; set; } = new();

    public int? MaxLimit { get; set; }

    public MoeSiteConfig Clone()
    {
        return (MoeSiteConfig) MemberwiseClone();
    }
}

public class MirrorSiteConfig
{
    public string Name { get; set; }
    public bool IsDefault { get; set; }
    public string HomeUrl { get; set; }
    public string SearchApi { get; set; }
}

public class MirrorSiteConfigs : ObservableCollection<MirrorSiteConfig>
{
    public MirrorSiteConfigs()
    {
        var msc = new MirrorSiteConfig
        {
            Name = "(不使用镜像）",
            IsDefault = true
        };
        Add(msc);
    }
}

/// <summary>
///     自动提示列表中的一项
/// </summary>
public class AutoHintItem
{
    public string Word { get; set; }
    public string Count { get; set; }
    public bool IsEnable { get; set; } = true;
    public bool IsHistory { get; set; }
}

public class AutoHintItems : ObservableCollection<AutoHintItem>
{
    public void AddHistory(string keyword, Settings settings)
    {
        if (keyword.IsEmpty()) return;
        foreach (var item in this)
            if (item.Word == keyword)
                return;
        var hintItem = new AutoHintItem
        {
            IsHistory = true,
            Word = keyword
        };
        if (Count >= settings.HistoryKeywordsMaxCount) RemoveAt(Count - 1);
        Insert(0, hintItem);
    }

    public void Add(string word, string count = null)
    {
        var item = new AutoHintItem {Word = word, Count = count};
        Add(item);
    }
}

public class Category
{
    public Category(MoeSiteConfig fatherConfig, string name)
    {
        Config = fatherConfig?.Clone();
        SubCategories = new Categories(Config);
        Name = name;
    }

    //public Category(MoeSiteConfig fatherConfig, string name, Categories cats = null)
    //{
    //    Config = fatherConfig.Clone();
    //    Name = name;
    //    if (cats != null) SubCategories = cats;
    //}

    //public Category()
    //{
    //}

    public string Name { get; set; }
    public Categories SubCategories { get; set; } 
    public MoeSiteConfig Config { get; set; }
}

public class Categories : List<Category>
{
    //public Categories(MoeSiteConfig fatherConfig, params string[] itemsNames)
    //{
    //    Config = fatherConfig;
    //    foreach (var item in itemsNames) Add(new Category(fatherConfig, item));
    //}

    //public Categories(MoeSiteConfig fatherConfig, Categories submenu, params string[] itemsNames)
    //{
    //    Config = fatherConfig;
    //    foreach (var name in itemsNames) Add(new Category(fatherConfig, name, submenu));
    //}

    //public Categories(IMoeSiteConfig site = null) { }
    public Categories(MoeSiteConfig fatherConfig)
    {
        Config = fatherConfig;
    }

    public void Add(string name)
    {
        Add(new Category(Config, name));
    }

    public void Adds(params string[] itemsNames)
    {
        foreach (var item in itemsNames) Add(new Category(Config, item));
    }

    public void EachSubAdds(params string[] itemsNames)
    {
        foreach (var category in this)
        {
            category.SubCategories.Adds(itemsNames);
        }
    }

    public MoeSiteConfig Config { get; set; }

    public Category AddAndSelect(string catname)
    {
        var cat = new Category(Config, catname);
        Add(cat);
        return cat;
    }
}