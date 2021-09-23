using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;
using MoeLoaderP.Wpf.ControlParts;

namespace MoeLoaderP.Wpf
{
    public partial class MainWindow
    {
        private Settings Settings { get; set; }
        private SiteManager SiteManager { get; set; }
        private SearchSession CurrentSearch { get; set; }
        public PreviewWindow PreviewWindowInstance { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();
        }

        public void Init(Settings settings)
        {
            Settings = settings;
            SiteManager = new SiteManager(Settings);

            DataContext = Settings;
            Closing += OnClosing;
            PreviewKeyDown += OnPreviewKeyDown;
            MouseLeftButtonDown += delegate { DragMove(); };
            Ex.ShowMessageAction += ShowMessage;

            // logo / menu : setting ,downloader, about
            DownloaderMenuCheckBox.Checked += DownloaderMenuCheckBoxCheckChanged;
            DownloaderMenuCheckBox.Unchecked += DownloaderMenuCheckBoxCheckChanged;
            MoeDownloaderControl.Init(Settings);
            MoeSettingsControl.Init(Settings);
            AboutControl.Init();
            LogoImageButton.MouseRightButtonDown += LogoImageButtonOnMouseRightButtonDown;

            // explorer
            MoeExplorer.Settings = Settings;
            MoeExplorer.NextPageButton.Click += NextPageButtonOnClick;
            MoeExplorer.ImageItemDownloadButtonClicked += MoeExplorerOnImageItemDownloadButtonClicked;
            MoeExplorer.DownloadSelectedImagesButton.Click += DownloadSelectedImagesButtonOnClick;
            MoeExplorer.SearchByAuthorIdAction += SearchByAuthorIdAction;
            MoeExplorer.MoeItemPreviewButtonClicked += MoeExplorerOnMoeItemPreviewButtonClicked;
            
            // search
            SearchControl.Init(SiteManager, Settings);
            SearchControl.SearchButton.Click += SearchButtonOnClick;
            SearchControl.AccountButton.Click += AccountButtonOnClick;
            SearchControl.KeywordTextBox.KeyDown += OnKeywordTextBoxOnKeyDown;

            // helper : collect ,log
            MoeExplorer.OutputSelectedImagesUrlsButton.Click += OutputSelectedImagesUrlsButtonOnClick;
            CollectCopyAllButton.Click += delegate { CollectTextBox.Text.CopyToClipboard(); }; 
            CollectClearButton.Click += delegate { CollectTextBox.Text = string.Empty; };
            Ex.LogAction += Log;
            LogListBox.MouseRightButtonUp += LogListBoxOnMouseRightButtonUp;
            ImageSizeSlider.MouseWheel += ImageSizeSliderOnMouseWheel;
            // egg
            LogoImageButton.Click += LogoImageButtonOnClick;

            // settings
            MoeSettingsControl.BgImageChangeButton.Click += delegate { ChangeBgImage(); };
            ChangeBgImage();
            
            // gen custom test
            var cus = new CustomSiteFactory();
            cus.GenTestSites();
            cus.OutputJson(App.CustomSiteDir);

            Settings.PropertyChanged += SettingsOnPropertyChanged;
        }

