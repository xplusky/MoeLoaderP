using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace MoeLoader.Core
{
    /// <summary>
    /// 表示下载列表中的单个项目（可以有一组子项目）
    /// </summary>
    public class DownloadItem : BindingObject
    {
        public Settings Settings { get; set; }
        public ImageItem ImageItem { get; set; }

        private ImageSource _imageSource;
        public ImageSource ImageSource
        {
            get => _imageSource;
            set => SetField(ref _imageSource, value, nameof(ImageSource));
        }

        public string FileName { get; set; }
        public string LocalFileShortNameWithoutExt { get; set; }

        public string LocalFileFullPath =>
            Path.Combine(Settings.ImageSavePath, ImageItem.Site.ShortName, $"{LocalFileShortNameWithoutExt}.{ImageItem.FileType.ToLower()}");

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
                switch (DownloadStatus)
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
                }
                return null;
            }
        }


        private DownloadStatusEnum _downloadStatus = DownloadStatusEnum.WaitForDownload;
        public DownloadStatusEnum DownloadStatus
        {
            get => _downloadStatus;
            set
            {
                var change = value != _downloadStatus;
                SetField(ref _downloadStatus, value, nameof(DownloadStatus));
                OnPropertyChanged(nameof(DownloadStatusIconText));
                if (change) DownloadStatusChanged?.Invoke(this);
            }
        }

        public string SaveLocation { set; get; }
        
        public CancellationTokenSource CurrentDownloadTaskCts { get; set; }
        public DownloadItems SubItems { get; set; } = new DownloadItems();
        public int SubIndex { get; set; }

        public async Task DownloadFileAsync()
        {
            CurrentDownloadTaskCts?.Cancel();
            CurrentDownloadTaskCts = new CancellationTokenSource();
            var token = CurrentDownloadTaskCts.Token;
            if (SubItems.Count > 0)
            {
                DownloadStatus = DownloadStatusEnum.Downloading;
                for (var i = 0; i < SubItems.Count; i++)
                {
                    var item = SubItems[i];
                    await item.DownloadFileAsync();
                    Progress = (i + 1d) / SubItems.Count * 100d;
                }
                DownloadStatus = DownloadStatusEnum.Success;
            }
            else
            {
                try
                {
                    NetSwap net;
                    if (ImageItem.Net == null)
                    {
                        net = new NetSwap(Settings);
                        if (ImageItem.FileReferer != null) net.SetReferer(ImageItem.FileReferer);
                        net.ProgressMessageHandler.HttpReceiveProgress += (sender, args) => { Progress = args.ProgressPercentage; };
                    }
                    else
                    {
                        net = ImageItem.Net.CreatNewWithRelatedCookie();
                        net.SetReferer(ImageItem.FileReferer);
                        net.ProgressMessageHandler.HttpReceiveProgress += (sender, args) => { Progress = args.ProgressPercentage; };
                    }
                    net.SetTimeOut(500);

                    DownloadStatus = DownloadStatusEnum.Downloading;
                    var data = await net.Client.GetAsync(ImageItem.FileUrl, token);
                    var bytes = await data.Content.ReadAsByteArrayAsync();
                    using (var fs = new FileStream(GetFilePath(), FileMode.Create))
                    {
                        await fs.WriteAsync(bytes, 0, bytes.Length, token);
                    }
                    Progress = 100;
                    App.Log($"{ImageItem.FileUrl} download ok");
                    DownloadStatus = DownloadStatusEnum.Success;
                }
                catch (TaskCanceledException)
                {
                    DownloadStatus = DownloadStatusEnum.Cancel;
                    CurrentDownloadTaskCts = null;
                }
                catch (Exception ex)
                {
                    App.Log(ex);
                    DownloadStatus = DownloadStatusEnum.Failed;
                    CurrentDownloadTaskCts = null;
                }
            }
            
        }
        
        public string GetFilePath()
        {
            var org = Path.GetFileName(ImageItem.FileUrl);
            if (org == null)
            {
                return null;
            }
            var txt = Path.GetExtension(ImageItem.FileUrl);

            var folder = Path.Combine(Settings.ImageSavePath, ImageItem.Site.ShortName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            var path = Path.Combine(Settings.ImageSavePath, ImageItem.Site.ShortName, org);
            return path;
        }

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value, nameof(StatusText));
        }

        public void GenFileNameWithouExt()
        {
            var set = Settings;
            if (set.IsUseCustomFileNameFormat)
            {

            }
            else
            {
                if (SubItems.Count > 1)
                {
                    foreach (var child in SubItems)
                    {
                        child.LocalFileShortNameWithoutExt = $"{ImageItem.Site.ShortName} {ImageItem.Id}-{child.SubIndex}";
                    }
                }
                else
                {
                    LocalFileShortNameWithoutExt = $"{ImageItem.Site.ShortName} {ImageItem.Id}";
                }
                
            }
        }

        public void AutoRename(int seed)
        {

        }
    }

    public class DownloadItems : ObservableCollection<DownloadItem> { }

    public enum DownloadStatusEnum
    {
        Success,
        Failed,
        Cancel,
        IsExist,
        Downloading,
        WaitForDownload
    }
}