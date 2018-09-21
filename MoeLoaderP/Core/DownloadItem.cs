using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MoeLoader.Core
{
    public class DownloadItem : NotifyBase
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
        public string Host { get; set; }
        public string LocalFileShortName { get; set; }
        public string LocalFileFullName { get; set; }
        public string Referer { get; set; }
        public bool NoVerify { get; set; }

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
                    var hchandler = new HttpClientHandler();
                    var progressHandler = new ProgressMessageHandler(hchandler);
                    var client = new HttpClient(progressHandler);
                    // todo referer
                    if (ImageItem.Site.Referer != null) client.DefaultRequestHeaders.Referrer = new Uri(ImageItem.Site.Referer);
                    progressHandler.HttpReceiveProgress += (sender, args) => { Progress = args.ProgressPercentage; };

                    DownloadStatus = DownloadStatusEnum.Downloading;
                    var data = await client.GetAsync(ImageItem.OriginalUrl, token);
                    var bytes = await data.Content.ReadAsByteArrayAsync();
                    using (var fs = new FileStream(GetFilePath(), FileMode.Create))
                    {
                        await fs.WriteAsync(bytes, 0, bytes.Length, token);
                    }
                    Extend.Log("download ok");
                    DownloadStatus = DownloadStatusEnum.Success;
                }
                catch (TaskCanceledException)
                {
                    DownloadStatus = DownloadStatusEnum.Cancel;
                    CurrentDownloadTaskCts = null;
                }
                catch (Exception ex)
                {
                    Extend.Log(ex);
                    DownloadStatus = DownloadStatusEnum.Failed;
                    CurrentDownloadTaskCts = null;
                }
            }
            
        }
        
        public string GetFilePath()
        {
            var org = Path.GetFileName(ImageItem.OriginalUrl);
            if (org == null)
            {
                return null;
            }
            var txt = Path.GetExtension(ImageItem.OriginalUrl);

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

        
    }

    public class DownloadItems : ObservableCollection<DownloadItem> { }
    public enum DownloadStatusEnum { Success, Failed, Cancel, IsExist, Downloading, WaitForDownload }
}