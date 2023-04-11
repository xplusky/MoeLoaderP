using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MoeLoaderP.Wpf.ControlParts;

/// <summary>
/// 图片列表浏览器
/// </summary>
public partial class MoeExplorerControl
{
    public PreviewWindow PreviewWindowInstance { get; set; }
    public Settings Settings { get; set; }
    public event Action<MoeItem, ImageSource> ImageItemDownloadButtonClicked;
    public event Action<MoeItem, ImageSource> MoeItemPreviewButtonClicked; 

    public MoeItemControl MouseOnImageControl { get; set; }
    public ObservableCollection<MoeItemControl> SelectedImageControls { get; set; } = new();
    public Storyboard SearchStartSb => this.Sb("SearchStartSb");
    public Storyboard SearchingSb => this.Sb("SearchingSb");
    public Storyboard ShowSb => this.Sb("ShowSb");
    public MoeExplorerControl()
    {
        InitializeComponent();            
    }

    public void Init(Settings settings)
    {
        Settings = settings;
        KeyDown += OnKeyDown;
        ImageItemsScrollViewer.MouseRightButtonUp += ImageItemsScrollViewerOnMouseRightButtonUp;
        
        MoeItemPreviewButtonClicked += OnMoeItemPreviewButtonClicked;
        DownloadSelectedImagesButton.Click += DownloadSelectedImagesButtonOnClick;
        ImageItemDownloadButtonClicked += OnImageItemDownloadButtonClicked;
        
        MoeContextMenu.InitContextMenu(ImageItemsWrapPanel,ContextMenuPopup,SelectedImageControls); 
        MoeContextMenu.SearchByAuthorIdAction += SearchByAuthorId;
        InitPaging();

        new ChooseBoxHelper().InitSelectBox(null, ChooseBox, ChooseCanvasRoot, ImageItemsScrollViewer, ImageItemsWrapPanel);

        OutputSelectedImagesUrlsButton.Click += OutputSelectedImagesUrlsButtonOnClick;
        SelectedImageControls.CollectionChanged += SelectedImageControlsOnCollectionChanged;

        Ex.ShowMessageAction += ShowMessageAction;
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        DownloadTypeComboBox.SelectionChanged += DownloadTypeComboBoxOnSelectionChanged;
        
        DownloadOperationGrid.Visibility = Visibility.Collapsed;
        ImageLoadingPool.CollectionChanged += ImageLoadingPoolOnCollectionChanged;
    }

