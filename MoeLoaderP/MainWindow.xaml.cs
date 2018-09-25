using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MoeLoader.Core;
using MoeLoader.UI;
using Newtonsoft.Json;

namespace MoeLoader
{
    public partial class MainWindow
    {
        private Settings Settings { get; }
        private SiteManager SiteManager { get; }
        private SearchSession CurrentSearch { get; set; }

        public MainWindow(Settings settings)
        {
            Settings = settings;
            SiteManager = new SiteManager(Settings);
            InitializeComponent();
            DataContext = Settings;
            Loaded += OnLoaded;
            Closing += OnClosing;
            KeyDown += OnKeyDown;
            MouseLeftButtonDown += (sender, args) => DragMove();
            DownloaderMenuCheckBox.Checked += DownloaderMenuCheckBoxCheckChanged;
            DownloaderMenuCheckBox.Unchecked += DownloaderMenuCheckBoxCheckChanged;
            // elements
            MoeSettingsControl.Init(Settings);
            ImageSizeSlider.MouseWheel += ImageSizeSliderOnMouseWheel;
            SettingsMenuCheckBox.Checked += (sender, args) => SeetingsPopupGrid.LargenShowSb().Begin(); 
            AboutMenuCheckBox.Checked += (sender, args) => AboutPopupGrid.LargenShowSb().Begin();

            // explorer
            MoeExlorer.Settings = Settings;
            MoeExlorer.NextPageButton.Click += NextPageButtonOnClick;
            MoeExlorer.AnyImageLoaded += MoeExlorerOnAnyImageLoaded;
            MoeExlorer.ImageItemDownloadButtonClicked += MoeExlorerOnImageItemDownloadButtonClicked;
            MoeExlorer.MouseWheel += MoeExlorerOnMouseWheel;
            MoeExlorer.ContextMenuTagButtonClicked += MoeExlorerOnContextMenuTagButtonClicked;
            MoeExlorer.DownloadSelectedImagesButton.Click += DownloadSelectedImagesButtonOnClick;

            // downloader
            MoeDownloaderControl.Init(Settings);

            // search control
            SearchControl.Init(SiteManager, Settings);
            SearchControl.SearchButton.Click += SearchButtonOnClick;

            //about
            AboutVersionTextBlock.Text = $"版本：{App.Version.ToString(3)} ({App.CompileTime:yyyy/MM/dd})";
            AboutDonateLink.MouseLeftButtonUp += (sender, args) => AboutDonateImageGrid.Visibility = Visibility.Visible;
            AboutDonateImage.MouseLeftButtonUp += (sender, args) => AboutDonateImageGrid.Visibility = Visibility.Collapsed;
            AboutHomeLinkButton.Click += (sender, args) => Process.Start("http://leaful.com/moeloader-p");
        }

        private void DownloaderMenuCheckBoxCheckChanged(object sender, RoutedEventArgs e)
        {
            var ischecked = DownloaderMenuCheckBox.IsChecked == true;
            VisualStateManager.GoToElementState(LayoutRoot, ischecked ? nameof(ShowDownloadPanelState) : nameof(HideDownloadPanelState), true);
        }

        private void DownloadSelectedImagesButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (DownloaderMenuCheckBox.IsChecked == false) DownloaderMenuCheckBox.IsChecked = true;
            foreach (var ctrl in MoeExlorer.SelectedImageControls)
            {
                MoeDownloaderControl.AddDownload(ctrl.ImageItem, ctrl.PreviewImage.Source);
            }
        }