        private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.IsCustomSiteMode))
            {
                SiteManager.Sites.Clear();
                if (Settings.IsCustomSiteMode) SiteManager.SetCustomSitesFormJson(App.CustomSiteDir);
                else SiteManager.SetDefaultSiteList();
            }
        }

        private void OnKeywordTextBoxOnKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Enter)
            {
                SearchButtonOnClick(sender, args);
                SearchControl.KeywordPopup.IsOpen = false;
            }
        }

        private void LogoImageButtonOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Settings.IsCustomSiteMode = !Settings.IsCustomSiteMode;
            LayoutRoot.GoElementState(Settings.IsCustomSiteMode ? nameof(CustomSitesState): nameof(DefaultSitesState));
            LogoImage.Visibility = Settings.IsCustomSiteMode ? Visibility.Collapsed : Visibility.Visible;
            LogoImage2.Visibility = Settings.IsCustomSiteMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MoeExplorerOnMoeItemPreviewButtonClicked(MoeItem item,ImageSource imgSource)
        {
            if (PreviewWindowInstance == null)
            {
                PreviewWindowInstance = new PreviewWindow();
                PreviewWindowInstance.Closed += delegate { PreviewWindowInstance = null; }; 
                PreviewWindowInstance.Owner = this;
                PreviewWindowInstance.Width = Width * 0.85d;
                PreviewWindowInstance.Height = Height * 0.85d;
                PreviewWindowInstance.Show();
            }
            else
            {
                PreviewWindowInstance.Activate();
            }

            PreviewWindowInstance.Init(item, imgSource);

        }

        private void OutputSelectedImagesUrlsButtonOnClick(object sender, RoutedEventArgs e)
        {
            foreach (var ctrl in MoeExplorer.SelectedImageControls)
            {
                var url = ctrl.MoeItem.DownloadUrlInfo;
                if (ctrl.MoeItem.ChildrenItems.Count > 0)
                {
                    foreach (var item in ctrl.MoeItem.ChildrenItems)
                    {
                        CollectTextBox.Text += item.DownloadUrlInfo?.Url + Environment.NewLine;
                    }
                }
                else CollectTextBox.Text += url?.Url + Environment.NewLine;
            }

            foreach(MoeItemControl ct in MoeExplorer.ImageItemsWrapPanel.Children) 
                ct.ImageCheckBox.IsChecked = false;

            Ex.ShowMessage("已添加至收集箱");
        }
        
        private async void AccountButtonOnClick(object sender, RoutedEventArgs e)
        {
            var wnd = new LoginWindow();
            await wnd.Init(Settings, SearchControl.CurrentSelectedSite);
            try
            {
                wnd.Owner = this;
                wnd.ShowDialog();
                SearchControl.Refresh();
            }
            catch (Exception ex)
            {
                Ex.Log(ex);
            }
        }

        private void LogListBoxOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

            if (LogListBox.SelectedIndex == -1) return;
            if (LogListBox.SelectedItem is TextBlock tb) tb.Text.CopyToClipboard();
        }

        private int LogoClickCount { get; set; }

        private void LogoImageButtonOnClick(object sender, RoutedEventArgs e)
        {
            LogoClickCount++;
            switch (LogoClickCount)
            {
                case 10:
                    ShowMessage("你好！");
                    break;
                case 15:
                    ShowMessage("你好像很无聊！");
                    break;
                case 20:
                    ShowMessage("你这样真的好吗？？！");
                    break;
                case 30:
                    ShowMessage("再点！再点就把你喝掉~~");
                    break;
                case 40:
                    ShowMessage("你好像真的好无聊 啊ᕦ(･ㅂ･)ᕤ");
                    break;
                case 50:
                    ShowMessage("你想和我说话吗(・ω<) ﾃﾍﾍﾟﾛ");
                    break;
                case 60:
                    ShowMessage("我不想和你说(╬￣皿￣)");
                    break;
                case 70:
                    ShowMessage("再点就要爆炸了！！＜(▰˘◡˘▰)");
                    break;
                case 80:
                    ShowMessage("你不信吗？？？？(*｀Ω´*)v");
                    break;
                case > 90 and < 120 when LogoClickCount % 3==0:
                    ShowMessage($"{(121 - LogoClickCount) / 3}!!");
                    break;
                case 130:
                    ShowMessage("嘻嘻~我和你说说话");
                    break;
                case 140:
                    ShowMessage("其实后面还有哦！！");
                    break;
                case 150:
                    ShowMessage("其实，我对你…………");
                    break;
                case 160:
                    ShowMessage("非常讨厌！！！(ノ｀Д)ノ");
                    break;
                case 170:
                    ShowMessage("不过看你真的很无聊！我就给你点惊喜吧！");
                    break;
                case 180:
                    ShowMessage("看招！！！！！！");
                    break;
                case 190:
                    ShowMessage("嘿！！！！开始啦！！！看看我的无敌雪景！！");
                    new EggWindow().Show();
                    break;
                default:
                    break;
            }

            if (LogoClickCount > 120)
            {
                var files = $@"{App.ExeDir}\Assets\Egg".GetDirFiles();
                var rndfile = files[new Random().Next(0, files.Length - 1)];
                Player.Source = new Uri(rndfile.FullName, UriKind.Absolute);
                Player.Stop();
                Player.Play();
            }
        }

        public void ChangeBgImage()
        {
            var files = App.BackgroundImagesDir.GetDirFiles()
                .Where(info => info.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase)).ToArray();
            var rndfile = files[new Random().Next(0, files.Length)];
            BgImage.Source = new BitmapImage(new Uri(rndfile.FullName, UriKind.Absolute));
            BgGridViewBox.Width = 670;
            BgGridViewBox.Height = 530;
            BgGridViewBox.HorizontalAlignment = HorizontalAlignment.Right;
            try
            {
                var par = rndfile.Name.Remove(rndfile.Name.LastIndexOf(".", StringComparison.Ordinal));
                BgGridViewBox.SetBgPos(par);
            }
            catch (Exception e)
            {
                Ex.Log(e);
            }
        }

        private void SearchByAuthorIdAction(MoeSite arg1, string arg2)
        {
            SearchControl.CurrentSelectedSite = arg1;
            SearchControl.MoeSitesLv2ComboBox.SelectedIndex = 1;
            SearchControl.KeywordTextBox.Text = arg2;
            SearchButtonOnClick(null, null);
        }

        private void Log(string obj)
        {
            var tb = new TextBlock
            {
                Text = obj
            };
            LogListBox.Items.Add(tb);
            LogListBox.ScrollIntoView(tb);
            if (LogListBox.Items.Count > 500) LogListBox.Items.RemoveAt(0);
        }

        private void ShowMessage(string mes, string detailMes = null, Ex.MessagePos pos = Ex.MessagePos.Popup)
        {
            switch (pos)
            {
                case Ex.MessagePos.Popup:
                    PopupMessageTextBlock.Text = mes;
                    this.Sb("PopupMessageShowSb").Begin();
                    break;
                case Ex.MessagePos.InfoBar:
                    StatusTextBlock.Text = mes;
                    this.Sb("InfoBarEmphasisSb").Begin();
                    break;
                case Ex.MessagePos.Window:
                    MessageWindow.ShowDialog(mes, detailMes, this);
                    break;
            }
        }

        private void DownloaderMenuCheckBoxCheckChanged(object sender, RoutedEventArgs e)
        {
            var ischecked = DownloaderMenuCheckBox.IsChecked == true;
            LayoutRoot.GoElementState(ischecked ? nameof(ShowDownloadPanelState) : nameof(HideDownloadPanelState));
        }

        private void DownloadSelectedImagesButtonOnClick(object sender, RoutedEventArgs e)
        {
            var count = 0;
            foreach (var ctrl in MoeExplorer.SelectedImageControls)
            {
                var img = ctrl.MoeItem;
                if (img.DownloadUrlInfo?.Url == null) continue;
                MoeDownloaderControl.Downloader.AddDownload(img, ctrl.PreviewImage.Source);
                count++;
            }
            if (DownloaderMenuCheckBox.IsChecked == false && count>0) DownloaderMenuCheckBox.IsChecked = true;
            else Ex.ShowMessage("没有图片可以下载T_T");
            foreach (MoeItemControl ct in MoeExplorer.ImageItemsWrapPanel.Children)
            {
                ct.ImageCheckBox.IsChecked = false;
            }

            var lb = MoeDownloaderControl.DownloadItemsListBox;
            if (lb.Items.Count != 0)
            {
                lb.ScrollIntoView(lb.Items[^1]);
            }

        }

        private void ImageSizeSliderOnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var v = ImageSizeSlider.Value;
            v += e.Delta / 5d;
            if (v > ImageSizeSlider.Minimum && v < ImageSizeSlider.Maximum) ImageSizeSlider.Value += e.Delta / 5d;
        }

        private void MoeExplorerOnImageItemDownloadButtonClicked(MoeItem item, ImageSource imgSource)
        {
            MoeDownloaderControl.Downloader.AddDownload(item, imgSource);
            var lb = MoeDownloaderControl.DownloadItemsListBox;
            var ctrl = lb.Items[^1];
            lb.ScrollIntoView(ctrl);
            if (DownloaderMenuCheckBox.IsChecked != false) return;
            DownloaderMenuCheckBox.IsChecked = true;
        }

        private int _f8KeyDownTimes;
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Right:
                    if (CurrentSearch?.LoadedVisualPages?.Count > 0) NextPageButtonOnClick(this, null);
                    break;
                case Key.F1: // 用于测试功能
                    ShowMessage("test");
                    break;
                case Key.F8:
                    if (!Settings.HaveEnteredXMode)
                    {
                        _f8KeyDownTimes++;
                        if (_f8KeyDownTimes <= 3) break;
                        if (_f8KeyDownTimes is > 3 and < 10)
                        {
                            ShowMessage($"还剩 {10 - _f8KeyDownTimes} 次粉碎！");
                            break;
                        }
                        if (_f8KeyDownTimes >= 10) Settings.HaveEnteredXMode = true;
                    }
                    ShowMessage(Settings.IsXMode ? "已关闭 R18 模式" : "已开启 R18 模式");
                    Settings.IsXMode = !Settings.IsXMode;
                    SiteManager.Sites.Clear();
                    SiteManager.SetDefaultSiteList();
                    SearchControl.MoeSitesLv1ComboBox.SelectedIndex = 0;
                    break;
            }
        }
        private async void SearchButtonOnClick(object sender, RoutedEventArgs e)
        {
            ChangeBgImage();
            if (CurrentSearch?.IsSearching == true)
            {
                CurrentSearch.StopSearch();
                ChangeSearchVisual(false);
                return;
            }
            ChangeSearchVisual(true);
            StatusTextBlock.Text = "";
            var para = SearchControl.GetSearchPara();
            CurrentSearch = new SearchSession(Settings, para);
            SiteTextBlock.Text = CurrentSearch.GetCurrentSearchStateText();
            Settings.HistoryKeywords.AddHistory(CurrentSearch.CurrentSearchPara.Keyword, Settings);
            MoeExplorer.ResetVisual();
            var t = await CurrentSearch.TrySearchNextPageAsync();
            if (t.IsCanceled || t.Exception != null)
            {
                if (!CurrentSearch.IsSearching) ChangeSearchVisual(false);
            }
            else
            {
                ChangeSearchVisual(false);
                MoeExplorer.AddPage(CurrentSearch);
            }
        }
        private async void NextPageButtonOnClick(object sender, RoutedEventArgs e)
        {
            ChangeSearchVisual(true);
            var t = await CurrentSearch.TrySearchNextPageAsync();
            if (t.IsCanceled || t.Exception != null)
            {
                if (!CurrentSearch.IsSearching) ChangeSearchVisual(false);
            }
            else
            {
                ChangeSearchVisual(false);
                if (Settings.IsClearImagesWhenSearchNextPage) MoeExplorer.ResetVisual();
                MoeExplorer.AddPage(CurrentSearch);
            }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            Settings.Save(App.SettingJsonFilePath);
            if (!MoeDownloaderControl.Downloader.IsDownloading) return;
            var result = MessageBox.Show(this, "正在下载图片，确定要关闭吗？", 
                App.DisplayName, MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) e.Cancel = true;
        }

        public void ChangeSearchVisual(bool isSearching)
        {
            if (isSearching)
            {
                if (CurrentSearch == null) this.Sb("BeginSearchSb").Begin();
                SearchControl.GoState(nameof(SearchControl.SearchingState));
                MoeExplorer.SearchStartedVisual();
            }
            else
            {
                MoeExplorer.SearchStopVisual();
                SearchControl.GoState(nameof(SearchControl.StopingState));
            }
        }

    }
}
