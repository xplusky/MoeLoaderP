using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using MoeLoaderP.Core.Sites;
using Newtonsoft.Json;

namespace MoeLoaderP.Core;

/// <summary>
///     用于存储设置、绑定及运行时参数传递（整个软件）。可保存为json文件
/// </summary>
public class Settings : BindingObject
{
    // runtime
    private SearchSession _currentSession;

    [JsonIgnore]
    public SearchSession CurrentSession
    {
        get => _currentSession;
        set => SetField(ref _currentSession, value, nameof(CurrentSession));
    }

    [JsonIgnore] public SiteManager SiteManager { get; set; }

    [JsonIgnore] public string CustomSitesDir { get; set; }

    #region Window size / Display

    private bool _isLowPerformanceMode;

    public bool IsLowPerformanceMode
    {
        get => _isLowPerformanceMode;
        set => SetField(ref _isLowPerformanceMode, value, nameof(IsLowPerformanceMode));
    }

    private double _mainWindowWidth = 1060d;

    public double MainWindowWidth
    {
        get => _mainWindowWidth;
        set => SetField(ref _mainWindowWidth, value, nameof(MainWindowWidth));
    }

    private double _mainWindowHeight = 760d;

    public double MainWindowHeight
    {
        get => _mainWindowHeight;
        set => SetField(ref _mainWindowHeight, value, nameof(MainWindowHeight));
    }

    private bool _isShowBgImage = true;

    public bool IsShowBgImage
    {
        get => _isShowBgImage;
        set => SetField(ref _isShowBgImage, value, nameof(IsShowBgImage));
    }

    private bool _isEnableAcrylicStyle = true;

    public bool IsEnableAcrylicStyle
    {
        get => _isEnableAcrylicStyle;
        set => SetField(ref _isEnableAcrylicStyle, value, nameof(IsEnableAcrylicStyle));
    }

    private bool _isShowEggWindowOnce;

    public bool IsShowEggWindowOnce
    {
        get => _isShowEggWindowOnce;
        set => SetField(ref _isShowEggWindowOnce, value, nameof(IsShowEggWindowOnce));
    }

    #endregion

    #region Searching Settings

    private int _preLoadPagesCount;

    public int PreLoadPagesCount
    {
        get => _preLoadPagesCount;
        set => SetField(ref _preLoadPagesCount, value, nameof(PreLoadPagesCount));
    }

    private int _maxOnLoadingImageCount = 8;

    public int MaxOnLoadingImageCount
    {
        get => _maxOnLoadingImageCount;
        set => SetField(ref _maxOnLoadingImageCount, value, nameof(MaxOnLoadingImageCount));
    }

    private int _maxOnDownloadingImageCount = 3;

    public int MaxOnDownloadingImageCount
    {
        get => _maxOnDownloadingImageCount;
        set => SetField(ref _maxOnDownloadingImageCount, value, nameof(MaxOnDownloadingImageCount));
    }

    private double _imageItemControlSize = 192d;

    public double ImageItemControlSize
    {
        get => _imageItemControlSize;
        set => SetField(ref _imageItemControlSize, value, nameof(ImageItemControlSize));
    }

    private int _historyKeywordsMaxCount = 25;

    public int HistoryKeywordsMaxCount
    {
        get => _historyKeywordsMaxCount;
        set => SetField(ref _historyKeywordsMaxCount, value, nameof(HistoryKeywordsMaxCount));
    }

    private bool _isClearImagesWhenSearchNextPage = true;

    public bool IsClearImagesWhenSearchNextPage
    {
        get => _isClearImagesWhenSearchNextPage;
        set => SetField(ref _isClearImagesWhenSearchNextPage, value, nameof(IsClearImagesWhenSearchNextPage));
    }

    #endregion

    #region Download Settings

    private int _downloadFirstSeveralCount = 1;

    public int DownloadFirstSeveralCount
    {
        get => _downloadFirstSeveralCount;
        set => SetField(ref _downloadFirstSeveralCount, value, nameof(DownloadFirstSeveralCount));
    }

    private bool _isDownloadFirstSeveral;

    public bool IsDownloadFirstSeveral
    {
        get => _isDownloadFirstSeveral;
        set => SetField(ref _isDownloadFirstSeveral, value, nameof(IsDownloadFirstSeveral));
    }

    private string _imageSavePath /* = App.MoePicFolder*/;

    public string ImageSavePath
    {
        get => _imageSavePath;
        set => SetField(ref _imageSavePath, value, nameof(ImageSavePath));
    }

    public const string SaveFileNameFormatDefaultValue = "%site %id %title";
    private string _saveFileNameFormat = SaveFileNameFormatDefaultValue;

    public string SaveFileNameFormat
    {
        get => _saveFileNameFormat;
        set => SetField(ref _saveFileNameFormat, value, nameof(SaveFileNameFormat));
    }

    public const string SortFolderNameFormatDefaultValue = "%site\\%title";
    private string _sortFolderNameFormat = SortFolderNameFormatDefaultValue;

