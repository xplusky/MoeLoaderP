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
        public string ThumbnailReferer { get; set; }
        public string ThumbnailUrl { get; set; } // 最小
        public string PreviewUrl { get; set; } // 中等
        public string JpegUrl { get; set; }
        public string FileReferer { get; set; }
        private string _fileUrl;

        public string FileUrl //最大
        {
            get => _fileUrl;
            set
            {
                _fileUrl = value; 
                OnPropertyChanged(nameof(FileUrl));
                OnPropertyChanged(nameof(FileType));
            } 
        }

        public string FileType
        {
            get
            {
                var type = Path.GetExtension(FileUrl)?.Replace(".", "").ToUpper();
                if (type?.Contains("?") == true)
                {
                    type = type.Split('?')[0];
                }
                return type?.Length < 5 ? type : null;
            }
        }
        public string FileSize { get; set; }
        public ulong FileBiteSize { get; set; }
        public string FileMd5 { get; set; }
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
        /// 获取详细信息委托 (若图片的某些信息需要单独获取，例如原图URL可能位于详情页面）
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

        public ImageItem()
        {
            ChilldrenItems.CollectionChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(ImagesCount));
                OnPropertyChanged(nameof(ImagesCountVisibility));
            };
        }
    }

    public class ImageItems : ObservableCollection<ImageItem> { }
}
