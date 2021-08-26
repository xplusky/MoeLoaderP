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
        public Settings Settings { get; set; }
        public MoeItem DownloadMoeItem { get; set; }
        /// <summary>
        /// bitmap image 用于显示下载图片图标
        /// </summary>
        public dynamic BitImg { get; set; } 

        /// <summary>
        /// 网站上原始文件名
        /// </summary>
        public string OriginFileName { get; set; }
        /// <summary>
        /// 网站上原始文件名（不含后缀）
        /// </summary>
        public string OriginFileNameWithoutExtension { get; set; }

        private string _localFileShortNameWithoutExt;

        public string LocalFileShortNameWithoutExt
        {
            get => _localFileShortNameWithoutExt;
            set => SetField(ref _localFileShortNameWithoutExt, value, nameof(LocalFileShortNameWithoutExt));
        }

        public string LocalFileFullPath { get; set; }

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
                var strings = new[] {"", "", "",  WebUtility.HtmlDecode("&#xF04D;"), "", "", ""};
                return strings[(int) Status];
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
            Settings = set;
            BitImg = bitimg;
            DownloadMoeItem = item;
            SubIndex = subindex;
            OriginFileName = Path.GetFileName(item.DownloadUrlInfo.Url);
            OriginFileNameWithoutExtension = Path.GetFileNameWithoutExtension(item.DownloadUrlInfo.Url).ToDecodedUrl();
            var father = subindex == 0 ? null : fatheritem;
            GenFileNameWithoutExt(father);
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
                var b = Settings.DownloadFirstSeveralCount < SubItems.Count && Settings.IsDownloadFirstSeveral;
                var count = b ? Settings.DownloadFirstSeveralCount : SubItems.Count;
                for (var i = 0; i < SubItems.Count; i++)
                {
                    
                    StatusText = $"正在下载 {i+1} / {count} 张";
                    if (i < Settings.DownloadFirstSeveralCount || Settings.IsDownloadFirstSeveral == false)
                    {
                        var item = SubItems[i];
                        await item.DownloadFileAsync();
                    }
                    
                    Progress = (i + 1d) / count * 100d;
                }
                Status = DownloadStatusEnum.Success;
                StatusText = $"{count} 张下载完成";
            }
            else
            {
                CurrentDownloadTaskCts?.Cancel();
                CurrentDownloadTaskCts = new CancellationTokenSource();
                var token = CurrentDownloadTaskCts.Token;
                try
                {
                    await DownloadSingleFileAsync(token);
                }
                catch (Exception ex)
                {
                    Extend.Log(ex);
                    Status = DownloadStatusEnum.Failed;
                    CurrentDownloadTaskCts = null;
                }
            }
        }

        public async Task DownloadSingleFileAsync(CancellationToken token)
        {
            // url不正常判断
            var durl = DownloadMoeItem.DownloadUrlInfo;
            if (durl == null)
            {
                Status = DownloadStatusEnum.Failed;
                StatusText = "下载失败";
                return;
            }

            // url解析、前置操作
            if (durl.ResolveUrlFunc != null)
            {
                try
                {
                    await durl.ResolveUrlFunc.Invoke(this, token);
                }
                catch (Exception e)
                {
                    Extend.Log(e);
                }
            }

            // 文件已存在判断、改名
            if (File.Exists(LocalFileFullPath))
            {
                if (Settings.IsAutoRenameWhenSame)
                {
                    var filename = AutoRenameFullPath();
                    LocalFileFullPath = filename;
                }
                else
                {
                    Status = DownloadStatusEnum.Skip;
                    StatusText = "已存在，跳过";
                    return;
                }

            }

            // 设置下载网络
            var net = DownloadMoeItem.Net == null ? new NetOperator(Settings) : DownloadMoeItem.Net.CloneWithOldCookie();
            if (!durl.Referer.IsEmpty()) net.SetReferer(durl.Referer);
            net.ProgressMessageHandler.HttpReceiveProgress += (sender, args) =>
            {
                Progress = args.ProgressPercentage;
                StatusText = $"正在下载：{Progress}%";
            };
            net.SetTimeOut(500);
            if (File.Exists(LocalFileFullPath))
            {
                Status = DownloadStatusEnum.Skip;
                StatusText = "已存在，跳过";
                return;
            }
            Status = DownloadStatusEnum.Downloading;
            var data = await net.Client.GetAsync(durl.Url, token);
            
            // 写入文件
            var stream = await data.Content.ReadAsStreamAsync();
            var dir = Path.GetDirectoryName(LocalFileFullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir ?? throw new InvalidOperationException());
            var file = new FileInfo(LocalFileFullPath);
            using (var fileStream = file.Create())
            using (stream)
            {
                var buffer = new byte[1024];
                int length;
                while ((length = await stream.ReadAsync(buffer, 0, buffer.Length, token)) != 0)
                {
                    // 写入到文件
                    fileStream.Write(buffer, 0, length);
                }
            }

            // 下载完成后期效果
            if (durl.AfterEffects != null)
            {
                try
                {
                    await durl.AfterEffects.Invoke(this, data.Content, token);
                }
                catch (Exception e)
                {
                    Extend.Log(e);
                }
            }

            // 完成
            Progress = 100;
            StatusText = "下载完成";
            Extend.Log($"{durl.Url} download ok");
            Status = DownloadStatusEnum.Success;
        }


        private string _statusText;
        /// <summary>
        /// 状态文字
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value, nameof(StatusText));
        }

        public void GenLocalFileFullPath(MoeItem father = null)
        {
            var img = father ?? DownloadMoeItem;
            var format = Settings.SortFolderNameFormat;
            var sub = format.IsEmpty() ? $"{img.Site.ShortName}" : FormatText(format, img, true);

            LocalFileFullPath = Path.Combine(Settings.ImageSavePath, sub, $"{LocalFileShortNameWithoutExt}.{img.FileType?.ToLower()}");
        }

        public void GenFileNameWithoutExt(MoeItem father = null)
        {
            var img = father ?? DownloadMoeItem;

            var format = Settings.SaveFileNameFormat;
            if (format.IsEmpty())
            {
                LocalFileShortNameWithoutExt = $"{img.Site.ShortName} {img.Id}";
                return;
            }

            var sb = FormatText(format, img);

            LocalFileShortNameWithoutExt = SubIndex > 0 ? $"{sb} p{SubIndex}" : $"{sb}";
        }

        public string FormatText(string format, MoeItem img, bool isFolder = false)
        {
            var sb = new StringBuilder(format);
            sb.Replace("%site", img.Site.ShortName);
            sb.Replace("%id", $"{img.Id}");
            sb.Replace("%keyword", img.Para.Keyword.IsEmpty() ? "no-keyword" : img.Para.Keyword);
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
            sb.Replace("%uploader_id", img.UploaderId ?? "no-uploader-id");
            sb.Replace("%date", img.DateString ?? "no-date");
            sb.Replace("%origin", OriginFileNameWithoutExtension);
            sb.Replace("%character", img.Character ?? "no-character");
            sb.Replace("%artist", img.Artist ?? "no-artist");
            sb.Replace("%copyright", img.Copyright ?? "no-copyright");
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                if (c == '\\' && isFolder) continue;
                sb.Replace($"{c}", "");
            }

            return sb.ToString();
        }

        public string AutoRenameFullPath()
        {
            var oldF = LocalFileFullPath;
            var ext = Path.GetExtension(oldF);
            var dir = Path.GetDirectoryName(oldF);
            var file = Path.GetFileNameWithoutExtension(oldF);
            var i = 2;
            var newF = $"{dir}\\{file}{-i}{ext}";
            while (File.Exists(newF))
            {
                i++;
                newF = $"{dir}{-i}{ext}";
            }

            return newF;
        }

    }

    public class DownloadItems : ObservableCollection<DownloadItem> { }

    public enum DownloadStatusEnum
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