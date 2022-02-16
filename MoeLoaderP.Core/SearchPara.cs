using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MoeLoaderP.Core.Sites;

namespace MoeLoaderP.Core;

/// <summary>
///     搜索参数
/// </summary>
public class SearchPara
{
    public MoeSite Site { get; set; }
    public SearchSession Session { get; set; }
    public string Keyword { get; set; }
    public List<string> MultiKeywords { get; set; }
    public int? PageIndex { get; set; }
    public string PageIndexCursor { get; set; }
    public int CountLimit { get; set; }

    public bool IsShowExplicit { get; set; }
    public bool IsShowExplicitOnly { get; set; }

    public bool IsFilterResolution { get; set; }

    public int MinWidth { get; set; }
    public int MinHeight { get; set; }
    
    public bool IsFileTypeShowSpecificOnly { get; set; }

    public ImageOrientation Orientation { get; set; } = ImageOrientation.None;

    public ImageOrder OrderBy { get; set; }

    public DateTime? Date { get; set; }

    public int Lv2MenuIndex { get; set; }
    public int Lv3MenuIndex { get; set; }
    public int Lv4MenuIndex { get; set; }

    public MoeSiteConfig Config { get; set; }
    public MirrorSiteConfig MirrorSite { get; set; }

    public SearchPara Clone()
    {
        return (SearchPara) MemberwiseClone();
    }
}

public enum ImageOrientation
{
    None = 0,
    Landscape = 1,
    Portrait = 2
}

public enum ImageOrderBy
{
    Id,
    IdDescending,
    Date,
    DateDescending,
    Popular,
    PopularDescending
}

public class ImageOrder
{
    public string Name { get; set; }
    public ImageOrderBy Order { get; set; }
    public bool IsDefault { get; set; }
}

public class ImageOrders : ObservableCollection<ImageOrder>
{
    public ImageOrders()
    {
        Add(new ImageOrder
        {
            Name = "(默认)",
            IsDefault = true
        });
    }

    public void Add(string name, ImageOrderBy order)
    {
        var io = new ImageOrder();
        io.Name = name;
        io.Order = order;
        Add(io);
    }
}