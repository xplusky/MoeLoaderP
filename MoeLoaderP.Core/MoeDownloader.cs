using System;
using System.Linq;

namespace MoeLoaderP.Core;

/// <summary>
///     下载管理器
/// </summary>
public class MoeDownloader
{
    public MoeDownloader(Settings set)
    {
        Set = set;
    }

    public MoeItems DownloadItems { get; set; } = new();

    public bool IsDownloading => DownloadItems.Any(t
        => t.DlStatus is DownloadStatus.Downloading or DownloadStatus.WaitForDownload);

    public Settings Set { get; set; }

    public void TimerOnTick(object sender, EventArgs e)
    {
        var downingCount = 0;
        foreach (var item in DownloadItems)
        {
            if (item.DlStatus == DownloadStatus.Downloading) downingCount += 1;
            if (item.DlStatus == DownloadStatus.WaitForDownload)
                if (downingCount < Set.MaxOnDownloadingImageCount)
                {
                    var _ = item.DownloadFileAsync(item.CurrentDownloadTaskCts?.Token ?? default);
                    downingCount += 1;
                }
        }
    }

    public void AddDownload(MoeItem item, dynamic bitimg)
    {
        item.InitDownload(bitimg);
        if (item.ChildrenItems.Count > 0)
            for (var i = 0; i < item.ChildrenItems.Count; i++)
            {
                var subItem = item.ChildrenItems[i];
                subItem.InitDownload(bitimg, i + 1, item);
            }

        DownloadItems.Add(item);
    }


    public static void Stop(MoeItems items)
    {
        foreach (var item in items)
            if (item.DlStatus is DownloadStatus.WaitForDownload or DownloadStatus.Downloading)
            {
                item.CurrentDownloadTaskCts?.Cancel();
                item.DlStatus = DownloadStatus.Stop;
            }
    }

    public void Delete(MoeItems items)
    {
        foreach (var item in items)
        {
            DownloadItems.Remove(item);
            item.CurrentDownloadTaskCts?.Cancel();
            item.DlStatus = DownloadStatus.Cancel;
        }
    }

    public void DeleteAllSuccess()
    {
        for (var i = 0; i < DownloadItems.Count; i++)
        {
            var item = DownloadItems[i];
            if (item.DlStatus is DownloadStatus.Success or DownloadStatus.Skip)
            {
                DownloadItems.Remove(item);
                i--;
            }
        }
    }

    public static void Retry(MoeItems items)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.DlStatus == DownloadStatus.Downloading)
            {
                item.CurrentDownloadTaskCts?.Cancel();
                item.DlStatus = DownloadStatus.WaitForDownload;
            }

            if (item.DlStatus == DownloadStatus.Failed) item.DlStatus = DownloadStatus.WaitForDownload;
        }
    }

    public static Pairs GenRenamePairs()
    {
        var pairs = new Pairs
        {
            {"站点缩略名", "%site"},
            {"站点显示名", "%sitedispname"},
            {"搜索关键字", "%keyword"},
            {"作品ID", "%id"},
            {"作者", "%uploader"},
            {"作者ID", "%uploader_id"},
            {"标题", "%title"},
            {"标签", "%tag"},
            {"日期", "%date"},
            {"原始文件名", "%origin"},
            {"作品名", "%copyright"},
            {"角色名", "%character"},
            {"画师名", "%artist"}
        };

        return pairs;
    }
}

public enum DownloadStatus
{
    /// <summary>
    ///     成功
    /// </summary>
    Success,

    /// <summary>
    ///     下载失败
    /// </summary>
    Failed,

    /// <summary>
    ///     下载取消
    /// </summary>
    Cancel,

    /// <summary>
    ///     下载停止
    /// </summary>
    Stop,

    /// <summary>
    ///     正在下载
    /// </summary>
    Downloading,

    /// <summary>
    ///     进入下载列队等待下载
    /// </summary>
    WaitForDownload,

    /// <summary>
    ///     跳过（重名等）
    /// </summary>
    Skip
}