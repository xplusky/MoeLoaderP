using MoeLoaderP.Core.Sites;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 一次搜索会话
    /// </summary>
    public class SearchSession
    {
        public Settings Settings { get; set; }
        public SearchPara CurrentSearchPara { get; set; }
        public SearchedVisualPages LoadedVisualPages { get; set; } = new SearchedVisualPages();

        public bool IsSearching => SearchingTasksCts.Count > 0;
        public List<CancellationTokenSource> SearchingTasksCts { get; set; } = new List<CancellationTokenSource>();

        public SearchSession(Settings settings, SearchPara para)
        {
            Settings = settings;
            CurrentSearchPara = para;
        }

        //public int AllImageCount => LoadedVisualPages.Sum(page => page.ImageItems.Count);

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
                Ex.ShowMessage("搜索已取消");
            }
            catch (Exception ex)
            {
                Ex.ShowMessage(ex.Message, ex.ToString(), Ex.MessagePos.Window);
                Ex.Log(ex.Message, ex.StackTrace);
            }

            SearchingTasksCts.Remove(cts);
            return t;
        }


        /// <summary>
        /// 搜索下一页
        /// </summary>
        public async Task SearchNextPageAsync(CancellationToken token)
        {
            var newVPage = new SearchedVisualPage(); // 建立虚拟页信息
            // 虚拟页：本地设定的一页数量的图片页容器  真实页：在线网站上Get到的真实数量的图片页容器
            var images = new MoeItems();
            SearchPara tempPara;
            if (LoadedVisualPages.Count == 0)
            {
                tempPara = CurrentSearchPara.Clone(); // 浅复制一份参数
                newVPage.LastRealPageIndex = tempPara.PageIndex;
                // 搜索起始页的所有图片（若网站查询参数有支持的条件过滤，则在搜索时就已自动在线过滤相关条件）
                var sb = new StringBuilder();
                sb.AppendLine($"正在搜索站点 {tempPara.Site.DisplayName} 第 {tempPara.PageIndex} 页图片");
                sb.Append($"(参数：kw：{(!tempPara.Keyword.IsEmpty() ? tempPara.Keyword : "N/A")},num:{tempPara.Count})");
                Ex.ShowMessage(sb.ToString(), null, Ex.MessagePos.Searching);
                var imagesOrg = await tempPara.Site.GetRealPageImagesAsync(tempPara, token);
                CurrentSearchPara.NextPageMark = tempPara.NextPageMark;
                if (imagesOrg == null || imagesOrg.Count == 0)
                {
                    Ex.ShowMessage("无搜索结果", null, Ex.MessagePos.Searching);
                    return;
                }
                for (var i = 0; i < imagesOrg.Count; i++)
                {
                    var item = imagesOrg[i];
                    if (i < tempPara.Count) images.Add(item);
                    else
                    {
                        newVPage.PreLoadNextPageItems.Add(item);
                        if (!newVPage.HasNextVisualPage) newVPage.HasNextVisualPage = true;
                    }
                }
            }
            else if (!LoadedVisualPages.Last().HasNextVisualPage) // 若无下一页则返回
            {
                return;
            }
            else
            {
                tempPara = CurrentSearchPara.Clone(); // 浅复制一份参数
                tempPara.PageIndex = LoadedVisualPages.Last().LastRealPageIndex;

                // 若不是第一页则使用上一页搜索多出来的图片作为本页基数
                images = new MoeItems();
                for (var i = 0; i < LoadedVisualPages.Last().PreLoadNextPageItems.Count; i++)
                {
                    var item = LoadedVisualPages.Last().PreLoadNextPageItems[i];
                    if (i < tempPara.Count) images.Add(item);
                    else
                    {
                        newVPage.PreLoadNextPageItems.Add(item);
                        newVPage.HasNextVisualPage = true;
                    }
                }
            }

            Filter(images); // 本地条件过滤，images数量有可能减少

            // 进入 loop 循环
            var startTime = DateTime.Now;
            while (images.Count < tempPara.Count) // 当images数量不够搜索参数数量时循环
            {
                token.ThrowIfCancellationRequested(); // 整体Task的取消Token，取消时会抛出异常
                tempPara.PageIndex++; // 设置新搜索参数为下一页（真）
                //tempPara.LastImageId = images.LastOrDefault()?.Id ?? 0; // 设置新搜索参数为最后ID（真）
                newVPage.LastRealPageIndex = tempPara.PageIndex;
                var sb = new StringBuilder();
                sb.AppendLine($"正在搜索站点 {tempPara.Site.DisplayName} 第 {tempPara.PageIndex} 页图片");
                sb.AppendLine($"已获取第{tempPara.PageIndex - 1}页{images.Count}张图片，还需{tempPara.Count - images.Count}张");
                sb.Append($"(参数：kw：{(!tempPara.Keyword.IsEmpty() ? tempPara.Keyword : "N/A")},num:{tempPara.Count})");
                Ex.ShowMessage(sb.ToString(), null, Ex.MessagePos.Searching);
                var nextRealPageImageItems = await tempPara.Site.GetRealPageImagesAsync(tempPara, token); // 搜索下一页（真）的所有图片
                CurrentSearchPara.NextPageMark = tempPara.NextPageMark;
                if (nextRealPageImageItems == null || nextRealPageImageItems.Count == 0) // 当下一页（真）的搜索到的未进行本地过滤图片数量为0时，表示已经搜索完了
                {
                    newVPage.HasNextVisualPage = false; // 没有下一页
                    newVPage.LastRealPageIndex = tempPara.PageIndex;
                    break;
                }
                else // 当下一页（真）未过滤图片数量不为0时
                {

                    Filter(nextRealPageImageItems); // 本地过滤下一页（真）

                    foreach (var item in nextRealPageImageItems)
                    {
                        if (images.Count < tempPara.Count) images.Add(item); // 添加图片数量直到够参数设定的图片数量为止
                        else newVPage.PreLoadNextPageItems.Add(item); // 多出来的图片存在另一个对象中，下一虚拟页可以调用
                    }
                    if (images.Count >= tempPara.Count) break; // 数量已够参数数量，当前虚拟页完成任务
                    //if (nextRealPageImageItems.Count < tempPara.Count && !tempPara.NextPagePara.IsEmpty())
                    //{
                    //    break;
                    //}
                }
                if (DateTime.Now - startTime > TimeSpan.FromSeconds(30)) break; // loop超时跳出循环（即使不够数量也跳出）
            }
            token.ThrowIfCancellationRequested();
            // Load end
            newVPage.ImageItems = images;
            LoadedVisualPages.Add(newVPage);
            if (images.Message != null) Ex.ShowMessage(images.Message);
            if (images.Exs?.Count > 0)
            {
                if(images.Exs.Count==1) Ex.ShowMessage(images.Exs[0].Message, images.Exs[0].ToString(), Ex.MessagePos.Window);
                if (images.Exs.Count > 1)
                {
                    var sb = new StringBuilder();
                    foreach (var ex in images.Exs)
                    {
                        sb.AppendLine(ex.ToString());
                    }
                    Ex.ShowMessage("发生多个错误", sb.ToString(), Ex.MessagePos.Window);
                    
                }
                
            }
            Ex.ShowMessage("搜索完毕", null, Ex.MessagePos.Searching);
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
                var state = item.Site.Config;
                if (state.IsSupportRating) // 过滤r18评级图片
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
                    foreach (var s in para.FilterFileTypeText.Split(';'))
                    {
                        if (s.IsEmpty()) continue;
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
            if (site.Lv2Cat?.Count > 0 && para.Lv2MenuIndex > -1)
            {
                var lv2 = site.Lv2Cat?[para.Lv2MenuIndex];
                sb += $"→{lv2.Name}";

                if (lv2.SubCategories?.Count > 0 && para.Lv3MenuIndex > -1)
                {
                    var lv3 = lv2.SubCategories?[para.Lv3MenuIndex];
                    sb += $"→{lv3.Name}";
                    if (lv3.SubCategories?.Count > 0 && para.Lv4MenuIndex > -1)
                    {
                        var lv4 = lv3.SubCategories?[para.Lv4MenuIndex];
                        sb += $"→{lv4.Name}";
                    }
                }
            }

            if (!para.Keyword.IsEmpty()) sb += $"→\"{para.Keyword}\"";
            return sb;
        }
    }

    /// <summary>
    /// 搜索的其中一页（虚拟页）
    /// </summary>
    public class SearchedVisualPage
    {
        public MoeItems ImageItems { get; set; }
        public MoeItems PreLoadNextPageItems { get; set; } = new MoeItems();
        public int LastRealPageIndex { get; set; }
        public bool HasNextVisualPage { get; set; } = true;
    }

    public class SearchedVisualPages : ObservableCollection<SearchedVisualPage> { }

    /// <summary>
    /// 搜索参数
    /// </summary>
    public class SearchPara
    {
        public MoeSite Site { get; set; }
        //public SearchSession CurrentSearch { get; set; }
        public string Keyword { get; set; }
        public int PageIndex { get; set; }
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

        public MoeSiteConfig SupportState { get; set; }

        public SearchPara Clone() => (SearchPara)MemberwiseClone();
    }

    public enum ImageOrientation
    {
        None = 0,
        Landscape = 1,
        Portrait = 2
    }
}
