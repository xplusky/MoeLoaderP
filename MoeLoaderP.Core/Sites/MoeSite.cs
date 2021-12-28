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
        private bool _isUserLogin;

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

        public virtual Uri Icon => new($"/Assets/SiteIcon/{ShortName}.ico", UriKind.Relative);

        public Categories Lv2Cat { get; set; }

        /// <summary>
        /// 功能配置
        /// </summary>
        public MoeSiteConfig Config { get; set; } = new();

        /// <summary>
        /// 浏览和下载所用接口
        /// </summary>
        public NetOperator Net { get; set; }

        public virtual NetOperator GetCloneNet(string referer = null,double timeout = 40)
        {
            Net ??= new NetOperator(Settings);
            var net = Net.CreateNewWithOldCookie();
            net.SetTimeOut(timeout);
            net.SetReferer(referer);
            return net;
        }

        /// <summary>
        /// 异步获取图片列表，开发者需实现该功能
        /// </summary>
        public abstract Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token);
        

        /// <summary>
        /// 获取关键词自动提示列表
        /// </summary>
        public virtual Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token) => null;

        public Settings Settings { get; set; }

        public IndividualSiteSettings SiteSettings => Settings.AllSitesSettings.GetSettings(this);

        public DownloadTypes DownloadTypes { get; set; } = new();

        public MirrorSiteConfigs Mirrors { get; set; }

        #region 账户及在线功能相关

        public bool IsUserLogin
        {
            get => _isUserLogin;
            set => SetField(ref _isUserLogin, value, nameof(IsUserLogin));
        }
        public virtual bool VerifyCookieAndSave(CookieCollection ccol) => false;
        public virtual string[] GetCookieUrls()
        {
            return new[] { HomeUrl };
        }
        public string LoginPageUrl { get; set; }

        /// <summary>
        /// 点赞
        /// </summary>
        public virtual Task<bool> ThumbAsync(MoeItem item, CancellationToken token) => null;

        /// <summary>
        /// 标心或者喜欢
        /// </summary>
        public virtual Task<bool> StarAsync(MoeItem item, CancellationToken token) => null;

        public virtual void Logout()
        {
            SiteSettings.LoginCookies = null;
            Ex.ShowMessage("已清除登录信息！");
        }

        #endregion
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
}
