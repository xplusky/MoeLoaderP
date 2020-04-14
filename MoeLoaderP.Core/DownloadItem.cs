using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 表示下载列表中的单个项目（可以有一组子项目）
    /// </summary>
    public class DownloadItem : BindingObject
    {
        public Settings Set { get; set; }
        public MoeItem ImageItem { get; set; }
        public dynamic BitImg { get; set; } // bitmap image
        public string OriginFileName { get; set; }
        public string OriginFileNameWithoutExt { get; set; }
        private string _localFileShortNameWithoutExt;

        public string LocalFileShortNameWithoutExt
        {
            get => _localFileShortNameWithoutExt;
            set => SetField(ref _localFileShortNameWithoutExt, value, nameof(LocalFileShortNameWithoutExt));
        }

        public string LocalFileFullPath { get; set; }
        public string LocalExtraFileFullPath { get; set; }

        public event Action<DownloadItem> DownloadStatusChanged;

        private string _size;
        public string Size
        {
            get => _size;
            set => SetField(ref _size, value, nameof(Size));
        }

        private double _progress;
        public double Progress
        {
            get => _progress;
            set => SetField(ref _progress, value, nameof(Progress));
        }

        public string DownloadStatusIconText
        {
            get
            {
                switch (Status)
                {
                    case DownloadStatusEnum.WaitForDownload:
                        return "";
                    case DownloadStatusEnum.Success:
                        return "";
                    case DownloadStatusEnum.Cancel:
                        return "";
                    case DownloadStatusEnum.IsExist:
                        return "";
                    case DownloadStatusEnum.Failed:
                        return "";
                    case DownloadStatusEnum.Downloading:
                        return "";
                    case DownloadStatusEnum.Skip:
                        return "";
                    case DownloadStatusEnum.Stop:
                        return WebUtility.HtmlDecode("&#xF04D;");
                }
                return null;
            }
        }

        private DownloadStatusEnum _downloadStatus = DownloadStatusEnum.WaitForDownload;
        public DownloadStatusEnum Status
        {
            get => _downloadStatus;
            set
            {
                var isChanged = value != _downloadStatus;
                SetField(ref _downloadStatus, value, nameof(Status));
                if (isChanged)
                {
                    OnPropertyChanged(nameof(DownloadStatusIconText));
                    DownloadStatusChanged?.Invoke(this);
                }
            }
        }

        public CancellationTokenSource CurrentDownloadTaskCts { get; set; }
        /// <summary>
        /// 子图片组
        /// </summary>
        public DownloadItems SubItems { get; set; } = new DownloadItems();
        public int SubIndex { get; set; }

        public DownloadItem(Settings set, dynamic bitimg, MoeItem item, int subindex = 0, MoeItem fatheritem = null)
        {
            Set = set;
            BitImg = bitimg;
            ImageItem = item;
            SubIndex = subindex;
            OriginFileName = Path.GetFileName(item.DownloadUrlInfo.Url);
            OriginFileName = Path.GetFileNameWithoutExtension(item.DownloadUrlInfo.Url);
            var father = subindex == 0 ? null : fatheritem;
            GenFileNameWithouExt(father);
            GenLocalFileFullPath(father);
        }

        /// <summary>
        /// 异步下载图片
        /// </summary>
        /// <returns></returns>
        public async Task DownloadFileAsync()
        {
            if (SubItems.Count > 0)
            {
                Status = DownloadStatusEnum.Downloading;
                for (var i = 0; i < SubItems.Count; i++)
                {
                    if (i < Set.DownladFirstSeveralCount || Set.IsDownladFirstSeveral == false)
                    {
                        var item = SubItems[i];
                        await item.DownloadFileAsync();
                    }

                    var count = Set.DownladFirstSeveralCount < SubItems.Count ? Set.DownladFirstSeveralCount : SubItems.Count;
                    Progress = (i + 1d) / count * 100d;
                }
                Status = DownloadStatusEnum.Success;
                StatusText = "下载完成";
            }
            else
            {
                CurrentDownloadTaskCts?.Cancel();
                CurrentDownloadTaskCts = new CancellationTokenSource();
                var token = CurrentDownloadTaskCts.Token;
                try
                {
                    var url = ImageItem.DownloadUrlInfo;
                    if (url == null)
                    {
                        Status = DownloadStatusEnum.Failed;
                        StatusText = "下载失败";
                        return;
                    }
                    if (File.Exists(LocalFileFullPath))
                    {
                        if (Set.IsAutoRenameWhenSame)
                        {
                            var filename = AutoRenameFullPath();
                            LocalFileFullPath = filename;
                        }
                        else
                        {
                            Status = DownloadStatusEnum.Skip;
                            StatusText = "跳过";
                            return;
                        }

                    }

                    var net = ImageItem.Net == null ? new NetDocker(Set) : ImageItem.Net.CloneWithOldCookie();
                    if (!string.IsNullOrWhiteSpace(url.Referer)) net.SetReferer(url.Referer);
                    net.ProgressMessageHandler.HttpReceiveProgress += (sender, args) =>
                    {
                        Progress = args.ProgressPercentage;
                        StatusText = $"正在下载：{Progress}%";
                        if (Progress >= 100)
                        {
                            StatusText = "下载完成";
                        }
                    };
                    net.SetTimeOut(500);

                    if (File.Exists(LocalFileFullPath))
                    {
                        Status = DownloadStatusEnum.Skip;
                        StatusText = "跳过";
                        return;
                    }
                    Status = DownloadStatusEnum.Downloading;
                    var data = await net.Client.GetAsync(url.Url, token);
                    var bytes = await data.Content.ReadAsByteArrayAsync();

                    var dir = Path.GetDirectoryName(LocalFileFullPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir ?? throw new InvalidOperationException());
                    using (var fs = new FileStream(LocalFileFullPath, FileMode.Create))
                    {
                        await fs.WriteAsync(bytes, 0, bytes.Length, token);
                    }

                    if (ImageItem.ExtraFile != null)
                    {
                        File.WriteAllText(LocalExtraFileFullPath, ImageItem.ExtraFile.Content);
                    }
                    
                    Progress = 100;
                    Extend.Log($"{url.Url} download ok");
                    Status = DownloadStatusEnum.Success;
                }
                catch (Exception ex)
                {
                    Extend.Log(ex);
                    Status = DownloadStatusEnum.Failed;
                    CurrentDownloadTaskCts = null;
                }
            }
        }

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value, nameof(StatusText));
        }

        public void GenLocalFileFullPath(MoeItem father = null)
        {
            var img = father ?? ImageItem;
            var sub = (Set.IsSortFolderByKeyword && !string.IsNullOrWhiteSpace(img.Para.Keyword))
                ? $"{img.Para.Keyword}\\" : "";
            LocalFileFullPath = Path.Combine(Set.ImageSavePath, img.Site.ShortName, $"{sub}{LocalFileShortNameWithoutExt}.{img.FileType?.ToLower()}");
            if (img.ExtraFile != null)
            {
                LocalExtraFileFullPath = Path.Combine(Set.ImageSavePath, img.Site.ShortName, $"{sub}{LocalFileShortNameWithoutExt}.{img.ExtraFile.FileExt}");
            }
        }
        public void GenFileNameWithouExt(MoeItem father = null)
        {
            var img = father ?? ImageItem;
            if (Set.IsUseCustomFileNameFormat)
            {
                var format = Set.SaveFileNameFormat;

                var sb = new StringBuilder(format);
                sb.Replace("%site", img.Site.DisplayName);
                sb.Replace("%id", $"{img.Id}");
                var tags = string.Empty;
                var i = 0;
                foreach (var tag in img.Tags)
                {
                    if (i > 15) break;
                    tags += $"{tag} ";
                    i++;
                }
                sb.Replace("%tag", $"{tags}");
                sb.Replace("%title", img.Title ?? "no-title");
                sb.Replace("%uploader", img.Uploader ?? "no-uploader");
                sb.Replace("%date", img.DateString ?? "no-date");
                sb.Replace("%origin", OriginFileNameWithoutExt);
                sb.Replace("%character", img.Character ?? "no-character");
                sb.Replace("%artist", img.Artist ?? "no-artist");
                sb.Replace("%copyright", img.Copyright ?? "no-copyright");
                foreach (var c in Path.GetInvalidFileNameChars()) sb.Replace($"{c}", "");

                LocalFileShortNameWithoutExt = SubIndex > 0 ? $"{sb} item-{SubIndex}" : $"{sb}";
            }
            else
            {
                LocalFileShortNameWithoutExt = $"{img.Site.ShortName} {img.Id}";
                LocalFileShortNameWithoutExt = SubIndex > 0 ? $"{img.Site.ShortName} {img.Id} p{SubIndex}" : $"{img.Site.ShortName} {img.Id}";
            }
        }


        public string AutoRenameFullPath()
        {
            var oldf = LocalFileFullPath;
            var ext = Path.GetExtension(oldf);
            var dir = Path.GetDirectoryName(oldf);
            var file = Path.GetFileNameWithoutExtension(oldf);
            var i = 2;
            var newf = $"{dir}\\{file}{-i}{ext}";
            while (File.Exists(newf))
            {
                i++;
                newf = $"{dir}{-i}{ext}";
            }

            return newf;
        }

    }

    public class DownloadItems : ObservableCollection<DownloadItem> { }

    public enum DownloadStatusEnum
    {
        Success,
        Failed,
        Cancel,
        IsExist,
        Stop,
        Downloading,
        WaitForDownload,
        Skip
    }
}