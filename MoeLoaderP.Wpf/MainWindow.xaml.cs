using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;
using MoeLoaderP.Wpf.ControlParts;
using MoeLoaderP.Wpf.Egg;

namespace MoeLoaderP.Wpf
{
    public partial class MainWindow
    {
        private Settings Settings { get; set; }
        private SiteManager SiteManager { get; set; }
        private SearchSession CurrentSearch { get; set; }
        
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
            MouseLeftButtonDown += (sender, args) => DragMove();
            Extend.ShowMessageAction += ShowMessage;

            // menu : setting ,downloader, about
            DownloaderMenuCheckBox.Checked += DownloaderMenuCheckBoxCheckChanged;
            DownloaderMenuCheckBox.Unchecked += DownloaderMenuCheckBoxCheckChanged;
            MoeDownloaderControl.Init(Settings);
            MoeSettingsControl.Init(Settings);
            AboutControl.Init();
            ChangeModeButton.Click += ChangeModeButtonOnClick;

            // explorer
            MoeExplorer.Settings = Settings;
            MoeExplorer.NextPageButton.Click += NextPageButtonOnClick;
            MoeExplorer.ImageItemDownloadButtonClicked += MoeExplorerOnImageItemDownloadButtonClicked;
            MoeExplorer.DownloadSelectedImagesButton.Click += DownloadSelectedImagesButtonOnClick;
            MoeExplorer.SearchByAuthorIdAction += SearchByAuthorIdAction;
            
            // search
            SearchControl.Init(SiteManager, Settings);
            SearchControl.SearchButton.Click += SearchButtonOnClick;
            SearchControl.AccountButton.Click += AccountButtonOnClick;

            // helper : collect ,log
            MoeExplorer.OutputSelectedImagesUrlsButton.Click += OutputSelectedImagesUrlsButtonOnClick;
            CollectCopyAllButton.Click += (sender, args) => CollectTextBox.Text.CopyToClipboard(); 
            CollectClearButton.Click += (sender, args) => CollectTextBox.Text = string.Empty;
            Extend.LogAction += Log;
            LogListBox.MouseRightButtonUp += LogListBoxOnMouseRightButtonUp;
            ImageSizeSlider.MouseWheel += ImageSizeSliderOnMouseWheel;
            // egg
            LogoImageButton.Click += LogoImageButtonOnClick;
            ChangeBgImage();
            
        }

        private void ChangeModeButtonOnClick(object sender, RoutedEventArgs e)
        {
            LayoutRoot.GoElementState(Settings.IsCustomSiteMode ? nameof(DefaultSitesState) : nameof(CustomSitesState));
            Settings.IsCustomSiteMode = !Settings.IsCustomSiteMode;
        }

        private void OutputSelectedImagesUrlsButtonOnClick(object sender, RoutedEventArgs e)
        {
            foreach (var ctrl in MoeExplorer.SelectedImageControls)
            {
                var url = ctrl.ImageItem.DownloadUrlInfo;
                if (ctrl.ImageItem.ChildrenItems.Count > 0)
                {
                    foreach (var item in ctrl.ImageItem.ChildrenItems)
                    {
                        CollectTextBox.Text += item.DownloadUrlInfo?.Url + Environment.NewLine;
                    }
                }
                else
                {
                    CollectTextBox.Text += url?.Url + Environment.NewLine;
                }
            }

            foreach(ImageControl ct in MoeExplorer.ImageItemsWrapPanel.Children) ct.ImageCheckBox.IsChecked = false;

            Extend.ShowMessage("已添加至收集箱");
        }
        
        private void AccountButtonOnClick(object sender, RoutedEventArgs e)
        {
            var wnd = new LoginWindow();
            wnd.Init(Settings, SearchControl.CurrentSelectedSite);
            wnd.Owner = this;
            wnd.ShowDialog();
        }

        private void LogListBoxOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

