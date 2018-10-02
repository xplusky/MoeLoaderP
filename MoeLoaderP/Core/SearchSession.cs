using System;
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
        public bool IsSearching => CurrentSearchCts != null;
        public event Action<SearchSession,string> SearchStatusChanged;
        public CancellationTokenSource CurrentSearchCts { get; set; }
        
        public SearchSession(Settings settings, SearchPara para)
        {
            Settings = settings;
            CurrentSearchPara = para;
        }

        public async Task<Task> TrySearchNextPageAsync()
        {
            CurrentSearchCts?.Cancel();
            CurrentSearchCts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var t = SearchNextPageAsync();
            try { await t; }
            catch
            {
                // ignored
            }
            CurrentSearchCts = null;
            return t;
        }

        public void SearchStatusChange(string message)
        {
            SearchStatusChanged?.Invoke(this,message);
        }

        /// <summary>
        /// 搜索下一页
        /// </summary>
        public async Task SearchNextPageAsync()
        {
            var token = CurrentSearchCts.Token;
            var mpage = new SearchedPage(); // 建立虚拟页信息
            var images = new ImageItems();
            SearchPara temppara;
            if (LoadedPages.Count == 0)
            {
                temppara = CurrentSearchPara.Clone(); // 浅复制一份参数
                mpage.LastRealPageIndex = temppara.PageIndex;
                // 搜索起始页的所有图片（若网站查询参数有支持的条件过滤，则在搜索时就已自动过滤相关条件）
                SearchStatusChange($"正在搜索站点 {temppara.Site.DisplayName} 第 {temppara.PageIndex} 页");
                var imagesOrg = await temppara.Site.GetRealPageImagesAsync(temppara,token);
                if (imagesOrg == null || imagesOrg.Count == 0)
                {
                    App.ShowMessage("无搜索结果");
                    SearchStatusChange("无搜索结果");
                    return;
                }
                for (var i = 0; i < imagesOrg.Count; i++)
                {
                    var item = imagesOrg[i];
                    if (i < temppara.Count) images.Add(item);
                    else
                    {
                        mpage.PreLoadNextPageItems.Add(item);
                        if(!mpage.HasNextPage) mpage.HasNextPage = true;
                    }
                }
            }
            else if (!LoadedPages.Last().HasNextPage) // 若无下一页则返回
            {
                return;
            }
            else
            {
                temppara = CurrentSearchPara.Clone(); // 浅复制一份参数
                temppara.PageIndex = LoadedPages.Last().LastRealPageIndex;

                // 若不是第一页则使用上一页搜索多出来的图片作为本页基数
                images = new ImageItems();
                for (var i = 0; i < LoadedPages.Last().PreLoadNextPageItems.Count; i++)
                {
                    var item = LoadedPages.Last().PreLoadNextPageItems[i];
                    if (i < temppara.Count) images.Add(item);
                    else
                    {
                        mpage.PreLoadNextPageItems.Add(item);
                        mpage.HasNextPage = true;
                    }
                }
            }

            Filter(images); // 本地过滤，images数量有可能减少

             // 进入 loop 循环
            var startTime = DateTime.Now;
            while (images.Count < temppara.Count) // 当images数量不够搜索参数数量时循环
            {
                token.ThrowIfCancellationRequested(); // 整体Task的取消Token，取消时会抛出异常
                
                temppara.PageIndex++; // 设置新搜索参数为下一页（真）
                mpage.LastRealPageIndex = temppara.PageIndex;
                SearchStatusChange($"正在搜索站点 {temppara.Site.DisplayName} 第 {temppara.PageIndex} 页");
                var imagesNextRPage = await temppara.Site.GetRealPageImagesAsync(temppara,token); // 搜索下一页（真）的所有图片
                if (imagesNextRPage==null || imagesNextRPage.Count == 0) // 当下一页（真）的搜索到的未进行本地过滤图片数量为0时，表示已经搜索完了
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
                        if (images.Count < temppara.Count) images.Add(item); // 添加图片数量直到够参数设定的图片数量为止
                        else mpage.PreLoadNextPageItems.Add(item); // 多出来的图片存在另一个对象中，下一虚拟页可以调用
                    }
                    if (images.Count >= temppara.Count) break; // 数量已够参数数量，当前虚拟页完成任务
                }
                if (DateTime.Now - startTime > TimeSpan.FromSeconds(20)) break; // loop超时跳出循环（即使不够数量也跳出）
            }
            token.ThrowIfCancellationRequested();
            // Loadend
            mpage.ImageItems = images;
            LoadedPages.Add(mpage);
            SearchStatusChange("搜索完毕");
        }

        /// <summary>
        /// 本地过滤图片
        /// </summary>
        public void Filter(ImageItems items)
        {
            if (items == null) return;
            var para = CurrentSearchPara;
            for (var i = 0; i < items.Count; i++)
            {
                var del = false;
                var item = items[i];
                var state = item.Site.SurpportState;
                if (state.IsSupportRating) // 过滤Explicit评级图片
                {
                    if ((!Settings.IsXMode || !para.IsShowExplicit) && item.IsExplicit) del = true;
                    if (Settings.IsXMode && para.IsShowExplicitOnly && item.IsExplicit == false) del = true;
                }
                if (state.IsSupportResolution && para.IsFilterResolution) // 过滤分辨率
                {
                    if (item.Width < para.MinWidth || item.Height < para.MinHeight) del = true;
                }
                if (state.IsSupportResolution) // 过滤图片方向
                {
                    switch (para.Orientation)
                    {
                        case ImageOrientation.Landscape:
                            if (item.Height >= item.Width) del = true;
                            break;
                        case ImageOrientation.Portrait:
                            if (item.Height <= item.Width) del = true;
                            break;
                    }
                }
                if (para.IsFilterFileType) // 过滤图片扩展名
                {
                    foreach (var s in para.FilterFileTpyeText.Split(';'))
                    {
                        if (string.IsNullOrWhiteSpace(s)) continue;
                        if (string.Equals(item.FileType, s, StringComparison.CurrentCultureIgnoreCase)) del = true;
                    }
                }
                if (!del) continue;
                items.RemoveAt(i);
                i--;
            }
        }

        public void StopSearch()
        {
            CurrentSearchCts?.Cancel();
            CurrentSearchCts = null;
        }
    }

    /// <summary>
    /// 搜索的其中一页（虚拟页）
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

        public ImageOrientation Orientation { get; set; } = ImageOrientation.None;
        
        public SearchPara Clone()
        {
            return (SearchPara) MemberwiseClone(); // 浅克隆
        }
    }

    public enum ImageOrientation
    {
        None = 0,
        Landscape = 1,
        Portrait = 2
    }
}
