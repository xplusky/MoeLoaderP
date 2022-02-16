using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MoeLoaderP.Core;
using MoeLoaderP.Helper;
using MoeLoaderP.Wpf.ControlParts;

namespace MoeLoaderP.Wpf;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private Settings Settings { get; set; }

    public EggWindow EggWindowInst { get; set; }
    public LogWindow LogWindowInst { get; set; }

    private int LogoClickCount { get; set; }

    public void Init(Settings settings)
    {
        Settings = settings;
        Settings.CustomSitesDir = App.CustomSiteDir;
        Settings.SiteManager = new SiteManager(Settings);
        DataContext = Settings;
            
        Closing += OnClosing;
        PreviewKeyDown += OnPreviewKeyDown;
        MouseLeftButtonDown += delegate { DragMove(); };
        Ex.ShowMessageAction += ShowMessage;

        // logo
        LogoImageButton.MouseRightButtonDown += LogoImageButtonOnMouseRightButtonDown;

        // menu
        DownloaderMenuCheckBox.Checked += DownloaderMenuCheckBoxCheckChanged;
        DownloaderMenuCheckBox.Unchecked += DownloaderMenuCheckBoxCheckChanged;
        
        // user ctrl
        AboutControl.Init();
        SearchControl.Init(Settings);
        MoeDownloaderControl.Init(Settings);
        MoeSettingsControl.Init(Settings);
        MoeExplorer.Init(Settings);

        // helper : collect ,log
        CollectCopyAllButton.Click += delegate { CollectTextBox.Text.CopyToClipboard(); };
        CollectClearButton.Click += delegate { CollectTextBox.Text = string.Empty; };
        LogButton.Click += delegate
        {
            if (LogWindowInst == null)
            {
                LogWindowInst = new LogWindow();
                LogWindowInst.Init(settings);
                LogWindowInst.Show();
                LogWindowInst.Closed += delegate { LogWindowInst = null; };
            }
            else
            {
                LogWindowInst.Activate();
            }
                
        };

        //LogListBox.MouseRightButtonUp += LogListBoxOnMouseRightButtonUp;
        ImageSizeSlider.MouseWheel += ImageSizeSliderOnMouseWheel;
        // egg
        LogoImageButton.Click += LogoImageButtonOnClick;

        // gen custom test 请删除后运行
        if (Debugger.IsAttached)
        {
            var cus = new CustomSiteFactory();
            cus.GenTestSites();
            cus.OutputJson(App.CustomSiteDir);
        }
    }

    private void LogoImageButtonOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Settings.IsCustomSiteMode = !Settings.IsCustomSiteMode;
        LayoutRoot.GoElementState(Settings.IsCustomSiteMode ? nameof(CustomSitesState) : nameof(DefaultSitesState));
        LogoImage.Visibility = Settings.IsCustomSiteMode ? Visibility.Collapsed : Visibility.Visible;
        LogoImage2.Visibility = Settings.IsCustomSiteMode ? Visibility.Visible : Visibility.Collapsed;
    }

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
            case > 90 and < 120 when LogoClickCount % 3 == 0:
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
                ShowMessage("嘿！！！！开始啦！！！看看我的无敌雪景！！你以后可以按F6来开启和关闭雪景啦");
                Settings.IsShowEggWindowOnce = true;
                ShowEggWindow();
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


    public void ShowEggWindow()
    {
        if (EggWindowInst is null)
        {
            EggWindowInst = new EggWindow();
            EggWindowInst.Show();
        }
        else
        {
            EggWindowInst.Activate();
        }
    }

    public void CloseEggWindow()
    {
        EggWindowInst?.Close();
        EggWindowInst = null;
    }

        

    private async void ShowMessage(string mes, string detailMes = null, Ex.MessagePos pos = Ex.MessagePos.Popup, bool ishighlight =false)
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
            case Ex.MessagePos.Searching:
                var mesctrl = new SearchMessageControl();
                mesctrl.Set(mes,ishighlight);
                SearchMessageStackPanel.Children.Add(mesctrl);
                mesctrl.ShowOneTime(6);
                await Task.Delay(TimeSpan.FromSeconds(7));
                SearchMessageStackPanel.Children.Remove(mesctrl);
                break;
        }
    }

    private void DownloaderMenuCheckBoxCheckChanged(object sender, RoutedEventArgs e)
    {
        var ischecked = DownloaderMenuCheckBox.IsChecked == true;
        LayoutRoot.GoElementState(ischecked ? nameof(ShowDownloadPanelState) : nameof(HideDownloadPanelState));
    }

    private void ImageSizeSliderOnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var v = ImageSizeSlider.Value;
        v += e.Delta / 5d;
        if (v > ImageSizeSlider.Minimum && v < ImageSizeSlider.Maximum) ImageSizeSlider.Value += e.Delta / 5d;
    }


    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F1: // 用于测试功能
                ShowMessage("test");
                break;
            case Key.F8:
                if (Settings.SiteManager.R18Check()) SearchControl.MoeSitesLv1ComboBox.SelectedIndex = 0;
                break;
            case Key.F7:
                if (Settings.IsShowEggWindowOnce) // Settings.IsShowEggWindowOnce
                {
                    if (EggWindowInst is null) ShowEggWindow();
                    else CloseEggWindow();
                }

                break;
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
}