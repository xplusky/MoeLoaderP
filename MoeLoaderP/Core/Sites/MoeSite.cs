using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// 图片站点基类
    /// </summary>
    public abstract class MoeSite : NotifyBase
    {
        /// <summary>
        /// 站点URL，用于打开该站点主页。eg. http://yande.re
        /// </summary>
        public abstract string HomeUrl { get; }

        /// <summary>
        /// 站点名称，用于站点列表中的显示。
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// 站点的短名称，将作为站点的唯一标识，eg. yande
        /// 提示：可以在程序集中加入以短名称作为文件名的ico图标（eg. yande.ico），该图标会自动作为该站点的图标显示在站点列表中。
        /// </summary>
        public abstract string ShortName { get; }
        
        /// <summary>
        /// 站点支持情况
        /// </summary>
        public MoeSiteSurpportState SurpportState { get; set; } = new MoeSiteSurpportState();
        
        /// <summary>
        /// 向该站点发起请求时需要伪造的Referer，若不需要则保持null
        /// </summary>
        public virtual string Referer => null;

        public MoeSiteSubMenu SubMenu { get; set; } = new MoeSiteSubMenu();

        public int SubListIndex { get; set; }

        public int Lv3ListIndex { get; set; }


        /// <summary>
        /// 获取页面的源代码，例如HTML（已过时）
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="count">单页数量（可能不支持）</param>
        /// <param name="keyWord">关键词</param>
        /// <param name="proxy">全局的代理设置，进行网络操作时请使用该代理</param>
        /// <returns>页面源代码</returns>
        public virtual string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            return null;
        }

        /// <summary>
        /// 从页面源代码获取图片列表（已过时）
        /// </summary>
        /// <param name="pageString">页面源代码</param>
        /// <param name="proxy">全局的代理设置，进行网络操作时请使用该代理</param>
        /// <returns>图片信息列表</returns>
        public virtual List<ImageItem> GetImages(string pageString, IWebProxy proxy)
        {
            return null;
        }

        /// <summary>
        /// 异步获取图片列表
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public virtual async Task<ImageItems> GetRealPageImagesAsync(SearchPara para)
        {
            // page source
            var pre = new PreLoader(Settings.Proxy);
            var pageString = await Task.Run(() => pre.GetPreFetchedPage(para.PageIndex, para.Count, Uri.EscapeDataString(para.Keyword), this));
            List<ImageItem> list;
            if (pageString == null)
            {
                list = await Task.Run(() => GetImages(para.PageIndex, para.Count, para.Keyword, Settings.Proxy));
            }
            else
            {
                list = await Task.Run(() => GetImages(pageString, Settings.Proxy));
            }
            var items = new ImageItems();
            foreach (var item in list)
            {
                item.Site = this;
                items.Add(item);
            }
            return items;
        }

        /// <summary>
        /// 获取关键词自动提示列表（已过时）
        /// </summary>
        /// <param name="word">关键词</param>
        /// <param name="proxy">全局的代理设置，进行网络操作时请使用该代理</param>
        /// <returns>提示列表项集合</returns>
        public virtual List<AutoHintItem> GetAutoHintItems(string word, IWebProxy proxy)
        {
            return new List<AutoHintItem>();
        }

        /// <summary>
        /// 获取关键词自动提示列表
        /// </summary>
        /// <param name="para"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para,CancellationToken token)
        {
            var tagss = await Task.Run(() =>
            {
                var tags = GetAutoHintItems(para.Keyword, para.Site.Settings.Proxy);
                return tags;
            }, token);
            token.ThrowIfCancellationRequested();
            var tagsc = new AutoHintItems();
            foreach (var tag in tagss)
            {
                tagsc.Add(tag);
            }
            return tagsc;
        }
        

        public BitmapImage Icon => new BitmapImage(new Uri($"/Assets/SiteIcon/{ShortName}.ico", UriKind.Relative));

        public virtual List<ImageItem> GetImages(int page, int count, string keyWord, IWebProxy proxy)
        {
            return GetImages(GetPageString(page, count, keyWord, proxy), proxy);
        }


        public Settings Settings { get; set; }
        public Visibility KeywordVisible => SurpportState.IsSupportKeyword ? Visibility.Visible : Visibility.Collapsed;
    }

    
    public class MoeSiteSurpportState
    {
        /// <summary>
        /// 是否支持评分，若为false则不可按分数过滤图片
        /// </summary>
        public bool IsSupportScore { get; set; } = true;

        /// <summary>
        /// 是否支持分级
        /// </summary>
        public bool IsSupportRating { get; set; } = true;

        /// <summary>
        /// 是否支持分辨率，若为false则不可按分辨率过滤图片
        /// </summary>
        public bool IsSupportResolution { get; set; } = true;
        
        /// <summary>
        /// 是否支持预览图，若为false则缩略图上无查看预览图的按钮
        /// </summary>
        public bool IsSupportPreview { get; set; } = true;

        /// <summary>
        /// 是否支持搜索框自动提示，若为false则输入关键词时无自动提示
        /// </summary>
        public bool IsSupportAutoHint { get; set; } = true;

        /// <summary>
        /// 是否支持关键字搜索
        /// </summary>
        public bool IsSupportKeyword { get; set; } = true;

        
    }

    public class MoeSites : ObservableCollection<MoeSite>
    {
        public Settings Settings { get; set; }
        public new void Add(MoeSite site)
        {
            site.Settings = Settings;
            base.Add(site);
        }
    }

    /// <summary>
    /// 自动提示列表中的一项
    /// </summary>
    public class AutoHintItem
    {
        public string Word { get; set; }
        public string Count { get; set; }
        public bool IsEnable { get; set; } = true;
        public bool IsHistory { get; set; }
    }

    public class AutoHintItems : ObservableCollection<AutoHintItem>
    {
        public void AddHistory(string keyword,Settings settings)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return;
            foreach (var item in this)
            {
                if (item.Word == keyword) return;
            }
            var aitem = new AutoHintItem
            {
                IsHistory = true,
                Word = keyword
            };
            if (Count>=settings.HistoryKeywordsMaxCount)RemoveAt(Count-1);
            Add(aitem);
        }
    }

    public class MoeSiteSubMenuItem
    {
        public MoeSiteSubMenuItem() { }
        public MoeSiteSubMenuItem(string name, MoeSiteSubMenu menu = null)
        {
            Name = name;
            if (menu != null) SubMenu = menu;
        }
        public string Name { get; set; }

        public MoeSiteSubMenu SubMenu { get; set; } = new MoeSiteSubMenu();

    }

    public class MoeSiteSubMenu : ObservableCollection<MoeSiteSubMenuItem>
    {
        public void Add(string name, MoeSiteSubMenu subMenu)
        {
            Add(new MoeSiteSubMenuItem
            {
                Name = name,
                SubMenu = subMenu
            });
        }

        public void Add(string name)
        {
            Add(new MoeSiteSubMenuItem
            {
                Name = name
            });
        }

        public MoeSiteSubMenu(params MoeSiteSubMenuItem[] items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
    }
}