    public string SortFolderNameFormat
    {
        get => _sortFolderNameFormat;
        set => SetField(ref _sortFolderNameFormat, value, nameof(SortFolderNameFormat));
    }

    private bool _isAutoRenameWhenSame;

    public bool IsAutoRenameWhenSame
    {
        get => _isAutoRenameWhenSame;
        set => SetField(ref _isAutoRenameWhenSame, value, nameof(IsAutoRenameWhenSame));
    }

    private int _nameFormatTagCount;

    public int NameFormatTagCount
    {
        get => _nameFormatTagCount;
        set => SetField(ref _nameFormatTagCount, value, nameof(NameFormatTagCount));
    }

    #endregion

    #region Proxy Settings

    public enum ProxyModeEnum
    {
        None = 0,
        Custom = 1,
        Ie = 2,
        Default = 3,
    }

    private ProxyModeEnum _proxyMode = ProxyModeEnum.None;

    public ProxyModeEnum ProxyMode
    {
        get => _proxyMode;
        set => SetField(ref _proxyMode, value, nameof(ProxyMode));
    }

    private string _proxySetting = "127.0.0.1:1080";

    public string ProxySetting
    {
        get => _proxySetting;
        set => SetField(ref _proxySetting, value, nameof(ProxySetting));
    }

    #endregion

    #region R18 Mode Setting

    private bool _isXMode;

    public bool IsXMode
    {
        get => _isXMode;
        set => SetField(ref _isXMode, value, nameof(IsXMode));
    }

    private bool _haveEnteredXMode;

    public bool HaveEnteredXMode
    {
        get => _haveEnteredXMode;
        set => SetField(ref _haveEnteredXMode, value, nameof(HaveEnteredXMode));
    }

    private bool _isDisplayExplicitImages = true;


    public bool IsDisplayExplicitImages
    {
        get => _isDisplayExplicitImages;
        set => SetField(ref _isDisplayExplicitImages, value, nameof(IsDisplayExplicitImages));
    }

    #endregion

    #region MoeSites Settings

    /// <summary>
    ///     每一个站点分别的设置（所有站点集合）
    /// </summary>
    public MoeSitesSettings AllSitesSettings { get; set; } = new();


    private bool _isCustomSiteMode;

    [JsonIgnore]
    public bool IsCustomSiteMode
    {
        get => _isCustomSiteMode;
        set => SetField(ref _isCustomSiteMode, value, nameof(IsCustomSiteMode));
    }

    #endregion

    #region Save&Load Func

    public void Save(string jsonPath)
    {
        // 保存 json
        var json = JsonConvert.SerializeObject(this);
        File.WriteAllText(jsonPath, json);
    }

    public static Settings Load(string jsonPath)
    {
        Settings settings;
        try
        {
            if (File.Exists(jsonPath))
            {
                var json = File.ReadAllText(jsonPath);
                settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            else
            {
                settings = new Settings();
            }
        }
        catch (Exception ex)
        {
            Ex.ShowMessage("设置读取失败，将读取默认设置", null, Ex.MessagePos.Window);
            Ex.Log(ex);
            settings = new Settings();
        }

        return settings;
    }

    #endregion
}

#region Settings Helper Class

public class MoeSitesSettings : Dictionary<string, IndividualSiteSettings>
{
    public IndividualSiteSettings GetSettings(MoeSite site)
    {
        if (!ContainsKey(site.ShortName)) Add(site.ShortName, new IndividualSiteSettings());
        var set = this[site.ShortName];
        return set;
    }
}

public class IndividualSiteSettings : BindingObject
{
    private Dictionary<string, string> _items = new();

    private CookieCollection _loginCookies;

    public Dictionary<string, string> Items
    {
        get => _items;
        set => SetField(ref _items, value, nameof(Items));
    }

    public CookieCollection LoginCookies
    {
        get => _loginCookies;
        set => SetField(ref _loginCookies, value, nameof(LoginCookies));
    }

    public DateTime? LoginExpiresTime { get; set; }

    public AutoHintItems History { get; set; } = new();

    public Settings.ProxyModeEnum SiteProxy { get; set; } = Settings.ProxyModeEnum.Default;

    public string GetSetting(string key)
    {
        return Items.ContainsKey(key) ? Items[key] : null;
    }

    public void SetSetting(string key, string value)
    {
        if (Items.ContainsKey(key))
            Items[key] = value;
        else
            Items.TryAdd(key, value);

        Ex.Log($"Add Key:{key} Value:{value} ");
    }

    public CookieContainer GetCookieContainer()
    {
        if (!(LoginCookies?.Count > 0)) return null;
        var cc = new CookieContainer();
        foreach (Cookie cookie in LoginCookies) cc.Add(cookie);
        return cc;
    }
}


/// <summary>
///     实现绑定所需的属性值变更通知接口
/// </summary>
public class BindingObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected bool SetField<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

#endregion