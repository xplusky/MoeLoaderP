using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MoeLoaderP.Core.Sites;

namespace MoeLoaderP.Core;

/// <summary>
///     表示一张图片（可以包含一组子图片册）及其相关信息
/// </summary>
public class MoeItem : BindingObject
{
    public MoeItem(MoeSite site, SearchPara para)
    {
        Site = site;
        Para = para;
        ChildrenItems.CollectionChanged += delegate { OnPropertyChanged(nameof(ChildrenItemsCount)); };
        Urls.CollectionChanged += delegate
        {
            OnPropertyChanged(nameof(FileType));
            OnPropertyChanged(nameof(DownloadUrlInfo));
        };
        para.Session.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SearchSession.CurrentDownloadType))
            {
                OnPropertyChanged(nameof(DownloadUrlInfo));
                OnPropertyChanged(nameof(FileType));
            }
        };
    }

    /// <summary>
    ///     所属站点
    /// </summary>
    public MoeSite Site { get; set; }

    /// <summary>
    ///     搜索参数
    /// </summary>
    public SearchPara Para { get; set; }

    /// <summary>
    ///     图组项目集合
    /// </summary>
    public MoeItems ChildrenItems { get; set; } = new();

    private Settings Set => Site.Settings;

    #region 参数属性--当前图片从网络获取到的本身参数及相关处理方法

    public int Id { get; set; }

    /// <summary>
    ///     Id字符串
    /// </summary>
    public string Sid { get; set; }

    public string Title { get; set; }

    public string DateString
    {
        get => _dateString.IsEmpty() ? Date?.ToString("G", new CultureInfo("zh-CN")) : _dateString;
        set => _dateString = value;
    }

    public DateTime? Date { get; set; }

    /// <summary>
    ///     上传用户
    /// </summary>
    public string Uploader { get; set; }

    /// <summary>
    ///     上传用户ID
    /// </summary>
    public string UploaderId { get; set; }

    public string UploaderHeadUrl { get; set; }

    /// <summary>
    ///     评分
    /// </summary>
    public double Score
    {
        get => _score;
        set
        {
            _score = value;
            OnPropertyChanged(nameof(Score));
        }
    }

    public int FavCount
    {
        get => _favCount;
        set
        {
            _favCount = value;
            OnPropertyChanged(nameof(FavCount));
        }
    }

    public bool IsFav
    {
        get => _isFav;
        set
        {
            _isFav = value;
            OnPropertyChanged(nameof(FavCount));
        }
    }

    /// <summary>
    ///     排名
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    ///     图源
    /// </summary>
    public string Source { get; set; }

    public string Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsNsfw { get; set; }
    public string DetailUrl { get; set; }
    public string OriginString { get; set; }

    /// <summary>
    ///     作品名
    /// </summary>
    public string Copyright { get; set; }

    /// <summary>
    ///     角色名
    /// </summary>
    public string Character { get; set; }

    /// <summary>
    ///     画师名
    /// </summary>
    public string Artist { get; set; }

    public UrlInfo ThumbnailUrlInfo => Urls.GetMin();

    public UrlInfo DownloadUrlInfo
    {
        get
        {
            var type = Para.Session.CurrentDownloadType;
            if (type == null) return null;
            if (type.Type != DownloadTypeEnum.Auto)
                return Urls.FirstOrDefault(urlInfo => urlInfo.DownloadType == type.Type);
            // todo
            var i = 0;
            UrlInfo info = null;
            foreach (var url in Urls)
            {
                if ((int) url.DownloadType <= i) continue;
                i = (int) url.DownloadType;
                info = url;
            }

            return info;
        }
    }

    public UrlInfos Urls { get; set; } = new();
    public TextFileInfo ExtraFile { get; set; }

    public string FileType => DownloadUrlInfo?.GetFileExtFromUrl();

    private int _width;

    public int Width
    {
        get => _width;
        set
        {
            _width = value;
            OnPropertyChanged(nameof(ResolutionText));
        }
    }

    private int _height;

    public int Height
    {
        get => _height;
        set
        {
            _height = value;
            OnPropertyChanged(nameof(ResolutionText));
        }
    }

    /// <summary>
    ///     分辨率文字
    /// </summary>
    public string ResolutionText => Width != 0 && Height != 0 ? $"{Width} × {Height}" : null;

    private int _imageCount;
    private string _dateString;
    private string _tip;
    private double _score;

    public int ChildrenItemsCount
    {
        get => _imageCount == 0 ? ChildrenItems.Count : _imageCount;
        set => _imageCount = value;
    }

    public bool IsAdOrOther { get; set; }

    #endregion

    #region 辅助属性及方法

    public bool IsLocalFilter { get; set; }

    /// <summary>
    ///     是否显示注释
    /// </summary>
    public bool TipHighLight { get; set; }

    /// <summary>
    ///     注释，显示在左上角
    /// </summary>
    public string Tip
    {
        get => _tip;
        set
        {
            _tip = value;
            OnPropertyChanged(nameof(Tip));
        }
    }

    /// <summary>
    ///     获取详细信息Task委托 (图片的某些信息需要单独获取，例如原图URL可能位于详情页面）
    /// </summary>
    public Func<CancellationToken, Task> GetDetailTaskFunc { get; set; }

    public async Task TryGetDetail(CancellationToken token = default)
    {
        if (GetDetailTaskFunc == null) return;
        try
        {
            await GetDetailTaskFunc(token);
        }
        catch (Exception e)
        {
            var m = $"获取详情页失败!ID:{Id},PAGE:{DetailUrl}";
            Ex.Log(m, e);
            ErrorMessage = m;
        }
    }

    public string ErrorMessage { get; set; }

    /// <summary>
    ///     本地过滤图片(false:过滤,true:不过滤)
    /// </summary>
    public void LocalFilter()
    {
        var isNotFilter = true;
        var para = Para;
        var config = Site.Config;
        var set = Site.Settings;
        if (IsAdOrOther) isNotFilter = false;
        if (config.IsSupportRating) // 过滤r18评级图片
        {
            switch (set.IsXMode)
            {
                case false or false when IsNsfw:
                case true when para.IsShowExplicitOnly && IsNsfw == false:
                    isNotFilter = false;
                    break;
            }
        }

        if (config.IsSupportResolution && para.IsFilterResolution) // 过滤分辨率
        {
            if (Width < para.MinWidth || Height < para.MinHeight)
                isNotFilter = false;
        }

        if (config.IsSupportResolution) // 过滤图片方向
        {
            switch (para.Orientation)
            {
                case ImageOrientation.Landscape:
                    if (Height >= Width) isNotFilter = false;
                    break;
                case ImageOrientation.Portrait:
                    if (Height <= Width) isNotFilter = false;
                    break;
            }
        }

        IsLocalFilter = !isNotFilter;
    }

    #endregion

    #region 下载相关

    public bool CanDownload
    {
        get => _canDownload;
        set
        {
            _canDownload = value;
            OnPropertyChanged(nameof(CanDownload));
        }
    }


    /// <summary>
    ///     子项目专用 ---- 父级对象
    /// </summary>
    public MoeItem FatherItem { get; set; }

    /// <summary>
    ///     子项目专用--子项目所在列表中的位置（从1开始）
    /// </summary>
    public int SubIndex { get; set; }

    /// <summary>
    ///     Bitmap image 用于绑定显示下载图片图标
    /// </summary>
    public dynamic BitImg { get; set; }

    public async Task<Stream> TryLoadThumbnailStreamAsync(CancellationToken token)
    {
        var net = Site.GetCloneNet(ThumbnailUrlInfo.Referer, 20d);
        var url = ThumbnailUrlInfo.Url;
        var response = await net.Client.GetAsync(url, token);
        return await response.Content.ReadAsStreamAsync(token);
    }

    public bool IsResolveAndDownloadNextItem { get; set; }

    public Func<CancellationToken, Task<MoeItems>> GetNextItemsTaskFunc { get; set; }

    /// <summary>
    ///     网站上原始文件名
    /// </summary>
    public string OriginFileName { get; set; }

    /// <summary>
    ///     网站上原始文件名（不含后缀）
    /// </summary>
    public string OriginFileNameWithoutExtension { get; set; }

    private string _localFileShortNameWithoutExt;

    public string LocalFileShortNameWithoutExt
    {
        get => _localFileShortNameWithoutExt;
        set => SetField(ref _localFileShortNameWithoutExt, value, nameof(LocalFileShortNameWithoutExt));
    }

    public string LocalFileFullPath { get; set; }

    public event Action<MoeItem> DownloadStatusChanged;

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
            var strings = new[] {"", "", "", WebUtility.HtmlDecode("&#xF04D;"), "", "", ""};
            return strings[(int) DlStatus];
        }
    }

    private DownloadStatus _dlStatus = DownloadStatus.WaitForDownload;

    /// <summary>
    ///     设置或获取下载状态
    /// </summary>
    public DownloadStatus DlStatus
    {
        get => _dlStatus;
        set
        {
            var isChanged = value != _dlStatus;
            SetField(ref _dlStatus, value, nameof(DlStatus));
            if (isChanged)
            {
                OnPropertyChanged(nameof(DownloadStatusIconText));
                DownloadStatusChanged?.Invoke(this);
            }
        }
    }

    public void InitDownload(dynamic bitimg, int subindex = 0, MoeItem fatheritem = null)
    {
        BitImg = bitimg;
        if (ChildrenItems?.Count > 0)
        {
            LocalFileShortNameWithoutExt = "多张图片";
            if (!Title.IsEmpty()) LocalFileShortNameWithoutExt += $"({Title})";
            return;
        }

        SubIndex = subindex;
        FatherItem = fatheritem;
        if (DownloadUrlInfo == null) return;
        if (DownloadUrlInfo.ResolveUrlFunc == null)
        {
            OriginFileName = Path.GetFileName(DownloadUrlInfo.Url);
            OriginFileNameWithoutExtension = Path.GetFileNameWithoutExtension(DownloadUrlInfo.Url).ToDecodedUrl();
            var father = SubIndex == 0 ? null : FatherItem;
            GenFileNameWithoutExt(father);
            GenLocalFileFullPath(father);
        }
    }

    /// <summary>
    ///     当前下载指示器
    /// </summary>
    public CancellationTokenSource CurrentDownloadTaskCts { get; set; } = new();

    /// <summary>
    ///     异步下载图片
    /// </summary>
    /// <returns></returns>
    public async Task DownloadFileAsync(CancellationToken token)
    {
        if (ChildrenItems.Count > 0)
        {
            DlStatus = DownloadStatus.Downloading;
            var i = 0;
            while (true)
            {
                if (i >= ChildrenItemsCount ||
                    i > Set.DownloadFirstSeveralCount && Set.IsDownloadFirstSeveral) break;

                if (token.IsCancellationRequested)
                {
                    DlStatus = DownloadStatus.Cancel;
                    break;
                }

                Progress = i / (double) ChildrenItemsCount * 100d;
                StatusText = $"正在下载 {i + 1} / {ChildrenItemsCount} 张";
                if (i < Set.DownloadFirstSeveralCount || Set.IsDownloadFirstSeveral == false)
                {
                    var subItem = ChildrenItems[i];
                    try
                    {
                        await subItem.DownloadFileAsync(token);
                    }
                    catch (Exception e)
                    {
                        Ex.Log(e);
                        DlStatus = DownloadStatus.Failed;
                        i++;
                        continue;
                    }

                    // 解析下一个
                    if (subItem.IsResolveAndDownloadNextItem)
                        try
                        {
                            var newitems = await subItem.GetNextItemsTaskFunc(token);
                            var k = ChildrenItems.Count + 1;
                            foreach (var newitem in newitems)
                            {
                                ChildrenItems.Add(newitem);
                                newitem.InitDownload(null, k, this);
                                k++;
                            }
                        }
                        catch (Exception e)
                        {
                            DlStatus = DownloadStatus.Failed;
                            Ex.Log(e);
                            break;
                        }
                }

                i++;
            }

            var count = ChildrenItems.Count;
            if (DlStatus == DownloadStatus.Cancel)
            {
                StatusText = $"{count - 1}张成功，失败{ChildrenItemsCount - count}张";
            }
            else if (DlStatus == DownloadStatus.Failed)
            {
                StatusText = $"{count - 1}张成功，失败{ChildrenItemsCount - count}张";
            }
            else
            {
                DlStatus = DownloadStatus.Success;
                Progress = 100d;
                StatusText = $"{count} 张下载完成";
            }
        }
        else // 为子项目
        {
            try
            {
                await DownloadSingleFileAsync(token);
            }
            catch (Exception ex)
            {
                Ex.Log(ex);
                DlStatus = DownloadStatus.Failed;
                CurrentDownloadTaskCts = null;
            }
        }
    }

    public async Task DownloadSingleFileAsync(CancellationToken token)
    {
        var durl = DownloadUrlInfo;
        // url不正常判断
        if (durl == null)
        {
            DlStatus = DownloadStatus.Failed;
            StatusText = "下载失败";
            return;
        }

        // url解析、前置操作
        if (durl.ResolveUrlFunc != null)
            try
            {
                await durl.ResolveUrlFunc.Invoke(this, durl, token);
                durl.ResolveUrlFunc = null;
                InitDownload(BitImg, SubIndex, FatherItem);
            }
            catch (Exception e)
            {
                Ex.Log(e);
            }


        // 文件已存在判断、改名
        if (File.Exists(LocalFileFullPath))
        {
            if (Set.IsAutoRenameWhenSame)
            {
                var filename = AutoRenameFullPath();
                LocalFileFullPath = filename;
            }
            else
            {
                DlStatus = DownloadStatus.Skip;
                StatusText = "已存在，跳过";
                return;
            }
        }

        // 设置下载网络
        var net = Site.GetCloneNet(durl.Referer, 500d);
        net.ProgressMessageHandler.HttpReceiveProgress += (_, args) =>
        {
            Progress = args.ProgressPercentage;
            StatusText = $"正在下载：{Progress}%";
        };
        if (File.Exists(LocalFileFullPath))
        {
            DlStatus = DownloadStatus.Skip;
            StatusText = "已存在，跳过";
            return;
        }

        DlStatus = DownloadStatus.Downloading;
        var data = await net.GetAsync(durl.Url,false,3, token);

        // 写入文件
        var dir = Path.GetDirectoryName(LocalFileFullPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir ?? throw new InvalidOperationException());
        var file = new FileInfo(LocalFileFullPath);
        await using (var fileStream = file.Create())
        {
            await using var stream = await data.Content.ReadAsStreamAsync(token);
            var buffer = new byte[1024];
            int length;
            while ((length = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) != 0)
                await fileStream.WriteAsync(buffer.AsMemory(0, length), token);
        }

        // 下载完成后期效果
        if (durl.AfterEffectsFunc != null)
            try
            {
                await durl.AfterEffectsFunc.Invoke(this, token);
            }
            catch (Exception e)
            {
                Ex.Log(e);
            }

        // 完成
        Progress = 100d;
        StatusText = "下载完成";
        Ex.Log($"{durl.Url} download ok");
        DlStatus = DownloadStatus.Success;
    }


    private string _statusText;
    private int _favCount;
    private bool _isFav;
    private bool _canDownload;

    /// <summary>
    ///     状态文字
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value, nameof(StatusText));
    }

    public void GenLocalFileFullPath(MoeItem father = null)
    {
        var img = father ?? this;
        var format = Set.SortFolderNameFormat;
        var sub = format.IsEmpty() ? $"{img.Site.ShortName}" : FormatText(format, img, true);

        LocalFileFullPath =
            Path.Combine(Set.ImageSavePath, sub, $"{LocalFileShortNameWithoutExt}.{FileType?.ToLower()}");
    }

    public void GenFileNameWithoutExt(MoeItem father = null)
    {
        var img = father ?? this;

        var format = Set.SaveFileNameFormat;
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
        sb.Replace("%sitedispname", img.Site.DisplayName);
        sb.Replace("%id", $"{img.Id}");
        sb.Replace("%keyword", img.Para.Keyword.IsEmpty() ? "no-keyword" : img.Para.Keyword);
        var tags = string.Empty;
        var i = 0;
        foreach (var tag in img.Tags)
        {
            if (Set.NameFormatTagCount != 0)
                if (i >= Set.NameFormatTagCount)
                    break;
            tags += $"{tag} ";
            i++;
        }

        sb.Replace("%tag", $"{tags}");
        sb.Replace("%title", img.Title ?? "no-title");
        sb.Replace("%uploader_id", img.UploaderId ?? "no-uploader-id");
        sb.Replace("%uploader", img.Uploader ?? "no-uploader");
        sb.Replace("%date", img.DateString ?? "no-date");
        sb.Replace("%origin", OriginFileNameWithoutExtension);
        sb.Replace("%character", img.Character ?? "no-character");
        sb.Replace("%artist", img.Artist ?? "no-artist");
        sb.Replace("%copyright", img.Copyright ?? "no-copyright");
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            if (c == '\\' && isFolder) continue;
            sb.Replace($"{c}", "_");
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

    #endregion
}

public class MoeItems : ObservableCollection<MoeItem>
{
    public int FilterCount { get; set; }

    public new void Add(MoeItem item)
    {
        item.LocalFilter();
        if (item.IsLocalFilter) FilterCount++;
        base.Add(item);
    }

    public void AddRange(MoeItems items)
    {
        foreach (var moeItem in items) Add(moeItem);
    }
}