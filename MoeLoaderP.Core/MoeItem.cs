using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MoeLoaderP.Core.Sites;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 表示一张图片（可以包含一组子图片册）及其相关信息
    /// </summary>
    public class MoeItem : BindingObject
    {
        public MoeSite Site { get; set; }
        public NetDocker Net { get; set; }
        public SearchPara Para { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }

        public string DateString
        {
            get => string.IsNullOrWhiteSpace(_dateString) ? Date?.ToString("G", new CultureInfo("zh-CN")) : null;
            set => _dateString = value;
        }

        public DateTime? Date { get; set; }
        /// <summary>
        /// 上传用户
        /// </summary>
        public string Uploader { get; set; }
        /// <summary>
        /// 上传用户ID
        /// </summary>
        public string UploaderId { get; set; }
        public double Score { get; set; }
        public int Rank { get; set; }
        public bool TipHighLight { get; set; }
        public string Tip { get; set; }
        public string Source { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public bool IsExplicit { get; set; }
        public string DetailUrl { get; set; }
        public string OriginString { get; set; }
        /// <summary>
        /// 作品名
        /// </summary>
        public string Copyright { get; set; }
        /// <summary>
        /// 角色名
        /// </summary>
        public string Character { get; set; }
        /// <summary>
        /// 画师名
        /// </summary>
        public string Artist { get; set; }

        public UrlInfo ThumbnailUrlInfo => Urls.GetMin();

        public UrlInfo DownloadUrlInfo => Urls.FirstOrDefault(urlInfo => urlInfo.Priority > 1 && urlInfo.Priority == Para.DownloadType.Priority);

        public UrlInfos Urls { get; set; } = new UrlInfos();
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

        public string ResolutionText
        {
            get
            {
                if (Width != 0 && Height != 0) return $"{Width} × {Height}";
                return null;
            }
        }

        public MoeItems ChildrenItems { get; set; } = new MoeItems();

        private int _imageCount;
        private string _dateString;

        public int ImagesCount
        {
            get => _imageCount == 0 ? ChildrenItems.Count : _imageCount;
            set => _imageCount = value;
        }
        
        /// <summary>
        /// 获取详细信息Task委托 (图片的某些信息需要单独获取，例如原图URL可能位于详情页面）
        /// </summary>
        public Func<Task> GetDetailTaskFunc { get; set; }

        public async Task TryGetDetailTask()
        {
            try
            {
                await GetDetailTaskFunc();
            }
            catch (Exception e)
            {
                Extend.Log($"获取详情页失败!ID:{Id},PAGE:{DetailUrl}",e);
            }
        }

        public MoeItem(MoeSite site, SearchPara para)
        {
            Site = site;
            Para = para;
            ChildrenItems.CollectionChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(ImagesCount));
            };
            Urls.CollectionChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(FileType));
                OnPropertyChanged(nameof(DownloadUrlInfo));
            };
        }
    }

    public class MoeItems : ObservableCollection<MoeItem>
    {
        public Exception Ex { get; set; }
        public string Message { get; set; }
        public enum ResponseMode { Ok, Fail, OkAndOver }
        public ResponseMode Response { get; set; }
    }

    public class TextFileInfo
    {
        public string FileExt { get; set; }
        public string Content { get; set; }
    }

    public class UrlInfo
    {
        public string Name { get; set; }
        /// <summary>
        /// 优先级， size 越大，数字越大,优先下载大的,从1开始
        /// </summary>
        public int Priority { get; set; }
        public string Url { get; set; }
        public string Md5 { get; set; }
        public string Referer { get; set; }
        public ulong BiteSize { get; set; }

        public UrlInfo(string name, int priority, string url, string referer = null)
        {
            Name = name;
            Priority = priority;
            Url = url;
            if (referer != null) Referer = referer;
        }

        public string GetFileExtFromUrl()
        {
            if (string.IsNullOrEmpty(Url)) return null;
            var type = Path.GetExtension(Url).Replace(".", "").ToUpper();
            if (type.Contains("?"))
            {
                type = type.Split('?')[0];
            }
            return type.Length < 5 ? type : null;
        }

    }

    public class UrlInfos : ObservableCollection<UrlInfo>
    {
        public UrlInfo GetMax()
        {
            UrlInfo info = null;
            foreach (var i in this)
            {
                if (info == null)
                {
                    info = i; continue;
                }

                if (i.Priority > info.Priority) info = i;
            }

            return info;
        }

        public UrlInfo GetMin()
        {
            UrlInfo info = null;
            foreach (var i in this)
            {
                if (info == null)
                {
                    info = i; continue;
                }

                if (i.Priority < info.Priority) info = i;
            }

            return info;
        }
    }
}
