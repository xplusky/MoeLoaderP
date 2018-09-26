using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// 图片站点基类，需要开发新站点的请继承此类
    /// </summary>
    public abstract class MoeSite
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
        /// 提示：可以在Assets\SiteIcon中加入加入以短名称作为文件名的ico图标（eg. yande.ico），该图标会作为该站点的图标显示在站点列表中。
        /// </summary>
        public abstract string ShortName { get; }
        
        /// <summary>
        /// 站点支持的功能情况
        /// </summary>
        public MoeSiteSurpportState SurpportState { get; set; } = new MoeSiteSurpportState();
        
        /// <summary>
        /// 附加菜单，若不需要则无需设置
        /// </summary>
        public MoeSiteSubMenu SubMenu { get; set; } = new MoeSiteSubMenu();

        public int SubListIndex { get; set; }

        public int Lv3ListIndex { get; set; }

        public virtual NetSwap Net { get; set; }
        
        /// <summary>
        /// 异步获取图片列表，开发者需实现该功能
        /// </summary>
        public abstract Task<ImageItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token);


        /// <summary>
        /// 获取关键词自动提示列表
        /// </summary>
        public virtual Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            return null;
        }

        public BitmapImage Icon => new BitmapImage(new Uri($"/Assets/SiteIcon/{ShortName}.ico", UriKind.Relative));

        public Settings Settings { get; set; }

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
