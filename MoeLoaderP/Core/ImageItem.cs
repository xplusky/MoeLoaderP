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
    /// 表示一张图片（或一组图片）及其相关信息
    /// </summary>
    public class ImageItem : NotifyBase
    {
        public MoeSite Site { get; set; }

        public MoeNet Net { get; set; }

        public string Referer { get; set; }

        /// <summary>
        /// 原图地址
        /// </summary>
        public string OriginalUrl { get; set; }

        /// <summary>
        /// 预览图地址，尺寸位于原图与缩略图之间
        /// </summary>
        public string PreviewUrl { get; set; }

        /// <summary>
        /// 文件类型
        /// </summary>
        public string ImageFileType
        {
            get
            {
                var type = Path.GetExtension(OriginalUrl)?.Replace(".", "").ToUpper();
                return type?.Length < 5 ? type : null;
            }
        }

        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 原图宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 原图高度
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 缩略图地址
        /// </summary>
        public string ThumbnailUrl { get; set; }

        /// <summary>
        /// 多图时候所有图片列表
        /// </summary>
        public ImageItems ChilldrenItems { get; set; } = new ImageItems();
        public int ImagesCount => ChilldrenItems.Count;
        public Visibility ImagesCountVisibility => ChilldrenItems.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 图片创建日期
        /// </summary>
        public string Date { get; set; }

        public DateTime? CreatTime { get; set; }

        /// <summary>
        /// 图片Tags
        /// </summary>
        public string TagsText { get; set; }

        /// <summary>
        /// 原图文件尺寸
        /// </summary>
        public string FileSize { get; set; }

        public ulong FileBiteSize { get; set; }

        /// <summary>
        /// 图片id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 图片得分
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 图片来源
        /// </summary>
        public string Source { get; set; }
        
        /// <summary>
        /// 图片描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// JPEG格式原图地址（非所有站点支持，不支持时请与 OriginalUrl 保持一致）
        /// </summary>
        public string JpegUrl { get; set; }

        /// <summary>
        /// 原图分辨率
        /// </summary>
        public string DimensionString
        {
            get
            {
                if (Width != 0 && Height != 0) return $"{Width} × {Height}";
                return null;
            }
        }

        /// <summary>
        /// 是否Explicit评级
        /// </summary>
        public bool IsExplicit { get; set; }

        /// <summary>
        /// 图片详情页地址
        /// </summary>
        public string DetailUrl { get; set; }

        /// <summary>
        /// 作品作者
        /// </summary>
        public string Author { get; set; } = "UnknownAuthor";
        
        /// <summary>
        /// 不对下载的图标进行完整性验证（对于无法获取原文件大小的站点）
        /// </summary>
        public bool NoVerify { get; set; }

        public string Md5 { get; set; }

        /// <summary>
        /// 若图片的某些信息需要单独获取（例如原图URL可能位于第二层页面），则实现该接口，将网络操作、提取信息操作置于此处
        /// </summary>
        public Action GetDetailAction { get; set; }

        /// <summary>
        /// 异步获取子页信息
        /// </summary>
        /// <returns></returns>
        public async Task GetDetailAsync()
        {
            if(GetDetailAction!=null) await Task.Run(GetDetailAction);
        }
    }

    public class ImageItems : ObservableCollection<ImageItem> { }
}