    private void ImageLoadingPoolOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (Debugger.IsAttached)
        {
            Ex.Log($"ImageLoadingPoolCount:{ImageLoadingPool.Count}");
        }
        var loadcount = ImageLoadingPool.Count(img => img.LoadingState == MoeItemControl.LoadingStateEnum.Loading);
        if (loadcount >= Settings.MaxOnLoadingImageCount) return;
        foreach (var image in ImageLoadingPool)
        {
            if (image.LoadingState != MoeItemControl.LoadingStateEnum.Waiting) continue;
            if (loadcount >= Settings.MaxOnLoadingImageCount) return;
            _ = image.TryLoad();
            loadcount++;
        }
    }

    public void DownloadTypeComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Settings.CurrentSession.CurrentDownloadType = DownloadTypeComboBox.SelectedItem as DownloadType;
        Settings.CurrentSession.OnPropertyChanged(nameof(SearchSession.CurrentDownloadType));
    }

    private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.CurrentSession))
        {
            InitSession(Settings.CurrentSession);
            ResetVisualPageDisplay();
        }
    }

    public void InitSession(SearchSession session)
    {
        PagingStackPanel.Children.Clear();
        Settings.CurrentSession = session;
        session.VisualPages.AddEvent += AddPageButton;
    }
    
    public string GetCountString(int? i)
    {
        if (i == null) return "?";
        return i.ToString();
    }

    private void OutputSelectedImagesUrlsButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow is not MainWindow mw) return;
        foreach (var ctrl in SelectedImageControls)
        {
            var url = ctrl.MoeItem.DownloadUrlInfo;
            if (ctrl.MoeItem.ChildrenItems.Count > 0)
            {
                foreach (var item in ctrl.MoeItem.ChildrenItems)
                {
                    mw.CollectTextBox.Text += item.DownloadUrlInfo?.Url + Environment.NewLine;
                }
            }
            else mw.CollectTextBox.Text += url?.Url + Environment.NewLine;
        }

        foreach (MoeItemControl ct in ImageItemsWrapPanel.Children)
        {
            ct.ImageCheckBox.IsChecked = false;
        }

        Ex.ShowMessage("已添加至收集箱");
    }

    private void OnImageItemDownloadButtonClicked(MoeItem item, ImageSource imgSource)
    {
        if(Application.Current.MainWindow is not MainWindow mw) return;
        mw.MoeDownloaderControl.Downloader.AddDownload(item, imgSource);
        var lb = mw.MoeDownloaderControl.DownloadItemsListBox;
        var ctrl = lb.Items[^1];
        lb.ScrollIntoView(ctrl);
        if (mw.DownloaderMenuCheckBox.IsChecked != false) return;
        mw.DownloaderMenuCheckBox.IsChecked = true;
    }

    private void SearchByAuthorId(MoeSite site, string arg2)
    {
        if (Application.Current.MainWindow is not MainWindow mw ) return;
            
        Settings.SiteManager.CurrentSelectedSite = site;
        mw.SearchControl.MoeSitesLv2ComboBox.SelectedIndex = 1;
        mw.SearchControl.KeywordTextBox.Text = arg2;
        mw.SearchControl.SearchButtonOnClick(null, null);
    }

    private void DownloadSelectedImagesButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow is not MainWindow mw) return;
        var count = 0;
        foreach (var ctrl in SelectedImageControls)
        {
            var img = ctrl.MoeItem;
            if (img.DownloadUrlInfo?.Url == null) continue;
            mw.MoeDownloaderControl.Downloader.AddDownload(img, ctrl.PreviewImage.Source);
            count++;
        }
        if (mw.DownloaderMenuCheckBox.IsChecked == false && count > 0) mw.DownloaderMenuCheckBox.IsChecked = true;
            
        foreach (MoeItemControl ct in ImageItemsWrapPanel.Children)
        {
            ct.ImageCheckBox.IsChecked = false;
        }

        var lb = mw.MoeDownloaderControl.DownloadItemsListBox;
        if (lb.Items.Count != 0)
        {
            lb.ScrollIntoView(lb.Items[^1]);
        }
    }

    private void OnMoeItemPreviewButtonClicked(MoeItem item, ImageSource imgSource)
    {
        PreviewWindow.Show(PreviewWindowInstance, Application.Current.MainWindow, item, imgSource);
    }
    
        
    private void ShowMessageAction(string arg1, string arg2, Ex.MessagePos arg3, bool highlight)
    {
        switch (arg3)
        {
            case Ex.MessagePos.Searching:
                SearchingMessageTextBlock.Text = arg1;
                break;
            case Ex.MessagePos.Page:
                PageMessageTextBlock.Text = arg1;
                break;
        }
    }


    private void SelectedImageControlsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        this.GoState(SelectedImageControls.Count == 0 ? nameof(NoSelectedItemState) : nameof(HasSelectedItemState));
        ImageCountTextBlock.Text = $"已选择{SelectedImageControls.Count}张（组）图片";
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.A && Keyboard.IsKeyDown(Key.LeftCtrl))
        {
            MoeContextMenu.ContextSelectAllButtonOnClick(null, null);
        }
    }
    private void ImageItemsScrollViewerOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        MoeContextMenu.SpPanel.Children.Clear();
        MoeContextMenu.ContextMenuImageInfoStackPanel.Children.Clear();
        if (ImageItemsWrapPanel.Children.Count != 0) ContextMenuPopup.IsOpen = true;
    }


    private void ItemCtrlOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        ContextMenuPopup.IsOpen = true;
        if (sender is not MoeItemControl ctrl) return;
        MoeContextMenu.LoadExtFunc(ctrl.MoeItem);
        MoeContextMenu.LoadImgInfo(ctrl.MoeItem);
        e.Handled = true;
    }


    public void ResetVisualPageDisplay()
    {
        foreach (MoeItemControl ctrl in ImageItemsWrapPanel.Children)
        {
            ctrl.Dispose();
        }
        ImageItemsWrapPanel.Children.Clear();
        ImageLoadingPool.Clear();
        SelectedImageControls.Clear();
        ImageItemsScrollViewer.ScrollToTop();
        GC.Collect();
    }


    public void SearchStartedVisual()
    {
        SearchStartSb.Begin();
        SearchingSb.Begin();

        this.GoState(nameof(ShowSearchingMessageState));
    }

    public async void SearchStopVisual()
    {

        ShowSb.Begin();
        await Task.Delay(TimeSpan.FromSeconds(2));
        SearchingSb.Stop();
        this.GoState(nameof(HideSearchingMessageState));
    }


    public ObservableCollection<MoeItemControl> ImageLoadingPool { get; set; } = new();
    

    public async Task ShowVisualPage(SearchedVisualPage page)
    {
        if(DownloadOperationGrid.Visibility == Visibility.Collapsed) DownloadOperationGrid.Visibility = Visibility.Visible;
        NextButtonControl.Visibility = Visibility.Visible;
        CurrentDisplayIndex = page.VisualIndex;
        if (PagingStackPanel.Children[page.VisualIndex - 1] is PagingButtonControl button)
        {
            button.VisualPage.IsCurrentPage = true;
        }
        page.LoadStart();
        ResetVisualPageDisplay();
        foreach (var rp in page.RealPages)
        {
            foreach (var img in rp)
            {
                void DisplayImg()
                {
                    if(img.IsLocalFilter) return;
                    var ctrl = new MoeItemControl(Settings, img);
                    ctrl.DownloadButton.Click += delegate { ImageItemDownloadButtonClicked?.Invoke(ctrl.MoeItem, ctrl.PreviewImage.Source); };
                    ctrl.PreviewButton.Click += delegate { MoeItemPreviewButtonClicked?.Invoke(ctrl.MoeItem, ctrl.PreviewImage.Source); };
                    ctrl.MouseEnter += delegate { MouseOnImageControl = ctrl; };
                    ctrl.ImageCheckBox.Checked += delegate { SelectedImageControls.Add(ctrl); };
                    ctrl.ImageCheckBox.Unchecked += delegate { SelectedImageControls.Remove(ctrl); };
                    ctrl.MouseRightButtonUp += ItemCtrlOnMouseRightButtonUp;
                    ImageItemsWrapPanel.Children.Add(ctrl);
                    ctrl.Show();
                    ImageLoadingPool.Add(ctrl);
                    ctrl.ImageLoadingStateChangedEvent += control =>
                    {
                        if (control.LoadingState == MoeItemControl.LoadingStateEnum.Loaded) ImageLoadingPool.Remove(control);
                    };
                }

                await ImageItemsWrapPanel.Dispatcher.BeginInvoke(DispatcherPriority.Background, DisplayImg);
            }
                
        }
            
        page.LoadEnd();
    }
    

    #region 页码相关
    public int CurrentDisplayIndex { get; set; }
    public PageButtonTipDataList DataGridTipSource { get; set; } = new();
    private void InitPaging()
    {
        NextButtonControl.PageButton.Click += NextPageButtonOnClick;
        NextButtonControl.SetNextPageButton();
        NextButtonControl.Visibility = Visibility.Collapsed;
        PageTipDataGrid.ItemsSource = DataGridTipSource;
        this.GoState(nameof(NoNextPageState), nameof(NoSelectedItemState), nameof(HideSearchingMessageState));
    }

    private async void NextPageButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (Settings.CurrentSession.VisualPages.LastOrDefault()?.IsSearchComplete == true)
        {
            return;
        }
        foreach (PagingButtonControl control in PagingStackPanel.Children)
        {
            control.VisualPage.IsCurrentPage = false;
        }
        var vp = await Settings.CurrentSession.SearchNextVisualPage();
        var offset = PagingStackPanel.Children[^1].TranslatePoint(new Point(0, 0), PagingStackPanel).X;
        PagingScrollViewer.ScrollToHorizontalOffset(offset);
        _ = ShowVisualPage(vp);
    }

    public void AddPageButton(SearchedVisualPage page)
    {
        var button = new PagingButtonControl();
        button.Init(page, 64, page.FirstRealPageIndex);
        button.PageButton.Click += delegate
        {
            foreach (PagingButtonControl c in PagingStackPanel.Children)
            {
                c.VisualPage.IsCurrentPage = false;
            }
            if (button.VisualPage.VisualIndex == CurrentDisplayIndex) return;
            _ = ShowVisualPage(button.VisualPage);
        };
        button.MouseEnter += delegate
        {
            PageTipPopup.IsOpen = true;
            foreach (var p in button.VisualPage.RealPages)
            {
                var tb = new TextBlock
                {
                    Foreground = Brushes.Black,
                    Text = p.GetTipString()
                };
                PageTipRootStackPanel.Children.Add(tb);
                var idmax = 0;
                var idmin = 0;
                if (p.Count > 1)
                {
                    idmax = p.Max(img => img.Id);
                    idmin = p.Min(img => img.Id);
                }
                DataGridTipSource.Add(new PageButtonTipData
                {
                    CurrentPageNum = $"{GetCountString(p.CurrentPageNum)}/{GetCountString(p.TotalPageCount)}",
                    CurrentPagePicCount = $"{GetCountString(p.CurrentPageItemsOutputCount)}/{GetCountString(p.CurrentPageItemsOriginCount)}",
                    CurrentPagePicNumRange = $"{GetCountString(p.CurrentPageItemsStartNum)}~{GetCountString(p.CurrentPageItemsEndNum)}",
                    CurrentPagePicIdRange = $"{idmin}~{idmax}",
                });
            }

        };
        button.MouseLeave += delegate
        {
            PageTipPopup.IsOpen = false;
            PageTipRootStackPanel.Children.Clear();
            DataGridTipSource.Clear();
        };

        PagingStackPanel.Children.Add(button);
    }

    #endregion
    
}