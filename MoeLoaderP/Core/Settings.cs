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
    public class Settings : BindingObject
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

        private double _mainWindowTop;
        public double MainWindowTop
        {
            get
            {
                var sh = SystemParameters.WorkArea.Height;
                if (Math.Abs(_mainWindowTop) < 1d) return sh / 2 - _mainWindowHeight / 2;
                return _mainWindowTop;
            }
            set => SetField(ref _mainWindowTop, value, nameof(MainWindowTop));
        }

        private double _mainWindowLeft;
        public double MainWindowLeft
        {
            get
            {
                var sw = SystemParameters.WorkArea.Width;
                if (Math.Abs(_mainWindowLeft) < 1d) return sw / 2 - _mainWindowWidth / 2;
                return _mainWindowLeft;
            }
            set => SetField(ref _mainWindowLeft, value, nameof(MainWindowLeft));
        }

        private string _imageSavePath = App.MoePicFolder;
        public string ImageSavePath
        {
            get => _imageSavePath;
            set => SetField(ref _imageSavePath, value, nameof(ImageSavePath));
        }

        private bool _isUseCustomFileNameFormat;

        public bool IsUseCustomFileNameFormat
        {
            get => _isUseCustomFileNameFormat;
            set => SetField(ref _isUseCustomFileNameFormat, value, nameof(IsUseCustomFileNameFormat));
        }

        private string _saveFileNameFormat= "%origin";

        public string SaveFileNameFormat
        {
            get => _saveFileNameFormat;
            set => SetField(ref _saveFileNameFormat, value, nameof(SaveFileNameFormat));
        }

        public enum ProxyModeEnum
        {
            None = 0,
            System = 1,
            Custom = 2
        }

        private ProxyModeEnum _proxyMode = ProxyModeEnum.System;

        public ProxyModeEnum ProxyMode
        {
            get => _proxyMode;
            set => SetField(ref _proxyMode, value, nameof(ProxyMode));
        }

        [JsonIgnore]
        public IWebProxy Proxy
        {
            get
            {
                switch (ProxyMode)
                {
                    case ProxyModeEnum.None: return new WebProxy();
                    case ProxyModeEnum.System: return WebRequest.DefaultWebProxy;
                    case ProxyModeEnum.Custom:
                    {
                        try
                        {
                            var strs = ProxySetting.Split(':');
                            var port = int.Parse(strs[1]);
                            var address = IPAddress.Parse(strs[0]);
                            var porxy = new WebProxy(address.ToString(), port);
                            return porxy;
                        }
                        catch (Exception e)
                        {
                            App.Log(e);
                            return WebRequest.DefaultWebProxy;
                        }
                    }

                }
                return null;
            }
        }

        private string _proxySetting = "127.0.0.1:1080";

        public string ProxySetting
        {
            get => _proxySetting;
            set => SetField(ref _proxySetting, value, nameof(ProxySetting));
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
        
        public void Save()
        {
            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText(App.SettingJsonFilePath, json);
        }

        public static Settings Load()
        {
            Settings settings;
            try
            {
                if (File.Exists(App.SettingJsonFilePath))
                {
                    var json = File.ReadAllText(App.SettingJsonFilePath);
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
                App.Log(ex);
                settings = new Settings();
            }
            return settings;
        }
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
}
