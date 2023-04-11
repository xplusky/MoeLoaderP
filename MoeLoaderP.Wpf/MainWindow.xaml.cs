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

    public Settings Settings { get; set; }
    
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
        //LogoImageButton.MouseRightButtonDown += LogoImageButtonOnMouseRightButtonDown;

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
        new LogWindowHelper().Init(LogButton, Settings);
        
        ImageSizeSlider.MouseWheel += ImageSizeSliderOnMouseWheel;

        // egg
        new EggWindowHelper().Init(this);

        // gen custom test 请删除后运行
        if (Debugger.IsAttached)
        {
            var cus = new CustomSiteFactory();
            cus.GenTestSites();
            cus.OutputJson(App.CustomSiteDir);
        }

        // ali
        this.SetWindowFluent(settings );

        Settings.SiteManager.PropertyChanged += SiteManagerOnPropertyChanged;
    }

    private void SiteManagerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.SiteManager.CurrentSelectedSite))
        {
            var isCsm = Settings.SiteManager.CurrentSelectedSite.Config?.IsCustomSite == true;
            if (Settings.IsCustomSiteMode == isCsm) return;

            Settings.IsCustomSiteMode = isCsm;
            LayoutRoot.GoElementState(isCsm ? nameof(CustomSitesState) : nameof(DefaultSitesState));
            LogoImage.Visibility = isCsm ? Visibility.Collapsed : Visibility.Visible;
            LogoImage2.Visibility = isCsm ? Visibility.Visible : Visibility.Collapsed;
        }
    }
    
    
    public  async void ShowMessage(string mes, string detailMes = null, Ex.MessagePos pos = Ex.MessagePos.Popup, bool ishighlight =false)
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