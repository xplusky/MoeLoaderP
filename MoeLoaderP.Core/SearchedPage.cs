using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 真实页
    /// </summary>
    public class SearchedPage : MoeItems
    {
        public SearchPara Para { get; set; }
        public SearchPara NextPagePara { get; set; }
        public bool? HasNextPage { get; set; } 
        public string NextPageIndexCursor { get; set; }
        public int? TotalPageCount { get; set; }
        public string Message { get; set; }
        public Exception SearchException { get; set; }
        

        public void GenNextPagePara()
        {
            if(HasNextPage == false) return;
            var newPara = Para.Clone();
            if (newPara.PageIndex != null) newPara.PageIndex++;
            newPara.PageIndexCursor = NextPageIndexCursor;
            NextPagePara = newPara;
        }
    }

    public class SearchedPages : ObservableCollection<SearchedPage> { }

    /// <summary>
    /// 搜索结果的其中一页（虚拟页）
    /// </summary>
    public class SearchedVisualPage : MoeItems
    {
        private int _pageDisplayIndex;
        private bool _isCurrentPage;

        public bool IsCurrentPage
        {
            get => _isCurrentPage;
            set { _isCurrentPage = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsCurrentPage)));}
        }

        public bool IsSearchComplete { get; set; } = false;

        public int PageDisplayIndex
        {
            get => _pageDisplayIndex;
            set 
            { 
                _pageDisplayIndex = value; 
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(PageDisplayIndex)));
            }
        }


        public void LoadStart()
        {
            LoadStartEvent?.Invoke(this);
        }
        public void LoadEnd()
        {
            LoadEndEvent?.Invoke(this);
        }
        public event Action<SearchedVisualPage> LoadStartEvent;
        public event Action<SearchedVisualPage> LoadEndEvent;

        public new event PropertyChangedEventHandler PropertyChanged
        {
            add => base.PropertyChanged += value;
            remove => base.PropertyChanged -= value;
        }

        /// <summary>
        /// 本地过滤图片
        /// </summary>
        public bool FilterAdd(MoeItem item)
        {
            var add = true;
            var para = item.Para;
            var config = item.Site.Config;
            var set = item.Site.Settings;
            if (config.IsSupportRating) // 过滤r18评级图片
            {
                if ((!set.IsXMode || !para.IsShowExplicit) && item.IsExplicit) add = false;
                if (set.IsXMode && para.IsShowExplicitOnly && item.IsExplicit == false) add = false;
            }
            if (config.IsSupportResolution && para.IsFilterResolution) // 过滤分辨率
            {
                if (item.Width < para.MinWidth || item.Height < para.MinHeight) add = false;
            }
            if (config.IsSupportResolution) // 过滤图片方向
            {
                switch (para.Orientation)
                {
                    case ImageOrientation.Landscape:
                        if (item.Height >= item.Width) add = false;
                        break;
                    case ImageOrientation.Portrait:
                        if (item.Height <= item.Width) add = false;
                        break;
                }
            }
            if (para.IsFilterFileType) // 过滤图片扩展名
            {
                foreach (var s in para.FilterFileTypeText.Split(';'))
                {
                    if (s.IsEmpty()) continue;
                    if (string.Equals(item.FileType, s, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (!para.IsFileTypeShowSpecificOnly) add = false;
                    }
                    else if (para.IsFileTypeShowSpecificOnly)
                    {
                        add = false;
                    }
                }
            }

            if (add) Add(item);
            return add;
        }
    }

    public sealed class SearchedVisualPages : ObservableCollection<SearchedVisualPage>
    {
        private int _currentPageIndex;

        public SearchedVisualPages()
        {
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurrentPageIndex))
            {
                foreach (var page in Items)
                {
                    page.IsCurrentPage = false;

                }

                if (CurrentPageIndex < Items.Count) Items[CurrentPageIndex].IsCurrentPage = true;
            }
        }

        public int CurrentPageIndex
        {
            get => _currentPageIndex;
            set
            {
                if(value.Equals(_currentPageIndex)) return;
                _currentPageIndex = value;
                OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(CurrentPageIndex)));
            }
        }

        public new void Add(SearchedVisualPage page)
        {
            base.Add(page);
            page.PageDisplayIndex = Count;
            AddEvent?.Invoke(page);
        }

        public event Action<SearchedVisualPage> AddEvent;

        public new event PropertyChangedEventHandler PropertyChanged
        {
            add => base.PropertyChanged += value;
            remove => base.PropertyChanged -= value;
        }
    }
}