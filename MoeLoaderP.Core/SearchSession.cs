using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MoeLoaderP.Core.Sites;

namespace MoeLoaderP.Core
{
    /// <summary>
    ///     一次搜索会话
    /// </summary>
    public class SearchSession
    {
        public SearchSession(SearchPara para)
        {
            Settings = para.Site.Settings;
            FirstSearchPara = para;
        }

        public Settings Settings { get; set; }
        public SearchPara FirstSearchPara { get; set; }
        public SearchedPages RealPages { get; set; } = new();
        public SearchedVisualPages VisualPages { get; set; } = new();

        public bool IsSearching => SearchingTasksCtsList.Count > 0;
        public ObservableCollection<CancellationTokenSource> SearchingTasksCtsList { get; set; } = new();

        public void SaveKeywords()
        {
            var history = FirstSearchPara.Site.SiteSettings.History;
            var keys = FirstSearchPara.MultiKeywords;
            if (!FirstSearchPara.Keyword.IsEmpty())
            {
                if (history.Count > Settings.HistoryKeywordsMaxCount) history.RemoveAt(Settings.HistoryKeywordsMaxCount - 1);
                history.Insert(0, new AutoHintItem { IsHistory = true, Word = FirstSearchPara.Keyword });
            }
            else if (keys?.Count > 0)
            {
                foreach (var key in keys)
                {
                    if (history.Count > Settings.HistoryKeywordsMaxCount) history.RemoveAt(Settings.HistoryKeywordsMaxCount - 1);
                    history.Insert(0, new AutoHintItem { IsHistory = true, Word = key });
                    
                }
            }
        }


        public async Task<SearchedVisualPage> SearchNextVisualPage()
        {
            if (VisualPages.LastOrDefault()?.IsSearchComplete == true) return null;
            if (VisualPages.Count == 0 || VisualPages.LastOrDefault()?.Count >= FirstSearchPara.CountLimit)
            {
                VisualPages.Add(new SearchedVisualPage());
            }

            var newvp = new SearchedVisualPage();
            var vp = VisualPages[^1];
            vp.LoadStart();
            while (true)
            {
                if (RealPages.LastOrDefault()?.HasNextPage == false) break;
                var para = RealPages.Count == 0 ? FirstSearchPara : RealPages[^1].NextPagePara;
                var rp = await TryGetRealPage(para);
                RealPages.Add(rp);
                var ex = rp.SearchException;
                if (ex != null)
                {
                    Ex.ShowMessage($"搜索中断:{ex.Message}", rp.SearchException.StackTrace);
                    break;
                }

                var filterAddCount = 0;
                var vpcount = 0;
                var newvpcount = 0;
                foreach (var item in rp)
                {
                    bool b;
                    if (vp.Count < para.CountLimit)
                    {
                        b = vp.FilterAdd(item);
                        vpcount++;
                    }
                    else
                    {
                        b = newvp.FilterAdd(item);
                        newvpcount++;
                    }
                    if (b) filterAddCount++;
                }

                var mes = $"获取到图片{rp.Count}张，条件过滤掉{rp.Count - filterAddCount}张，第{vp.PageDisplayIndex}页共获得{vp.Count}/{para.CountLimit}张";
                if (vp.Count == para.CountLimit)
                {
                    mes += $"，预载{newvpcount}张";
                }
                Ex.ShowMessage(mes, pos: Ex.MessagePos.Searching);
                if (rp.HasNextPage == false)
                {
                    if(newvp.Count > 0)
                    {
                        newvp.IsSearchComplete = true;
                    }
                    else
                    {
                        vp.IsSearchComplete = true;
                    }
                }

                if (rp.HasNextPage == false)
                {
                    if (newvp.Count > 0) newvp.IsSearchComplete = true;
                    else vp.IsSearchComplete = true;
                }

                if (newvp.Count > 0)
                {
                    VisualPages.Add(newvp);
                    break;
                }

                if (vp.Count == para.CountLimit) break;
            }

            vp.LoadEnd();
            return vp;
        }

        private static void SearchLog(string message)
        {
            Ex.ShowMessage(message, pos: Ex.MessagePos.Searching);
        }

        public async Task<SearchedPage> TryGetRealPage(SearchPara para)
        {
            var cts = new CancellationTokenSource();
            SearchingTasksCtsList.Add(cts);
            SearchedPage rp;

            try
            {
                SearchLog($"Search:{para.Site.DisplayName} index:{para.PageIndex} next:{para.PageIndexCursor}");
                rp = await para.Site.GetRealPageAsync(para, cts.Token);
                if (rp is null)
                {
                    rp = new SearchedPage();
                    rp.SearchException = new Exception("搜索不到图片");
                    rp.HasNextPage = false;
                }
            }
            catch (Exception e)
            {
                rp = new SearchedPage();
                rp.SearchException = e;
                rp.HasNextPage = false;
            }

            rp.Para = para;
            SearchingTasksCtsList.Remove(cts);
            if(rp.Count == 0)
            {
                rp.HasNextPage = false;
            }

            if (rp.HasNextPage != false) rp.GenNextPagePara();

            return rp;
        }

        public async Task StopSearch()
        {
            foreach (var cts in SearchingTasksCtsList) cts.Cancel();

            while (true)
            {
                if (SearchingTasksCtsList.Count == 0) break;
                await Task.Delay(500);
            }
        }

        public string GetCurrentSearchStateText()
        {
            var para = FirstSearchPara;
            var site = FirstSearchPara.Site;
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
}