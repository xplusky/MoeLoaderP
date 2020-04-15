using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MoeLoaderP.Core.Sites;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 一次搜索会话
    /// </summary>
    public class SearchSession
    {
        public Settings Settings { get; set; }
        public SearchPara CurrentSearchPara { get; set; }
        public SearchedPages LoadedPages { get; set; } = new SearchedPages();
        public int CurrentPageIndex { get; set; }
        public bool IsSearching => SearchingTasksCts.Count > 0;
        public List<CancellationTokenSource> SearchingTasksCts { get; set; } = new List<CancellationTokenSource>();

        public SearchSession(Settings settings, SearchPara para)
        {
            Settings = settings;
            CurrentSearchPara = para;
        }

        public async Task<Task> TrySearchNextPageAsync()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var t = SearchNextPageAsync(cts.Token);
            SearchingTasksCts.Add(cts);
            try
            {
                await t;
            }
            catch (OperationCanceledException)
            {
                Extend.ShowMessage("搜索已取消");
            }
            catch (Exception ex)
            {
                Extend.ShowMessage(ex.Message,ex.ToString(), Extend.MessagePos.Window);
                Extend.Log(ex.Message, ex.StackTrace);
            }

            SearchingTasksCts.Remove(cts);
            return t;
        }


        /// <summary>
        /// 搜索下一页
        /// </summary>
        public async Task SearchNextPageAsync(CancellationToken token)
        {
            var mPage = new SearchedPage(); // 建立虚拟页信息
            var images = new MoeItems();
            SearchPara tempPara;
            if (LoadedPages.Count == 0)
            {
                tempPara = CurrentSearchPara.Clone(); // 浅复制一份参数
                mPage.LastRealPageIndex = tempPara.PageIndex;
                // 搜索起始页的所有图片（若网站查询参数有支持的条件过滤，则在搜索时就已自动过滤相关条件）
                var sb = new StringBuilder();
                sb.AppendLine($"正在搜索站点 {tempPara.Site.DisplayName} 第 {tempPara.PageIndex} 页图片");
                sb.Append($"(参数：kw：{(!string.IsNullOrWhiteSpace(tempPara.Keyword) ? tempPara.Keyword : "N/A")},num:{tempPara.Count})");
                Extend.ShowMessage(sb.ToString(), null, Extend.MessagePos.Searching);
                var imagesOrg = await tempPara.Site.GetRealPageImagesAsync(tempPara, token);
                if (imagesOrg == null || imagesOrg.Count == 0)
                {
                    Extend.ShowMessage("无搜索结果",null, Extend.MessagePos.Searching);
                    return;
                }
                for (var i = 0; i < imagesOrg.Count; i++)
                {
                    var item = imagesOrg[i];
                    if (i < tempPara.Count) images.Add(item);
                    else
                    {
                        mPage.PreLoadNextPageItems.Add(item);
                        if (!mPage.HasNextPage) mPage.HasNextPage = true;
                    }
                }
            }
            else if (!LoadedPages.Last().HasNextPage) // 若无下一页则返回
            {
                return;
            }
            else
            {
                tempPara = CurrentSearchPara.Clone(); // 浅复制一份参数
                tempPara.PageIndex = LoadedPages.Last().LastRealPageIndex;

                // 若不是第一页则使用上一页搜索多出来的图片作为本页基数
                images = new MoeItems();
                for (var i = 0; i < LoadedPages.Last().PreLoadNextPageItems.Count; i++)
                {
                    var item = LoadedPages.Last().PreLoadNextPageItems[i];
                    if (i < tempPara.Count) images.Add(item);
                    else
                    {
                        mPage.PreLoadNextPageItems.Add(item);
                        mPage.HasNextPage = true;
                    }
                }
            }

            Filter(images); // 本地过滤，images数量有可能减少

            // 进入 loop 循环
            var startTime = DateTime.Now;
            while (images.Count < tempPara.Count) // 当images数量不够搜索参数数量时循环
            {
                token.ThrowIfCancellationRequested(); // 整体Task的取消Token，取消时会抛出异常

                tempPara.PageIndex++; // 设置新搜索参数为下一页（真）
                mPage.LastRealPageIndex = tempPara.PageIndex;
                var sb = new StringBuilder();
                sb.AppendLine($"正在搜索站点 {tempPara.Site.DisplayName} 第 {tempPara.PageIndex} 页图片");
                sb.AppendLine($"已获取第{tempPara.PageIndex - 1}页{images.Count}张图片，还需{tempPara.Count - images.Count}张");
                sb.Append($"(参数：kw：{(!string.IsNullOrWhiteSpace(tempPara.Keyword) ? tempPara.Keyword : "N/A")},num:{tempPara.Count})");
                Extend.ShowMessage(sb.ToString(), null, Extend.MessagePos.Searching);
                var imagesNextRPage = await tempPara.Site.GetRealPageImagesAsync(tempPara, token); // 搜索下一页（真）的所有图片
                if (imagesNextRPage == null || imagesNextRPage.Count == 0) // 当下一页（真）的搜索到的未进行本地过滤图片数量为0时，表示已经搜索完了
                {
                    mPage.HasNextPage = false; // 没有下一页
                    mPage.LastRealPageIndex = tempPara.PageIndex;
                    break;
                }
                else // 当下一页（真）未过滤图片数量不为0时
                {
                    Filter(imagesNextRPage); // 本地过滤下一页（真）
                    
                    foreach (var item in imagesNextRPage)
                    {
                        if (images.Count < tempPara.Count) images.Add(item); // 添加图片数量直到够参数设定的图片数量为止
                        else mPage.PreLoadNextPageItems.Add(item); // 多出来的图片存在另一个对象中，下一虚拟页可以调用
                    }
                    if (images.Count >= tempPara.Count) break; // 数量已够参数数量，当前虚拟页完成任务
                }
                if (DateTime.Now - startTime > TimeSpan.FromSeconds(30)) break; // loop超时跳出循环（即使不够数量也跳出）
            }
            token.ThrowIfCancellationRequested();
            // Load end
            mPage.ImageItems = images;
            LoadedPages.Add(mPage);
            if(images.Message!=null) Extend.ShowMessage(images.Message);
            if(images.Ex!=null) Extend.ShowMessage(images.Ex.Message,images.Ex.ToString(), Extend.MessagePos.Window);
            Extend.ShowMessage("搜索完毕", null, Extend.MessagePos.Searching);
        }

        /// <summary>
        /// 本地过滤图片
        /// </summary>
        public void Filter(MoeItems items)
        {
            if (items == null) return;
            var para = CurrentSearchPara;
            for (var i = 0; i < items.Count; i++)
            {
                var del = false;
                var item = items[i];
                var state = item.Site.SupportState;
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
                        if (string.Equals(item.FileType, s, StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (!para.IsFileTypeShowSpecificOnly) del = true;
                        }
                        else if (para.IsFileTypeShowSpecificOnly)
                        {
                            del = true;
                        }
                    }
                }

                // 过滤重复图片
                // 去除与上一页重复的
                // if (LoadedPages.Any() && LoadedPages.Last().ImageItems.Has(item)) del = true;

                if (!del) continue;
                items.RemoveAt(i);
                i--;
            }
        }

        public void StopSearch()
        {
            foreach (var cts in SearchingTasksCts)
            {
                cts?.Cancel();
            }
            SearchingTasksCts.Clear();
        }

        public string GetCurrentSearchStateText()
        {
            var para = CurrentSearchPara;
            var site = CurrentSearchPara.Site;
            var sb = $"当前搜索：{site.DisplayName}";
            if (site.SubMenu.Count > 0 && para.SubMenuIndex > -1)
            {
                var lv2 = site.SubMenu?[para.SubMenuIndex];
                sb += $"→{lv2.MenuItemName}";

                if (lv2.SubMenu.Count > 0 && para.Lv3MenuIndex > -1)
                {
                    var lv3 = lv2.SubMenu?[para.Lv3MenuIndex];
                    sb += $"→{lv3.MenuItemName}";
                    if (lv3.SubMenu.Count > 0 && para.Lv4MenuIndex > -1)
                    {
                        var lv4 = lv3.SubMenu?[para.Lv4MenuIndex];
                        sb += $"→{lv4.MenuItemName}";
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(para.Keyword)) sb += $"→\"{para.Keyword}\"";
            return sb;
        }
    }

    /// <summary>
    /// 搜索的其中一页（虚拟页）
    /// </summary>
    public class SearchedPage
    {
        public MoeItems ImageItems { get; set; }
        public MoeItems PreLoadNextPageItems { get; set; } = new MoeItems();
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
        public bool HasKeyword => !string.IsNullOrWhiteSpace(Keyword);
        public int PageIndex { get; set; }
        public int LastId { get; set; }
        public int Count { get; set; }

        public bool IsShowExplicit { get; set; }
        public bool IsShowExplicitOnly { get; set; }

        public bool IsFilterResolution { get; set; }
        public int MinWidth { get; set; }
        public int MinHeight { get; set; }

        public bool IsFilterFileType { get; set; }
        public string FilterFileTpyeText { get; set; }
        public bool IsFileTypeShowSpecificOnly { get; set; }

        public ImageOrientation Orientation { get; set; } = ImageOrientation.None;
        public DownloadType DownloadType { get; set; }

        public DateTime? Date { get; set; }

        public int SubMenuIndex { get; set; }
        public int Lv3MenuIndex { get; set; }
        public int Lv4MenuIndex { get; set; }

        public SearchPara Clone() => (SearchPara)MemberwiseClone();
    }

    public enum ImageOrientation
    {
        None = 0,
        Landscape = 1,
        Portrait = 2
    }
}
