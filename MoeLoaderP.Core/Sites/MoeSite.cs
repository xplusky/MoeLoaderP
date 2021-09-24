using System;
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

        public virtual Uri Icon => new Uri($"/Assets/SiteIcon/{ShortName}.ico", UriKind.Relative);

        public Categories Lv2Cat { get; set; }

        /// <summary>
        /// 站点功能配置
        /// </summary>
        public MoeSiteConfig Config { get; set; } = new MoeSiteConfig();

        /// <summary>
        /// 浏览和下载所用接口
        /// </summary>
        public NetOperator Net { get; set; }

        /// <summary>
        /// 包含cookie的网络接口
        /// </summary>
        //public NetOperator AccountNet { get; set; }

        /// <summary>
        /// 异步获取图片列表，开发者需实现该功能
        /// </summary>
        public abstract Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token);

        /// <summary>
        /// 获取关键词自动提示列表
        /// </summary>
        public virtual Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token) => null;

        public Settings Settings { get; set; }

        public IndividualSiteSettings SiteSettings => Settings.AllSitesSettings.GetSettings(this);

        public DownloadTypes DownloadTypes { get; set; } = new DownloadTypes();

        public MirrorSiteConfigs Mirrors { get; set; }

        #region 账户及在线功能相关

        public virtual bool VerifyCookieAndSave(CookieCollection ccol) => false;
        public virtual string[] GetCookieUrls()
        {
            return new[] { HomeUrl };
        }
        public string LoginPageUrl { get; set; }
        /// <summary>
        /// 点赞
        /// </summary>
        /// <param name="item"></param>
        /// <param name="token"></param>
        /// <returns>是否成功</returns>
        public virtual Task<bool> ThumbAsync(MoeItem item, CancellationToken token) => null;

        /// <summary>
        /// 标心或者喜欢
        /// </summary>
        /// <param name="token"></param>
        /// <returns>是否成功</returns>
        public virtual Task<bool> StarAsync(CancellationToken token) => null;

        #endregion
    }

    public class MirrorSiteConfig
    {
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public string HomeUrl { get; set; }
        public string SearchApi { get; set; }
    }

    public class MirrorSiteConfigs : ObservableCollection<MirrorSiteConfig>
    {
        public MirrorSiteConfigs()
        {
            var msc = new MirrorSiteConfig()
            {
                Name = "(不使用镜像）",
                IsDefault = true
            };
            Add(msc);
        }
    }

    public class MoeSiteConfig
    {
        /// <summary>
        /// 是否支持评分，若为false则不可按分数过滤图片
        /// </summary>
        public bool IsSupportScore { get; set; }

        /// <summary>
        /// 是否支持图片年龄分级
        /// </summary>
        public bool IsSupportRating { get; set; }

        /// <summary>
        /// 是否支持分辨率，若为false则不可按分辨率过滤图片
        /// </summary>
        public bool IsSupportResolution { get; set; }
        
        /// <summary>
        /// 是否支持关键字搜索. 不支持则不显示搜索框
        /// </summary>
        public bool IsSupportKeyword { get; set; } 

        /// <summary>
        /// 是否支持按图片id搜索,支持的话搜索参数里面可以设置
        /// </summary>
        public bool IsSupportSearchByImageLastId { get; set; }

        /// <summary>
        /// 是否支持用户登录，支持的话会显示登录按钮
        /// </summary>
        public bool IsSupportAccount { get; set; }
        public bool IsSupportThumbButton { get; set; }
        public bool IsSupportStarButton { get; set; }
        public bool IsSupportMultiKeywords { get; set; } 
        public bool IsSupportDatePicker { get; set; } 
        public bool IsR18Site { get; set; } = false;
        public bool IsCustomSite { get; set; }
        public ImageOrders ImageOrders { get; set; }
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
            var item = new AutoHintItem { Word = word, Count = count };
            Add(item);
        }
    }

    public class Category
    {
        public string Name { get; set; }
        public Categories SubCategories { get; set; } 
        public MoeSiteConfig OverrideConfig { get; set; }

        public Category() { }
        public Category(string name, Categories cats = null)
        {
            Name = name;
            if (cats != null) SubCategories = cats;
        }
    }

    public class Categories : List<Category>
    {
        public void Add(string name, Categories subMenu)
        {
            Add(new Category
            {
                Name = name,
                SubCategories = subMenu
            });
        }
        
        public Categories(params Category[] items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public Categories(params string[] itemsNames)
        {
            foreach (var item in itemsNames)
            {
                Add(new Category(item));
            }
        }

        public Categories(Categories submenu, params string[] itemsNames)
        {
            foreach (var name in itemsNames)
            {
                Add(new Category(name, submenu));
            }
        }

        public Categories(){}
    }
    
}
