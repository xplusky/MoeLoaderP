using System;
using System.Linq;

namespace MoeLoaderP.Core
{
    public class Downloader
    {
        public DownloadItems DownloadItems { get; set; } = new DownloadItems();

        public bool IsDownloading => DownloadItems.Any(t => t.Status == DownloadStatusEnum.Downloading || t.Status == DownloadStatusEnum.WaitForDownload);
        public Settings Set { get; set; }
        
        public Downloader(Settings set)
        {
            Set = set;
        }

        public void TimerOnTick(object sender, EventArgs e)
        {

            var downingCount = 0;
            foreach (var item in DownloadItems)
            {
                if (item.Status == DownloadStatusEnum.Downloading) downingCount += 1;
                if (item.Status == DownloadStatusEnum.WaitForDownload)
                {
                    if (downingCount < Set.MaxOnDownloadingImageCount)
                    {
                        var _ = item.DownloadFileAsync();
                        downingCount += 1;
                    }
                }
            }
        }

        public void AddDownload(MoeItem item,dynamic bitimg)
        {
            var downItem = new DownloadItem(Set, bitimg,item);
            if (item.ChildrenItems.Count > 0)
            {
                for (var i = 0; i < item.ChildrenItems.Count; i++)
                {
                    var subItem = item.ChildrenItems[i];
                    var downSubItem = new DownloadItem(Set, bitimg, subItem, i + 1, item);

                    downItem.SubItems.Add(downSubItem);
                }
            }

            DownloadItems.Add(downItem);
        }


        public void Stop(DownloadItems items)
        {
            foreach (var item in items)
            {
                if (item.Status == DownloadStatusEnum.WaitForDownload || item.Status == DownloadStatusEnum.Downloading)
                {
                    item.CurrentDownloadTaskCts?.Cancel();
                    item.Status = DownloadStatusEnum.Stop;
                }
            }
        }
        
        public void Delete(DownloadItems items)
        {
            foreach (var item in items)
            {
                DownloadItems.Remove(item);
                item.CurrentDownloadTaskCts?.Cancel();
                item.Status = DownloadStatusEnum.Cancel;
            }
        }

        public void DeleteAllSuccess()
        {
            for (var i = 0; i < DownloadItems.Count; i++)
            {
                var item = DownloadItems[i];
                if (item.Status == DownloadStatusEnum.Success 
                    || item.Status == DownloadStatusEnum.Skip)
                {
                    DownloadItems.Remove(item);
                    i--;
                }
            }
        }

        public void Retry(DownloadItems items)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.Status == DownloadStatusEnum.Downloading)
                {
                    item.CurrentDownloadTaskCts?.Cancel();
                    item.Status = DownloadStatusEnum.WaitForDownload;
                }

                if (item.Status == DownloadStatusEnum.Failed)
                {
                    item.Status = DownloadStatusEnum.WaitForDownload;
                }
            }
        }
    }
}
