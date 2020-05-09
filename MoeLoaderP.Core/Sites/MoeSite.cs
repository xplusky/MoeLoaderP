using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// 图片站点基类，需要开发新站点的请继承此类
    /// </summary>
    public abstract class MoeSite : BindingObject
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
        /// </summary>
        public abstract string ShortName { get; }

        public string LoginPageUrl { get; set; }
        public MoeMenuItems SubMenu { get; set; } = new MoeMenuItems();

        public virtual CookieContainer GetCookies() => null;
        public virtual bool VerifyCookie(string cookieStr) => false;
        /// <summary>
        /// 站点支持的功能情况
        /// </summary>
        public MoeSiteSupportState SupportState { get; set; } = new MoeSiteSupportState();
        public MoeSiteFuncSupportState FuncSupportState { get; set; } = new MoeSiteFuncSupportState();


        public MenuItemFunc MenuFunc { get; set; } = new MenuItemFunc();

        public NetDocker Net { get; set; }

        /// <summary>
        /// 异步获取图片列表，开发者需实现该功能
        /// </summary>
        public abstract Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token);

        /// <summary>
        /// 获取关键词自动提示列表
        /// </summary>
        public virtual Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token) => null;

        public Settings Settings { get; set; }

        public MoeSiteSetting CurrentSiteSetting
        {
            get
            {
                if (Settings?.MoeSiteSettings?.ContainsKey(ShortName) == true) return Settings.MoeSiteSettings[ShortName];
                if (Settings == null) return null;
                if(Settings.MoeSiteSettings == null) Settings.MoeSiteSettings = new Dictionary<string, MoeSiteSetting>();
                Settings.MoeSiteSettings.Add(ShortName, new MoeSiteSetting());
                return Settings.MoeSiteSettings[ShortName];
            }
        }

        public DownloadTypes DownloadTypes { get; set; } = new DownloadTypes();

    }


    public class DownloadType
    {
        public string Name { get; set; }
        public int Priority { get; set; }
    }

    public class DownloadTypes : ObservableCollection<DownloadType>
    {
        public void Add(string name, int pr)
        {
            Add(new DownloadType
            {
                Name = name,
                Priority = pr
            });
        }
    }


    public class MoeSiteSupportState
    {
        /// <summary>
        /// 是否支持用户登录，支持的话会显示登录按钮
        /// </summary>
        public bool IsSupportAccount { get; set; } = false;

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

        public bool IsSupportSearchByImageLastId { get; set; } = false;
    }

    public class MoeSiteFuncSupportState
    {
        public bool IsSupportSelectPixivRankNew { get; set; } = false;

        public bool IsSupportSearchByAuthorId { get; set; } = false;
    }

    public class MoeSiteOnlineUserFunc
    {
        public delegate CookieContainer GetCookieDelegate(string cookieStr);
        public GetCookieDelegate GetCookieFunc { get; set; }

        public delegate bool VerifyCookie(string cookieStr);
        
    }


    public class MoeSites : ObservableCollection<MoeSite>
    {
        public Settings Settings { get; set; }
        public new void Add(MoeSite site)
        {
            site.Settings = Settings;
            base.Add(site);
        }

        public MoeSites(Settings set)
        {
            Settings = set;
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
        public void AddHistory(string keyword, Settings settings)
        {
            if (keyword.IsEmpty()) return;
            foreach (var item in this)
            {
                if (item.Word == keyword) return;
            }
            var hintItem = new AutoHintItem
            {
                IsHistory = true,
                Word = keyword
            };
            if (Count >= settings.HistoryKeywordsMaxCount) RemoveAt(Count - 1);
            Insert(0, hintItem);
        }

        public void Add(string word, string count = null)
        {
            var item = new AutoHintItem {Word = word, Count = count};
            Add(item);
        }
    }

    public class MoeMenuItem
    {
        public MoeMenuItem() { }
        public MoeMenuItem(string name, MoeMenuItems menu = null)
        {
            MenuItemName = name;
            if (menu != null) SubMenu = menu;
        }
        public string MenuItemName { get; set; }

        public MenuItemFunc Func { get; set; } = new MenuItemFunc();

        public MoeMenuItems SubMenu { get; set; } = new MoeMenuItems();

    }

    public class MenuItemFunc
    {
        public bool? ShowKeyword { get; set; } = true;
        public bool? ShowDatePicker { get; set; } = false;
    }

    public class MoeMenuItems : List<MoeMenuItem>
    {
        public void Add(string name, MoeMenuItems subMenu)
        {
            Add(new MoeMenuItem
            {
                MenuItemName = name,
                SubMenu = subMenu
            });
        }

        public void Add(string name)
        {
            Add(new MoeMenuItem
            {
                MenuItemName = name
            });
        }

        public MoeMenuItems(params MoeMenuItem[] items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public MoeMenuItems(MoeMenuItems submenu, params string[] itemsNames)
        {
            foreach (var name in itemsNames)
            {
                Add(new MoeMenuItem(name, submenu));
            }
        }

        public MoeMenuItems() { }

    }
}
