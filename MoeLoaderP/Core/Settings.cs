using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows;
using MoeLoader.Core.Sites;
using Newtonsoft.Json;

namespace MoeLoader.Core
{
    /// <summary>
    /// 用于存储设置、绑定及运行时参数传递
    /// </summary>
    public class Settings : NotifyBase
    {
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

        private string _imageSavePath = AppRes.MoePicFolder;
        public string ImageSavePath
        {
            get => _imageSavePath;
            set => SetField(ref _imageSavePath, value, nameof(ImageSavePath));
        }
        
        private bool _isSaveFolderSeparateByTag;
        public bool IsSaveFolderSeparateByTag
        {
            get => _isSaveFolderSeparateByTag;
            set => SetField(ref _isSaveFolderSeparateByTag, value, nameof(IsSaveFolderSeparateByTag));
        }

        private string _saveFileNameFormat= "%origin";

        public string SaveFileNameFormat
        {
            get => _saveFileNameFormat;
            set => SetField(ref _saveFileNameFormat, value, nameof(SaveFileNameFormat));
        }

        public enum ProxyModeEnum { System,Costom,None}

        private ProxyModeEnum _proxyMode = ProxyModeEnum.System;

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
        

        private int _maxOnLoadingImageCount = 10;

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

        private double _imageItemControlSize = 128d;

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

        private bool _isXMode = true;

        public bool IsXMode
        {
            get => _isXMode;
            set => SetField(ref _isXMode, value, nameof(IsXMode));
        }

        private bool _isDisplayExplicitImages;

        public bool IsDisplayExplicitImages
        {
            get => _isDisplayExplicitImages;
            set => SetField(ref _isDisplayExplicitImages, value, nameof(IsDisplayExplicitImages));
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

        [JsonIgnore]
        public IWebProxy Proxy
        {
            get
            {
                switch (ProxyMode)
                {
                    case ProxyModeEnum.None: return new WebProxy();
                    case ProxyModeEnum.Costom: return new WebProxy(new Uri(ProxySetting), false);
                    case ProxyModeEnum.System: return WebRequest.DefaultWebProxy;
                }
                return null;
            }
        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText(AppRes.AppSettingJsonFilePath, json);
        }

        public static Settings Load()
        {
            Settings settings;
            try
            {
                if (File.Exists(AppRes.AppSettingJsonFilePath))
                {
                    var json = File.ReadAllText(AppRes.AppSettingJsonFilePath);
                    settings = JsonConvert.DeserializeObject<Settings>(json);
                }
                else
                {
                    settings = new Settings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置读取失败，将读取默认设置");
                Extend.Log(ex);
                settings = new Settings();
            }
            return settings;
        }
    }

    /// <summary>
    /// 提供绑定所需的通知接口
    /// </summary>
    public class NotifyBase : INotifyPropertyChanged
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
}
