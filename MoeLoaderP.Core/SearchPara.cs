using System;
using System.Collections.ObjectModel;
using MoeLoaderP.Core.Sites;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 搜索参数
    /// </summary>
    public class SearchPara
    {
        public MoeSite Site { get; set; }
        //public SearchSession CurrentSearch { get; set; }
        public string Keyword { get; set; }
        public int StartPageIndex { get; set; }
        public string NextPageMark { get; set; }
        public int Count { get; set; }

        public bool IsShowExplicit { get; set; }
        public bool IsShowExplicitOnly { get; set; }

        public bool IsFilterResolution { get; set; }
        public int MinWidth { get; set; }
        public int MinHeight { get; set; }

        public bool IsFilterFileType { get; set; }
        public string FilterFileTypeText { get; set; }
        public bool IsFileTypeShowSpecificOnly { get; set; }

        public ImageOrientation Orientation { get; set; } = ImageOrientation.None;
        public DownloadType DownloadType { get; set; }

        public DateTime? Date { get; set; }

        public int Lv2MenuIndex { get; set; }
        public int Lv3MenuIndex { get; set; }
        public int Lv4MenuIndex { get; set; }

        public MoeSiteConfig Config { get; set; }
        public MirrorSiteConfig MirrorSite { get; set; }

        public SearchPara Clone() => (SearchPara)MemberwiseClone();
    }

    public enum ImageOrientation
    {
        None = 0,
        Landscape = 1,
        Portrait = 2
    }

    public enum ImageOrderBy
    {
        Id,
        IdDescending,
        Date,
        DateDescending,
        Popular,
        PopularDescending
    }

    public class ImageOrder
    {
        public string Name { get; set; }
        public ImageOrderBy Order { get; set; }
    }

    public class ImageOrders: ObservableCollection<ImageOrder>{}
}