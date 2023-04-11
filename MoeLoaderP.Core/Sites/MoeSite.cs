using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     图片站点基类，需要开发新站点的请继承此类
/// </summary>
public abstract class MoeSite : BindingObject
{
    private bool _isUserLogin;

    /// <summary>
    ///     站点URL，用于打开该站点主页。eg. http://yande.re
    /// </summary>
    public abstract string HomeUrl { get; }

    /// <summary>
    ///     站点名称，用于站点列表中的显示。
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    ///     站点的短名称，将作为站点的唯一标识，eg. yande
    /// </summary>
    public abstract string ShortName { get; }

    public virtual Uri Icon => new($"/Assets/SiteIcon/{ShortName}.ico", UriKind.Relative);
    

    public Categories Lv2Cat { get; set; } 

    /// <summary>
    ///     功能配置
    /// </summary>
    public MoeSiteConfig Config { get; set; } = new();

    /// <summary>
    ///     浏览和下载所用接口
    /// </summary>
    public NetOperator Net { get; set; }

    public Settings Settings { get; set; }

    public IndividualSiteSettings SiteSettings => Settings.AllSitesSettings.GetSettings(this);

    public DownloadTypes DownloadTypes { get; set; } = new();

    public MirrorSiteConfigs Mirrors { get; set; } = new();

    public virtual void AfterInit() {}
    public virtual NetOperator GetCloneNet(string referer = null, double timeout = 40)
    {
        Net ??= new NetOperator(Settings, this);
        var net = Net.CloneWithCookie();
        net.SetTimeOut(timeout);
        net.SetReferer(referer);
        return net;
    }

    /// <summary>
    ///     异步获取图片列表，开发者需实现该功能
    /// </summary>
    public abstract Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token);


    /// <summary>
    ///     获取关键词自动提示列表
    /// </summary>
    public virtual Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token) => null;

    public bool IsUseProxy
    {
        get
        {
            return SiteSettings.SiteProxy switch
            {
                Settings.ProxyModeEnum.Default => Settings.ProxyMode switch
                {
                    Settings.ProxyModeEnum.None => false,
                    _ => true,
                },
                Settings.ProxyModeEnum.None => false,
                _ => true,
            };
        }
    }

    public event Action<Categories> CatChangeAction;

    public void CatChange()
    {
        CatChangeAction?.Invoke(Lv2Cat);
    }

    #region 账户及在线功能相关

    public bool IsUserLogin
    {
        get => _isUserLogin;
        set => SetField(ref _isUserLogin, value, nameof(IsUserLogin));
    }

    public virtual bool VerifyCookie(CookieCollection ccol)
    {
        return false;
    }

    public virtual string[] GetCookieUrls()
    {
        return new[] {HomeUrl};
    }

    public string LoginPageUrl { get; set; }

    /// <summary>
    ///     点赞
    /// </summary>
    public virtual Task<bool> ThumbAsync(MoeItem item, CancellationToken token)
    {
        return null;
    }

    /// <summary>
    ///     标心或者喜欢
    /// </summary>
    public virtual Task<bool> StarAsync(MoeItem item, CancellationToken token)
    {
        return null;
    }

    public virtual void Logout()
    {
        SiteSettings.LoginCookies = null;
        Ex.ShowMessage("已清除登录信息！");
    }

    #endregion
}

public class MoeSites : ObservableCollection<MoeSite>
{
    public MoeSites(Settings set)
    {
        Settings = set;
    }

    public Settings Settings { get; set; }

    public new void Add(MoeSite site)
    {
        site.Settings = Settings;
        site.Settings.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(Settings.ProxyMode))
            {
                site.OnPropertyChanged(nameof(MoeSite.IsUseProxy));
            } 
        };
        base.Add(site);
        site.AfterInit();
    }
}