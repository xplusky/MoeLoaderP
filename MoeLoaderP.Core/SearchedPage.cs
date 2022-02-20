using System;
using System.Collections.ObjectModel;
using System.Text;

namespace MoeLoaderP.Core;

/// <summary>
///     真实页
/// </summary>
public class SearchedPage : MoeItems
{
    public SearchPara Para { get; set; }
    public SearchPara NextPagePara { get; set; }
    public bool? HasNextPage { get; set; }
    public string NextPageIndexCursor { get; set; }

    public string Message { get; set; }
    public Exception SearchException { get; set; }

    public int? CurrentPageNumFromOne { get; set; }

    public int? CurrentPageNum { get; set; }
    public int? CurrentPageItemsOriginCount => Count;
    public int? CurrentPageItemsOutputCount { get; set; }
    public int? CurrentPageItemsStartNum { get; set; }
    public int? CurrentPageItemsEndNum { get; set; }

    public int? TotalPageCount { get; set; }
    public int? TotalItemCount { get; set; }

    public StringBuilder OriginString { get; set; }

    public void GenNextPagePara()
    {
        if (HasNextPage == false) return;
        var newPara = Para.Clone();
        if (newPara.PageIndex != null) newPara.PageIndex++;
        newPara.PageIndexCursor = NextPageIndexCursor;
        NextPagePara = newPara;
    }

    public string GetTipString()
    {
        var opt = $"第【{CurrentPageNumFromOne}/{TotalPageCount}】页: 总图片数量【{TotalItemCount}】，本页图片（过滤前/过滤后）【{CurrentPageItemsOriginCount}/{CurrentPageItemsOutputCount}】张,图片范围（过滤前）【{CurrentPageItemsStartNum}~{CurrentPageItemsEndNum}】";
        return opt;
    }
}

public class SearchedPages : ObservableCollection<SearchedPage>{}


public class PageButtonTipData
{
    public string CurrentPageNum { get; set; }
    public string CurrentPagePicCount { get; set; }
    public string CurrentPagePicNumRange { get; set; }
    public string CurrentPagePicIdRange { get; set; }
}

public class PageButtonTipDataList : ObservableCollection<PageButtonTipData>
{
    public string TotalCount { get; set; }
}
