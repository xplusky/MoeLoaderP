using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using MoeLoaderP.Core.Sites;
using Newtonsoft.Json;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 用于存储设置、绑定及运行时参数传递（整个软件）可保存为JSON文件
    /// </summary>
    public class Settings : BindingObject
    {
        #region Window size / Display

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

        #endregion

        #region Searching Settings

        private bool _isShowBgImage = true;
        public bool IsShowBgImage
        {
            get => _isShowBgImage;
            set => SetField(ref _isShowBgImage, value, nameof(IsShowBgImage));
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

        private List<string> _searchHistory = new List<string>();
        public List<string> SearchHistory
        {
            get => _searchHistory;
            set => SetField(ref _searchHistory, value, nameof(SearchHistory));
        }

        private int _historyKeywordsMaxCount = 15;
        public int HistoryKeywordsMaxCount
        {
            get => _historyKeywordsMaxCount;
            set => SetField(ref _historyKeywordsMaxCount, value, nameof(HistoryKeywordsMaxCount));
        }
        private AutoHintItems _historyKeywords = new AutoHintItems();
        
        public AutoHintItems HistoryKeywords
        {
            get => _historyKeywords;
            set => SetField(ref _historyKeywords, value, nameof(HistoryKeywords));
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

        private string _imageSavePath/* = App.MoePicFolder*/;
        public string ImageSavePath
        {
            get => _imageSavePath;
            set => SetField(ref _imageSavePath, value, nameof(ImageSavePath));
        }

        private bool _isClearImgsWhenSerachNextPage;
        public bool IsClearImgsWhenSerachNextPage
        {
            get => _isClearImgsWhenSerachNextPage;
            set => SetField(ref _isClearImgsWhenSerachNextPage, value, nameof(IsClearImgsWhenSerachNextPage));
        }

        public const string SaveFileNameFormatDefaultValue = "%site %id";
        private string _saveFileNameFormat= SaveFileNameFormatDefaultValue;
        public string SaveFileNameFormat
        {
            get => _saveFileNameFormat;
            set => SetField(ref _saveFileNameFormat, value, nameof(SaveFileNameFormat));
        }

        public const string SortFolderNameFormatDefaultValue = "%site";
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

        #endregion

        #region Proxy Settings

        public enum ProxyModeEnum
        {
            None = 0,
            Custom = 1,
            Ie = 2
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
        /// 每一个站点分别的设置（所有站点集合）
        /// </summary>
        public AllMoeSitesSettings AllMoeSitesSettings { get; set; } =new AllMoeSitesSettings();

        /// <summary>
        /// 自定义站点设置集合
        /// </summary>
        public AllCustomSitesSettings CustomSiteSettingList { get; set; } = new AllCustomSitesSettings();

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
                Extend.ShowMessage("设置读取失败，将读取默认设置", null, Extend.MessagePos.Window);
                Extend.Log(ex);
                settings = new Settings();
            }
            return settings;
        }

        #endregion
    }

    #region Settings Helper Class

    public class AllMoeSitesSettings : Dictionary<string, SingleMoeSiteSettings> { }
    public class SingleMoeSiteSettings : BindingObject
    {
        private string _loginCookie;
        private Dictionary<string, string> _otherSettings;

        public string LoginCookie
        {
            get => _loginCookie;
            set => SetField(ref _loginCookie, value, nameof(LoginCookie));
        }
        
        public Dictionary<string, string> OtherSettings
        {
            get => _otherSettings;
            set => SetField(ref _otherSettings, value, nameof(OtherSettings));
        }
    }

    public class AllCustomSitesSettings : Dictionary<string, SingleCustomSiteSettings> { }
    public class SingleCustomSiteSettings
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }

    
    /// <summary>
    /// 实现绑定所需的属性值变更通知接口
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
}
