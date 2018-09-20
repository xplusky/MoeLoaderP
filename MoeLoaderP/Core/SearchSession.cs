using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MoeLoader.Core.Sites;

namespace MoeLoader.Core
{
    /// <summary>
    /// 一次搜索会话
    /// </summary>
    public class SearchSession
    {
        public Settings Settings { get; set; }
        public SearchPara CurrentSearchPara { get; set; }
        public SearchedPages LoadedPages { get; set; } = new SearchedPages();
        public Exception SearchException { get; set; }
        public bool IsSearching => CurrentSearchCts != null;
        public event Action<SearchSession,string> SearchStatusChanged;
        public event Action<SearchSession> SearchCompleted;
        public CancellationTokenSource CurrentSearchCts { get; set; }
        
        public SearchSession(Settings settings, SearchPara para)
        {
            Settings = settings;
            CurrentSearchPara = para;
        }

        /// <summary>
        /// 搜索下一页
        /// </summary>
        public async Task SearchNextPageAsync()
        {
            CurrentSearchCts?.Cancel();
            CurrentSearchCts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var mpage = new SearchedPage(); // 建立虚拟页信息
            ImageItems images;
            SearchPara temppara;
            if (LoadedPages.Count == 0)
            {
                temppara = CurrentSearchPara.Clone(); // 浅复制一份参数
                mpage.LastRealPageIndex = temppara.PageIndex;
                // 搜索起始页的所有图片（若网站查询参数支持条件过滤，则自动过滤）
                SearchStatusChanged?.Invoke(this, $"正在搜索站点 {temppara.Site.DisplayName} 第 {temppara.PageIndex} 页");
                images = await temppara.Site.GetRealPageImagesAsync(temppara);
            }
            else if (!LoadedPages.Last().HasNextPage) // 若无下一页则返回
            {
                CurrentSearchCts = null;
                return;
            }
            else
            {
                temppara = CurrentSearchPara.Clone(); // 浅复制一份参数
                temppara.PageIndex = LoadedPages.Last().LastRealPageIndex;

                // 若不是第一页则使用上一页搜索多出来的图片作为本页基数
                images = new ImageItems();
                foreach (var item in LoadedPages.Last().PreLoadNextPageItems)
                {
                    images.Add(item);
                }
            }

            Filter(images); // 本地过滤，images数量有可能减少

             // 进入 loop 循环
            var startTime = DateTime.Now;
            while (images.Count < temppara.Count) // 当images数量不够搜索参数数量时
            {
                if (DateTime.Now - startTime > TimeSpan.FromSeconds(20)) break; // loop超时跳出循环（即使不够也跳出）

                temppara.PageIndex++; // 设置新搜索参数为下一页（真）
                mpage.LastRealPageIndex = temppara.PageIndex;
                SearchStatusChanged?.Invoke(this, $"正在搜索站点 {temppara.Site.DisplayName} 第 {temppara.PageIndex} 页");
                var imagesNextRPage = await temppara.Site.GetRealPageImagesAsync(temppara); // 搜索下一页（真）的所有图片
                if (imagesNextRPage.Count == 0) // 当下一页（真）的搜索到的未过滤图片数量为0时，表示已经搜索完了
                {
                    mpage.HasNextPage = false; // 没有下一页
                    mpage.LastRealPageIndex = temppara.PageIndex;
                    break;
                }
                else // 当下一页（真）未过滤图片数量不为0时
                {
                    Filter(imagesNextRPage); // 本地过滤下一页（真）
                    foreach (var item in imagesNextRPage)
                    {
                        if (images.Count < temppara.Count) images.Add(item);
                        else mpage.PreLoadNextPageItems.Add(item);
                    }
                    if (images.Count >= temppara.Count) break; // 数量已够参数数量，当前虚拟页完成任务
                }
            }

            // Loadok
            mpage.ImageItems = images;
            LoadedPages.Add(mpage);
            CurrentSearchCts = null;
            SearchStatusChanged?.Invoke(this, "搜索完毕");
            SearchCompleted?.Invoke(this);
        }

        public void Filter(ImageItems items)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var del = false;
                var item = items[i];
                var state = item.Site.SurpportState;
                if (state.IsSupportRating)
                {
                    if ((!Settings.IsXMode || !CurrentSearchPara.IsShowExplicit) && item.IsExplicit) del = true;
                    if (Settings.IsXMode && CurrentSearchPara.IsShowExplicitOnly && item.IsExplicit == false) del = true;
                }
                if (state.IsSupportResolution)
                {
                    if (item.Width < CurrentSearchPara.MinWidth || item.Height < CurrentSearchPara.MinHeight) del = true;
                }
                if (!del) continue;
                items.RemoveAt(i);
                i--;
            }
        }

        public void StopSearch()
        {
            CurrentSearchCts?.Cancel();
        }

        
    }

    /// <summary>
    /// 搜索的一页（虚拟页）
    /// </summary>
    public class SearchedPage
    {
        public ImageItems ImageItems { get; set; }
        public ImageItems PreLoadNextPageItems { get; set; } = new ImageItems();
        public int LastRealPageIndex { get; set; }
        public bool HasNextPage { get; set; } = true;
    }

    public class SearchedPages : ObservableCollection<SearchedPage> { }

    /// <summary>
    /// 搜索参数
    /// </summary>
    public class SearchPara
    {
        public MoeSite Site { get; set; }
        public string Keyword { get; set; }
        public int PageIndex { get; set; }
        public int Count { get; set; }
        
        public bool IsShowExplicit { get; set; }
        public bool IsShowExplicitOnly { get; set; }

        public bool IsFilterResolution { get; set; }
        public int MinWidth { get; set; }
        public int MinHeight { get; set; }

        public bool IsFilterFileType { get; set; }
        public string FilterFileTpyeText { get; set; }

        public SearchPara Clone()
        {
            return (SearchPara) MemberwiseClone();
        }
    }
}
