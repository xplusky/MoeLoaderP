using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core;

/// <summary>
///     MoeItem下载链接
/// </summary>
public class UrlInfo
{
    public UrlInfo(DownloadTypeEnum priority, string url, string referer = null,
        AfterEffectsDelegate afterEffects = null, ResolveUrlDelegate resolveUrlFunc = null, ulong fileSize = 0)
    {
        DownloadType = priority;
        Url = url;
        if (referer != null) Referer = referer;
        if (afterEffects != null) AfterEffectsFunc = afterEffects;
        ResolveUrlFunc = resolveUrlFunc;
        FileSize = fileSize;
    }

    public DownloadTypeEnum DownloadType { get; set; }
    public string Url { get; set; }
    public string Md5 { get; set; }
    public string Referer { get; set; }
    public ulong FileSize { get; set; }

    /// <summary>
    ///     下载完后处理代理
    /// </summary>
    public AfterEffectsDelegate AfterEffectsFunc { get; set; }

    /// <summary>
    ///     下载前解析出下载地址
    /// </summary>
    public ResolveUrlDelegate ResolveUrlFunc { get; set; }

    public string FormattedFileSize
    {
        get
        {
            var size = FileSize;
            if (size == 0) return null;
            var temp = size / 1024d;
            if (temp < 1024) return $"{Math.Round(temp)}kB";
            temp /= 1024d;
            return $"{Math.Round(temp, 2)}MB";
        }
    }

    //public string GetFileExtFromUrl()
    //{
    //    if (Url.IsEmpty()) return null;
    //    var type = Path.GetExtension(Url)?.Delete(".").ToUpper();
    //    if (type == null) return null;
    //    if (type.Contains("?"))
    //    {
    //        type = type.Split('?')[0];
    //    }
    //    return type.Length < 5 ? type : null;
    //}
    public string GetFileExtFromUrl()
    {
        if (Url.IsEmpty()) return null;
        string url;
        if (Url.Contains('?'))
        {
            url = Url.Split('?')[0];
            if (url[^1] == '/') url = url[..^1];
        }
        else
        {
            url = Url;
        }

        var type = Path.GetExtension(url)?.Delete(".").ToLower();
        return type?.Length < 5 ? type : null;
    }
}

public class UrlInfos : ObservableCollection<UrlInfo>
{
    public UrlInfo GetPreview()
    {
        if (Count == 0) return null;
        if (Count == 1) return this.FirstOrDefault();
        var min = GetMin();
        foreach (var urlInfo in this.OrderBy(u => u.DownloadType))
            if (urlInfo.DownloadType > min.DownloadType)
                return urlInfo;
        return null;
    }

    public UrlInfo GetMin()
    {
        UrlInfo info = null;
        foreach (var i in this)
        {
            if (info == null)
            {
                info = i;
                continue;
            }

            if (i.DownloadType < info.DownloadType) info = i;
        }

        return info;
    }

    public void Add(DownloadTypeEnum p, string url, string referer = null, AfterEffectsDelegate afterEffects = null,
        ResolveUrlDelegate resolveUrlFunc = null, ulong filesize = 0)
    {
        var urlinfo = new UrlInfo(p, url, referer, afterEffects, resolveUrlFunc, filesize);
        Add(urlinfo);
    }

    public void Add(int p, string url, string referer = null, AfterEffectsDelegate afterEffects = null,
        ResolveUrlDelegate resolveUrlFunc = null, ulong filesize = 0)
    {
        var urlinfo = new UrlInfo((DownloadTypeEnum) p, url, referer, afterEffects, resolveUrlFunc, filesize);
        Add(urlinfo);
    }
}

public delegate Task AfterEffectsDelegate(MoeItem item, CancellationToken token);

public delegate Task ResolveUrlDelegate(MoeItem item, UrlInfo url, CancellationToken token);

public class TagInfo
{
    public TagInfo()
    {
    }

    public TagInfo(string tagName, int id = default)
    {
        NameEn = tagName;
        Id = id;
    }

    public int Id { get; set; }
    public string Name => NameCn.IsEmpty() ? NameEn : NameCn;
    public string NameEn { get; set; }
    public string NameJp { get; set; }
    public string NameCn { get; set; }
    public int PicTotalCount { get; set; }

    public override string ToString()
    {
        return Name;
    }
}

public class TagInfos : List<TagInfo>
{
    public void AddTag(string name)
    {
        var tag = new TagInfo {NameEn = name};
        Add(tag);
    }
}

public class TextFileInfo
{
    public string FileExt { get; set; }
    public string Content { get; set; }
}

public class DownloadType
{
    public string Name { get; set; }
    public DownloadTypeEnum Type { get; set; }
}

public class DownloadTypes : ObservableCollection<DownloadType>
{
    public void Add(string name, DownloadTypeEnum pr)
    {
        Add(new DownloadType
        {
            Name = name,
            Type = pr
        });
    }

    public void AddAuto()
    {
        var item = new DownloadType
        {
            Name = "自动（优先大图）",
            Type = DownloadTypeEnum.Auto
        };
        Insert(0, item);
    }
}

public enum DownloadTypeEnum
{
    /// <summary>
    ///     缩略图
    /// </summary>
    Thumbnail = 0,

    /// <summary>
    ///     小图
    /// </summary>
    Small = 1,

    /// <summary>
    ///     预览图或中等图
    /// </summary>
    Medium = 2,

    /// <summary>
    ///     大图或JPEG图
    /// </summary>
    Large = 3,

    /// <summary>
    ///     原图
    /// </summary>
    Origin = 4,
    Auto = -1
}