        private void MoeExlorerOnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl)) ImageSizeSlider.Value += e.Delta;
        }

        private void MoeExlorerOnContextMenuTagButtonClicked(ImageItem item, string str)
        {
            SearchControl.KeywordComboBox.KeywordText = str;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            NewVersionPanel.Visibility = Visibility.Collapsed;
            try
            {
                var updateTask =  CheckUpdateAsync();
                var thankTask = CheckThankListAsync();
                await updateTask;
                await thankTask;
            }
            catch (Exception ex)
            {
                App.Log("Updater.CheckUpdateAsync() Fail", ex);
            }
        }

        private void ImageSizeSliderOnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var v = ImageSizeSlider.Value;
            v = v +  e.Delta / 5d;
            if (v > ImageSizeSlider.Minimum && v < ImageSizeSlider.Maximum)
            {
                ImageSizeSlider.Value += e.Delta / 5d;
            }
        }

        private void MoeExlorerOnImageItemDownloadButtonClicked(ImageItem item, ImageSource imgSource)
        {
            MoeDownloaderControl.AddDownload(item, imgSource);
            if (DownloaderMenuCheckBox.IsChecked != false) return;
            DownloaderMenuCheckBox.IsChecked = true;
        }

        private void MoeExlorerOnAnyImageLoaded(MoeExplorerControl obj)
        {
            StatusTextBlock.Text = MoeExlorer.ImageLoadingPool.Count == 0 ? "图片加载完毕" 
                : $"剩余 {MoeExlorer.ImageLoadingPool.Count + MoeExlorer.ImageWaitForLoadingPool.Count} 张图片等待加载";
        }

        private int _f8KeyDownTimes;
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1: // 用于测试功能
                    ShowPopupMessage($"{Settings.ProxyMode}");
                    break;
                case Key.F2:
                    //ShowPopupMessage($"{Settings.SaveFileNameFormat}");
                    break;
                case Key.F8:
                    if (!Settings.HaveEnteredXMode)
                    {
                        _f8KeyDownTimes++;
                        if (_f8KeyDownTimes <= 3) break;
                        if (_f8KeyDownTimes > 3 && _f8KeyDownTimes < 10)
                        {
                            ShowPopupMessage($"还剩 {10 - _f8KeyDownTimes} 次粉碎！");
                            break;
                        }
                        if (_f8KeyDownTimes >= 10) Settings.HaveEnteredXMode = true;
                    }
                    ShowPopupMessage(Settings.IsXMode ? "已关闭 18X 模式" : "已开启 18X 模式");
                    Settings.IsXMode = !Settings.IsXMode;
                    SiteManager.Sites.Clear();
                    SiteManager.SetDefaultSiteList();
                    SearchControl.MoeSitesComboBox.SelectedIndex = 0;
                    break;
            }
        }

        private async void NextPageButtonOnClick(object sender, RoutedEventArgs e)
        {
            ChangeSearchVisual(true);
            var task = await CurrentSearch.TrySearchNextPageAsync();
            if (task.IsCanceled)
            {
                //MessageBox.Show(t.Exception?.ToString());
            }
            else if (task.Exception != null)
            {
                MessageWindow.Show(task.Exception, null, this);
                ChangeSearchVisual(false);
            }
            else
            {
                ChangeSearchVisual(false);
                MoeExlorer.RefreshPaging(CurrentSearch);
            }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            Settings.Save();
            if (!MoeDownloaderControl.IsDownloading) return;
            var result = MessageBox.Show("正在下载图片，确定要关闭程序吗？", App.DisplayName, MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) e.Cancel = true;
            else MoeDownloaderControl.StopAll();
        }

        private async void SearchButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (CurrentSearch?.IsSearching == true)
            {
                CurrentSearch.StopSearch();
                ChangeSearchVisual(false);
                return;
            }
            ChangeSearchVisual(true);
            CurrentSearch = new SearchSession(Settings, SearchControl.GetSearchPara());
            CurrentSearch.SearchStatusChanged += (session, s) => StatusTextBlock.Text = s;
            SiteTextBlock.Text = $"当前站点：{CurrentSearch.CurrentSearchPara.Site.DisplayName}";
            Settings.HistoryKeywords.AddHistory(CurrentSearch.CurrentSearchPara.Keyword, Settings);
            var t = await CurrentSearch.TrySearchNextPageAsync();
            if (t.IsCanceled)
            {
                //MessageBox.Show(t.Exception?.ToString());
            }
            else if (t.Exception != null)
            {
                MessageWindow.Show(t.Exception, null, this);
                ChangeSearchVisual(false);
            }
            else
            {
                ChangeSearchVisual(false);
                MoeExlorer.RefreshPaging(CurrentSearch);
            }
        }

        public void ChangeSearchVisual(bool isSearching)
        {
            if (isSearching)
            {
                if (CurrentSearch == null) this.Sb("BeginSearchSb").Begin();
                VisualStateManager.GoToState(SearchControl, nameof(SearchControl.SearchingState), true);
                MoeExlorer.SearchStartedVisual();
            }
            else
            {
                MoeExlorer.SearchStopedVisual();
                VisualStateManager.GoToState(SearchControl, nameof(SearchControl.StopingState), true);
            }
        }
        
        public void ShowPopupMessage(string message)
        {
            PopupMessageTextBlock.Text = message;
            this.Sb("PopupMessageShowSb").Begin();
        }

        public async Task CheckUpdateAsync()
        {
            var htpp = new HttpClient();
            var upjson = await htpp.GetStringAsync(new Uri(App.SaeUrl + "update.json", UriKind.Absolute));
            dynamic upobject = JsonConvert.DeserializeObject(upjson);
            if(upobject==null)return;
            if (Version.Parse($"{upobject.NetVersion}") > App.Version)
            {
                ShowPopupMessage($"软件新版提示：{upobject.NetVersion}({upobject.RealeseDate})；更新内容：{upobject.RealeseNotes}；更新请点“关于”按钮");
                NewVersionTextBlock.Text = $"新版提示：{upobject.NetVersion}({upobject.RealeseDate})；更新内容：{upobject.RealeseNotes}";
                NewVersionPanel.Visibility = Visibility.Visible;
                NewVersionDownloadButton.Click += (sender, args) => Process.Start($"{upobject.UpdateUrl}");
            }
        }

        public async Task CheckThankListAsync()
        {
            var htpp = new HttpClient();
            var upjson = await htpp.GetStringAsync(new Uri(App.SaeUrl + "thanklist.json", UriKind.Absolute));
            dynamic jobject = JsonConvert.DeserializeObject(upjson);
            if (jobject == null) return;
            foreach (var user in jobject)
            {
                var button = new Button();

            }
        }
    }
}
