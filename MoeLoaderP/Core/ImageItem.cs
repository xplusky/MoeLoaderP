using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using MoeLoader.Core.Sites;

namespace MoeLoader.Core
{
    /// <summary>
    /// 表示一张图片（可以包含一组子图片册）及其相关信息
    /// </summary>
    public class ImageItem : BindingObject
    {
        public MoeSite Site { get; set; }
        public NetSwap Net { get; set; }
        public SearchPara Para { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
        public DateTime? CreatTime { get; set; }
        public string Author { get; set; }
        public int Score { get; set; }
        public string Source { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public bool IsExplicit { get; set; }

        public string DetailUrl { get; set; }

        public UrlInfo ThumbnailUrlInfo => Urls.GetMin();

        public UrlInfo DownloadUrlInfo
        {
            get
            {
                foreach (var urlInfo in Urls)
                {
                    if (urlInfo.Priority > 1 && urlInfo.Priority == Para.DownloadType.Priority)
                    {
                        return urlInfo;
                    }
                }

                return null;
            }
        }

        public UrlInfos Urls { get; set; } = new UrlInfos();

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

        public ImageItems ChilldrenItems { get; set; } = new ImageItems();
        public int ImagesCount => ChilldrenItems.Count;
        public Visibility ImagesCountVisibility => ChilldrenItems.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 获取详细信息委托 (图片的某些信息需要单独获取，例如原图URL可能位于详情页面）
        /// </summary>
        public Action GetDetailAction { get; set; }

        

        public async Task GetDetailAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    GetDetailAction?.Invoke();
                }
                catch (Exception e)
                {
                    App.Log(e);
                }
            });
        }

        public ImageItem(MoeSite site,SearchPara para) 
        {
            Site = site;
            Para = para;
            ChilldrenItems.CollectionChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(ImagesCount));
                OnPropertyChanged(nameof(ImagesCountVisibility));
            };
            Urls.CollectionChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(FileType));
                OnPropertyChanged(nameof(DownloadUrlInfo));
            };
        }

        public string GetFileType(string url)
        {
            var type = Path.GetExtension(DownloadUrlInfo.Url)?.Replace(".", "").ToUpper();
            if (type?.Contains("?") == true)
            {
                type = type.Split('?')[0];
            }
            return type?.Length < 5 ? type : null;
        }
    }

    public class ImageItems : ObservableCollection<ImageItem> { }

    public class UrlInfo
    {
        public string Name { get; set; }
        public int Priority { get; set; } //优先级， size 越大，数字越大,优先下载大的,从1开始
        public string Url { get; set; }
        public string Md5 { get; set; }
        public string Referer { get; set; }
        public ulong BiteSize { get; set; }

        public UrlInfo() { }

        public UrlInfo(string name, int priority, string url,string referer=null)
        {
            Name = name;
            Priority = priority;
            Url = url;
            if (referer != null) Referer = referer;
        }

        public string GetFileExtFromUrl()
        {
            if (string.IsNullOrEmpty(Url)) return null;
            var type = Path.GetExtension(Url)?.Replace(".", "").ToUpper();
            if (type?.Contains("?") == true)
            {
                type = type.Split('?')[0];
            }
            return type?.Length < 5 ? type : null;
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
                    info = i;continue;
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
