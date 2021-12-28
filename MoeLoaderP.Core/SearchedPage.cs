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

        public enum ResponseMode { Ok, Fail, OkAndOver }
        public ResponseMode Response { get; set; }

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

}