            if(LogListBox.SelectedIndex == -1)return;
            if (LogListBox.SelectedItem is TextBlock tb)
            {
                tb.Text.CopyToClipboard();
            }
        }

        private int LogoClickCount { get; set; }

        private void LogoImageButtonOnClick(object sender, RoutedEventArgs e)
        {
            LogoClickCount++;
            var c = LogoClickCount;
            if (c == 10) ShowMessage("你好！");
            if (c == 15) ShowMessage("你好像很无聊！");
            if (c == 20) ShowMessage("你这样真的好吗？？！");
            if (c == 30) ShowMessage("再点！再点就把你喝掉~~");
            if (c == 40) ShowMessage("你好像真的好无聊 啊ᕦ(･ㅂ･)ᕤ");
            if (c == 50) ShowMessage("你想和我说话吗(・ω<) ﾃﾍﾍﾟﾛ");
            if (c == 60) ShowMessage("我不想和你说(╬￣皿￣)");
            if (c == 70) ShowMessage("再点就要爆炸了！！＜(▰˘◡˘▰)");
            if (c == 80) ShowMessage("你不信吗？？？？(*｀Ω´*)v");
            if (c > 90 && c < 100) ShowMessage($"{100 - c}");
            if (c >= 100)
            {
                var dir = new DirectoryInfo($"{App.ExeDir}\\Egg\\Res");
                var files = dir.GetFiles();
                var rnd = new Random();
                var rndfile = files[rnd.Next(0, files.Length - 1)];
                Player.Source = new Uri(rndfile.FullName, UriKind.Absolute);
                Player.Stop();
                Player.Play();
            }
            if (c == 100) ShowMessage("嘻嘻~我和你说说话");
            if (c == 110) ShowMessage("其实后面还有哦！！");
            if (c == 120) ShowMessage("其实，我对你…………");
            if (c == 130) ShowMessage("非常讨厌！！！(ノ｀Д)ノ");
            if (c == 140) ShowMessage("不过看你真的很无聊！我就给你点惊喜吧！");
            if (c == 150) ShowMessage("看招！！！！！！");
            if (c == 160) ShowMessage("嘿！！！！开始啦！！！");
            if (c == 161)
            {
                var wnd = new EggWindow();
                wnd.Show();
                wnd.MousePierce();
            }
        }

        public void ChangeBgImage()
        {
            var dir = new DirectoryInfo($"{App.ExeDir}\\Assets\\Bg");
            var files = dir.GetFiles();
            var rnd = new Random();
            var rndfile = files[rnd.Next(0, files.Length)];
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
                Extend.Log(e);
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
            var tb = new TextBlock();
            tb.Text = obj;
            LogListBox.Items.Add(tb);
            LogListBox.ScrollIntoView(tb);
            if (LogListBox.Items.Count > 500) LogListBox.Items.RemoveAt(0);
        }

        private void ShowMessage(string mes, string detailMes =null,Extend.MessagePos pos = Extend.MessagePos.Popup)
        {
            switch (pos)
            {
                case Extend.MessagePos.Popup:
                    PopupMessageTextBlock.Text = mes;
                    this.Sb("PopupMessageShowSb").Begin();
                    break;
                case Extend.MessagePos.InfoBar:
                    StatusTextBlock.Text = mes;
                    this.Sb("InfoBarEmphasisSb").Begin();
                    break;
                case Extend.MessagePos.Window:
                    MessageWindow.Show(mes, detailMes, this);
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
            if (DownloaderMenuCheckBox.IsChecked == false) DownloaderMenuCheckBox.IsChecked = true;
            foreach (var ctrl in MoeExplorer.SelectedImageControls)
            {
                if (ctrl.ImageItem.DownloadUrlInfo == null) continue;
                MoeDownloaderControl.Downloader.AddDownload(ctrl.ImageItem, ctrl.PreviewImage.Source);
            }

            foreach (ImageControl ct in MoeExplorer.ImageItemsWrapPanel.Children)
            {
                ct.ImageCheckBox.IsChecked = false;
            }

            var lb = MoeDownloaderControl.DownloadItemsListBox;
            lb.ScrollIntoView(lb.Items[lb.Items.Count - 1]);
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
            var ctrl = lb.Items[lb.Items.Count - 1];
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
                    if (CurrentSearch?.LoadedPages?.Count > 0) NextPageButtonOnClick(this, null);
                    break;
                case Key.F1: // 用于测试功能
                    ShowMessage("test");
                    break;
                case Key.F8:
                    if (!Settings.HaveEnteredXMode)
                    {
                        _f8KeyDownTimes++;
                        if (_f8KeyDownTimes <= 3) break;
                        if (_f8KeyDownTimes > 3 && _f8KeyDownTimes < 10)
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
            para.CurrentSearch = CurrentSearch;
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
                if (Settings.IsClearImgsWhenSerachNextPage) MoeExplorer.ResetVisual();
                MoeExplorer.AddPage(CurrentSearch);
            }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            Settings.Save(App.SettingJsonFilePath);
            if (!MoeDownloaderControl.Downloader.IsDownloading) return;
            var result = MessageBox.Show(this, "正在下载图片，确定要关闭吗？", App.DisplayName, MessageBoxButton.OKCancel, MessageBoxImage.Question);
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
