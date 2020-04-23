using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Newtonsoft.Json;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 表示下载列表中的单个项目（可以有一组子项目）
    /// </summary>
    public class DownloadItem : BindingObject
    {
        public Settings Set { get; set; }
        public MoeItem CurrentMoeItem { get; set; }
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
            CurrentMoeItem = item;
            SubIndex = subindex;
            OriginFileName = Path.GetFileName(item.DownloadUrlInfo.Url);
            OriginFileName = Path.GetFileNameWithoutExtension(item.DownloadUrlInfo.Url);
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
                for (var i = 0; i < SubItems.Count; i++)
                {
                    if (i < Set.DownloadFirstSeveralCount || Set.IsDownloadFirstSeveral == false)
                    {
                        var item = SubItems[i];
                        await item.DownloadFileAsync();
                    }

                    var count = Set.DownloadFirstSeveralCount < SubItems.Count ? Set.DownloadFirstSeveralCount : SubItems.Count;
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
                    var url = CurrentMoeItem.DownloadUrlInfo;
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

                    var net = CurrentMoeItem.Net == null ? new NetDocker(Set) : CurrentMoeItem.Net.CloneWithOldCookie();
                    if (!url.Referer.IsNaN()) net.SetReferer(url.Referer);
                    net.ProgressMessageHandler.HttpReceiveProgress += (sender, args) =>
                    {
                        Progress = args.ProgressPercentage;
                        StatusText = $"正在下载：{Progress}%";
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

                    if (CurrentMoeItem.ExtraFile != null)
                    {
                        var path = Path.ChangeExtension(LocalFileFullPath, CurrentMoeItem.ExtraFile.FileExt) ?? $"{LocalFileFullPath}.{CurrentMoeItem.ExtraFile.FileExt}";
                        File.WriteAllText(path, CurrentMoeItem.ExtraFile.Content);
                    }

                    if (url.IsPixivGifZip && CurrentMoeItem.ExtraFile!=null)
                    {
                        dynamic json = JsonConvert.DeserializeObject(CurrentMoeItem.ExtraFile.Content);
                        var list = json.body.frames;
                        var gifpath = Path.ChangeExtension(LocalFileFullPath, "gif");
                        if (gifpath != null)
                        {
                            var fi = new FileInfo(gifpath);
                            try
                            {
                                StatusText = "正在转换为GIF格式...";
                                using (var stream = await data.Content.ReadAsStreamAsync())
                                {
                                    await Task.Run(() =>
                                    {
                                        ConvertPixivZipToGif(stream, list, fi);
                                    }, token);
                                }
                            }
                            catch (Exception e)
                            {
                                Extend.Log(e);
                            }
                        }
                    }
                    data.Dispose();
                    Progress = 100;
                    StatusText = "下载完成";
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

        public void ConvertPixivZipToGif(Stream stream,dynamic frames,FileInfo fi)
        {
            var delayList= new List<int>();
            using (var images = new MagickImageCollection())
            {
                foreach (var frame in frames)
                {
                    delayList.Add($"{frame.delay}".ToInt());
                }
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    for (var i = 0; i < zip.Entries.Count; i++)
                    {
                        var ms = new MemoryStream();
                        using (var aStream = zip.Entries[i].Open())
                        {
                            aStream.CopyTo(ms);
                            ms.Position = 0L;
                        }
                        var img = new MagickImage(ms);
                        img.AnimationDelay = delayList[i] / 10;
                        images.Add(img);
                        ms.Dispose();
                    }
                }
                var set = new QuantizeSettings();
                set.Colors = 256;
                images.Quantize(set);
                images.Optimize();
                images.Write(fi, MagickFormat.Gif);
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
            var img = father ?? CurrentMoeItem;
            var format = Set.SortFolderNameFormat;
            var sub = format.IsNaN() ? $"{img.Site.ShortName}" : FormatText(format, img, true);

            LocalFileFullPath = Path.Combine(Set.ImageSavePath, sub, $"{LocalFileShortNameWithoutExt}.{img.FileType?.ToLower()}");
        }

        public void GenFileNameWithoutExt(MoeItem father = null)
        {
            var img = father ?? CurrentMoeItem;
            
            var format = Set.SaveFileNameFormat;
            if (format.IsNaN())
            {
                LocalFileShortNameWithoutExt = $"{img.Site.ShortName} {img.Id}";
                return;
            }

            var sb = FormatText(format, img);

            LocalFileShortNameWithoutExt = SubIndex > 0 ? $"{sb} p{SubIndex}" : $"{sb}";
        }

        public string FormatText(string format,MoeItem img,bool isFolder=false)
        {
            var sb = new StringBuilder(format);
            sb.Replace("%site", img.Site.ShortName);
            sb.Replace("%id", $"{img.Id}");
            sb.Replace("%keyword", img.Para.Keyword.IsNaN() ? "no-keyword" : img.Para.Keyword);
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
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                if(c == '\\' && isFolder) continue;
                sb.Replace($"{c}", "");
            }

            return sb.ToString();
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
        Success, Failed, Cancel, IsExist, Stop, Downloading, WaitForDownload, Skip
    }
}