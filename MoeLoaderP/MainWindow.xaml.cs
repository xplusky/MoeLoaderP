using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
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
            SettingsMenuCheckBox.Checked += SettingsMenuCheckBoxOnChecked;
            AboutMenuCheckBox.Checked += (sender, args) => AboutPopupGrid.LargenShowSb().Begin();

            // explorer
            MoeExlorer.Settings = Settings;
            MoeExlorer.NextPageButton.Click += NextPageButtonOnClick;
            MoeExlorer.AnyImageLoaded += MoeExlorerOnAnyImageLoaded;
            MoeExlorer.ImageItemDownloadButtonClicked += MoeExlorerOnImageItemDownloadButtonClicked;
            MoeExlorer.MouseWheel += MoeExlorerOnMouseWheel;
            MoeExlorer.AllImagesLoaded += MoeExlorerOnAllImageLoaded;
            MoeExlorer.ContextMenuTagButtonClicked += MoeExlorerOnContextMenuTagButtonClicked;
            MoeExlorer.DownloadSelectedImagesButton.Click += DownloadSelectedImagesButtonOnClick;

            // downloader
            MoeDownloaderControl.Init(Settings);

            // search control
            SearchControl.Init(SiteManager, Settings);
            SearchControl.SearchButton.Click += SearchButtonOnClick;
        }

        private void DownloaderMenuCheckBoxCheckChanged(object sender, RoutedEventArgs e)
        {
            var ischecked = DownloaderMenuCheckBox.IsChecked == true;
            DownloaderMenuCheckBox.ToolTip = ischecked ? "隐藏下载面板" : "显示下载面板";
            this.Sb(ischecked ? "ShowDownloaderSb" : "HideDownloaderSb").Begin();
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
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                ImageSizeSlider.Value += e.Delta;
            }
        }

        private void MoeExlorerOnContextMenuTagButtonClicked(ImageItem arg1, string arg2)
        {
            SearchControl.KeywordComboBox.KeywordText = arg2;
        }


        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await CheckUpdateAsync();
        }

        private void SettingsMenuCheckBoxOnChecked(object sender, RoutedEventArgs e)
        {
            SeetingsPopupGrid.LargenShowSb().Begin();
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
                : $"剩余{MoeExlorer.ImageLoadingPool.Count + MoeExlorer.ImageWaitForLoadingPool.Count}张图片等待加载";
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    ShowPopupMessage($"{Settings.MaxOnLoadingImageCount}");
                    break;
                case Key.F2:
                    ShowPopupMessage($"{Settings.SaveFileNameFormat}");
                    break;
                case Key.F8:
                    ShowPopupMessage(Settings.IsXMode ? "已关闭 X 模式" : "已开启 X 模式");
                    Settings.IsXMode = !Settings.IsXMode;
                    SiteManager.Sites.Clear();
                    SiteManager.SetDefaultSiteList();
                    SearchControl.MoeSitesComboBox.SelectedIndex = 0;
                    break;
            }
        }

        private async void NextPageButtonOnClick(object sender, RoutedEventArgs e)
        {
            MoeExlorer.SearchStartedVisual(); 
            try
            {
                await CurrentSearch.SearchNextPageAsync();
            }
            catch (TaskCanceledException ex)
            {
                MessageBox.Show(ex.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            MoeExlorer.SearchStopedVisual();
            VisualStateManager.GoToState(SearchControl, nameof(SearchControl.StopingState), true);
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            Settings.Save();
            if (!MoeDownloaderControl.IsDownloading) return;
            var result = MessageBox.Show("正在下载图片，确定要关闭程序吗？", AppRes.AppDisplayName, MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) e.Cancel = true;
            else MoeDownloaderControl.StopAll();
        }

        private void MoeExlorerOnAllImageLoaded(MoeExplorerControl obj)
        {
            //CurrentSearch.IsSearching = false;
        }

        private async void SearchButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (CurrentSearch?.IsSearching == true)
            {
                CurrentSearch.StopSearch();
                VisualStateManager.GoToState(SearchControl, nameof(SearchControl.StopingState), true);
            }
            else
            {
                if (CurrentSearch == null) this.Sb("BeginSearchSb").Begin();
                
                MoeExlorer.SearchStartedVisual();
                CurrentSearch = new SearchSession(Settings, SearchControl.GetSearchPara());
                CurrentSearch.SearchCompleted += CurrentSearchOnSearchCompleted;
                CurrentSearch.SearchStatusChanged += (session, s) => StatusTextBlock.Text = s;
                SiteTextBlock.Text = $"当前站点：{CurrentSearch.CurrentSearchPara.Site.DisplayName}";
                Settings.HistoryKeywords.AddHistory(CurrentSearch.CurrentSearchPara.Keyword, Settings);
                VisualStateManager.GoToState(SearchControl, nameof(SearchControl.SearchingState), true);
                try
                {
                    await CurrentSearch.SearchNextPageAsync();
                }
                catch (TaskCanceledException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    if (Debugger.IsAttached) throw;
                }

                CurrentSearch.StopSearch();
                MoeExlorer.SearchStopedVisual();
                VisualStateManager.GoToState(SearchControl, nameof(SearchControl.StopingState), true);
            }
        }

        private void CurrentSearchOnSearchCompleted(SearchSession session)
        {
            MoeExlorer.RefreshPages(CurrentSearch);
        }

        public void ShowPopupMessage(string message)
        {
            PopupMessageTextBlock.Text = message;
            this.Sb("PopupMessageShowSb").Begin();
        }

        public async Task CheckUpdateAsync()
        {
            var htpp = new HttpClient();
            try
            {
                var upjson = await htpp.GetStringAsync(new Uri(AppRes.AppSaeUrl + "update.json", UriKind.Absolute));
                dynamic upobject = JsonConvert.DeserializeObject(upjson);
                if (Version.Parse($"{upobject?.NetVersion}") > AppRes.AppVersion)
                {
                    ShowPopupMessage($"MoeLoader +1s 新版提示：{upobject?.NetVersion}({upobject?.RealeseDate})；更新内容：{upobject?.RealeseNotes}；更新请点“关于”按钮");
                }
            }
            catch (Exception e)
            {
                Extend.Log("Updater.CheckUpdateAsync() Fail", e);
            }
        }
    }
}
