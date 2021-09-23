using System;
using System.Collections.Generic;
using System.Linq;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 下载管理器
    /// </summary>
    public class MoeDownloader
    {
        public MoeItems DownloadItems { get; set; } = new MoeItems();

        public bool IsDownloading => DownloadItems.Any(t 
            => t.DlStatus == DownloadStatus.Downloading || t.DlStatus == DownloadStatus.WaitForDownload);
        public Settings Set { get; set; }
        
        public MoeDownloader(Settings set)
        {
            Set = set;
        }

        public void TimerOnTick(object sender, EventArgs e)
        {
            var downingCount = 0;
            foreach (var item in DownloadItems)
            {
                if (item.DlStatus == DownloadStatus.Downloading) downingCount += 1;
                if (item.DlStatus == DownloadStatus.WaitForDownload)
                {
                    if (downingCount < Set.MaxOnDownloadingImageCount)
                    {
                        var _ = item.DownloadFileAsync(item.CurrentDownloadTaskCts.Token);
                        downingCount += 1;
                    }
                }
            }
        }

        public void AddDownload(MoeItem item,dynamic bitimg)
        {
            item.InitDownload(bitimg);
            if (item.ChildrenItems.Count > 0)
            {
                for (var i = 0; i < item.ChildrenItems.Count; i++)
                {
                    var subItem = item.ChildrenItems[i];
                    subItem.InitDownload(bitimg, i + 1, item);
                }
            }

            DownloadItems.Add(item);
        }


        public void Stop(MoeItems items)
        {
            foreach (var item in items)
            {
                if (item.DlStatus == DownloadStatus.WaitForDownload 
                    || item.DlStatus == DownloadStatus.Downloading)
                {
                    item.CurrentDownloadTaskCts?.Cancel();
                    item.DlStatus = DownloadStatus.Stop;
                }
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
                if (item.DlStatus == DownloadStatus.Success 
                    || item.DlStatus == DownloadStatus.Skip)
                {
                    DownloadItems.Remove(item);
                    i--;
                }
            }
        }

        public void Retry(MoeItems items)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.DlStatus == DownloadStatus.Downloading)
                {
                    item.CurrentDownloadTaskCts?.Cancel();
                    item.DlStatus = DownloadStatus.WaitForDownload;
                }

                if (item.DlStatus == DownloadStatus.Failed)
                {
                    item.DlStatus = DownloadStatus.WaitForDownload;
                }
            }
        }

        public static Pairs GenRenamePairs()
        {
            var pairs = new Pairs();
            pairs.Add("站点缩略名", "%site");
            pairs.Add("站点显示名", "%sitedispname");
            pairs.Add("搜索关键字", "%keyword");
            pairs.Add("作品ID", "%id");
            pairs.Add("作者", "%uploader");
            pairs.Add("作者ID", "%uploader_id");
            pairs.Add("标题", "%title");
            pairs.Add("标签", "%tag");
            pairs.Add("日期", "%date");
            pairs.Add("原始文件名", "%origin");
            pairs.Add("作品名", "%copyright");
            pairs.Add("角色名", "%character");
            pairs.Add("画师名", "%artist");

            return pairs;
        }
    }

    public enum DownloadStatus
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success,
        /// <summary>
        /// 下载失败
        /// </summary>
        Failed,
        /// <summary>
        /// 下载取消
        /// </summary>
        Cancel,
        /// <summary>
        /// 下载停止
        /// </summary>
        Stop,
        /// <summary>
        /// 正在下载
        /// </summary>
        Downloading,
        /// <summary>
        /// 进入下载列队等待下载
        /// </summary>
        WaitForDownload,
        /// <summary>
        /// 跳过（重名等）
        /// </summary>
        Skip
    }

}
