using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;


namespace MoeLoaderP.Wpf.ControlParts;

/// <summary>
/// 缩略图面板中的图片用户控件
/// </summary>
public partial class MoeItemControl : IDisposable
{
    private LoadingStateEnum _loadingState = LoadingStateEnum.Waiting;

    public enum LoadingStateEnum
    {
        Waiting, Loading,  Loaded
    }

    public LoadingStateEnum LoadingState
    {
        get => _loadingState;
        set
        {
            if(value == _loadingState) return;
            _loadingState = value;
            ImageLoadingStateChangedEvent?.Invoke(this);
        }
    }

    public MoeItem MoeItem { get; set; }

    public Settings Settings { get; set; }

    public event Action<MoeItemControl> ImageLoadingStateChangedEvent;

    public MoeItemControl(Settings settings, MoeItem item)
    {
        Settings = settings;
        MoeItem = item;
        DataContext = this;

        InitializeComponent();

        MouseEnter += delegate { VisualStateManager.GoToState(this, nameof(MouseOverState), true); };
        MouseLeave += delegate { VisualStateManager.GoToState(this, nameof(NormalState), true); };
        DetailPageLinkButton.Click += delegate { MoeItem.DetailUrl.GoUrl(); };
        RefreshButton.Click += RefreshButtonOnClick;
        ImageCheckBox.Click += ImageCheckBoxOnClick;
        StarButton.Click += StarButtonOnClick;
        MoeItem.PropertyChanged += MoeItemOnPropertyChanged;
        MoeItem.Site.PropertyChanged += SiteOnPropertyChanged;
        InitVisual();
    }
        
    private void SiteOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MoeSite.IsUserLogin))
        {
            if (MoeItem.Site.IsUserLogin)
            {
                StarButton.Visibility = MoeItem.Site.Config.IsSupportStarButton ? Visibility.Visible : Visibility.Collapsed;
                ThumbButton.Visibility = MoeItem.Site.Config.IsSupportThumbButton ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                StarButton.Visibility = Visibility.Collapsed;
                ThumbButton.Visibility = Visibility.Collapsed;
            }
                
        }
    }

    public void InitVisual()
    {
        OperationBorder.Opacity = 0;
        MoeItemOnPropertyChanged(this, new PropertyChangedEventArgs(nameof(MoeItem.IsFav)));
        SiteOnPropertyChanged(this, new PropertyChangedEventArgs(nameof(MoeSite.IsUserLogin)));
        if (MoeItem.ChildrenItemsCount > 1)
        {
            SetMultiPicVisual();
        }
    }

    public void SetMultiPicVisual()
    {
        ImageCheckBox.Margin = new Thickness(0, 0, 8, 8);
        MultiPicBgGrid.Visibility = Visibility.Visible;
    }

    private void MoeItemOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MoeItem.IsFav))
        {
            FavTextBlock.Foreground = MoeItem.IsFav ? Brushes.DeepPink : Brushes.White;
        }
        if(e.PropertyName ==  nameof(MoeItem.ChildrenItemsCount))
        {
            if (MoeItem.ChildrenItemsCount > 1)
            {
                SetMultiPicVisual();
            }
        }
    }

    private async void StarButtonOnClick(object sender, RoutedEventArgs e)
    {
        var b = await MoeItem.Site.StarAsync(MoeItem, default);
        if (b)
        {
            FavTextBlock.Foreground = Brushes.DeepPink;
            Ex.ShowMessage("收藏成功");
        }
        else
        {
            Ex.ShowMessage("收藏失败");
        }
    }


    private void ImageCheckBoxOnClick(object sender, RoutedEventArgs e)
    {
        var wnd = Application.Current.MainWindow;
        if(!Keyboard.IsKeyDown(Key.LeftAlt))return;
        MessageWindow.ShowDialog("原始内容",MoeItem.OriginString,wnd,true);
    }

    private async void RefreshButtonOnClick(object sender, RoutedEventArgs e)
    {
        await TryLoad();
    }
        

    public CancellationTokenSource DetailAndImgLoadCts { get; set; }

    public bool LowPerformanceMode => Settings.IsLowPerformanceMode;

    public void Show()
    {
        if(!LowPerformanceMode) ShowSb.Begin();
    }

    public Storyboard LoadingSb => this.Sb("LoadingSb");
    public Storyboard ShowSb => this.Sb("ShowSb");
    public Storyboard LoadingStartSb => this.Sb("LoadingStartSb");
    public Storyboard LoadedImageSb => this.Sb("LoadedImageSb");
    public Storyboard LoadFailSb => this.Sb("LoadFailSb");
    public Storyboard LoadedAllSb => this.Sb("LoadedAllSb");

    public async Task TryLoad()
    {
        LoadingState = LoadingStateEnum.Loading;
        DetailAndImgLoadCts?.Cancel();
        DetailAndImgLoadCts = new CancellationTokenSource();
        LoadingSb.Begin();
        if(LowPerformanceMode) LoadingSb.Pause();
        LoadingStartSb.Begin();
        if (LowPerformanceMode) LoadingStartSb.SkipToFill();
        var imgTask = LoadDisplayImageAsync(DetailAndImgLoadCts.Token);
        var detailTask = LoadDetailTask(DetailAndImgLoadCts.Token);
            
        bool imgB = false, detailB = false;

        try
        {
            imgB = await imgTask;
        }
        catch
        {
            // ignored
        }

        if (imgB)
        {
            LoadedImageSb.Begin();
            if (LowPerformanceMode) LoadedImageSb.SkipToFill();
        }
            
        else
        {
            LoadFailSb.Begin();
            if (LowPerformanceMode) LoadFailSb.SkipToFill();
            RefreshButton.Visibility = Visibility.Visible;
        }
        try
        {
            detailB = await detailTask;
        }
        catch
        {
            // ignored
        }

        if (detailB) MoeItem.CanDownload = true;
        else RefreshButton.Visibility = Visibility.Visible;

        if (imgB && detailB)
        {
            RefreshButton.Visibility = Visibility.Collapsed;
        }

        LoadedAllSb.Completed += delegate { LoadingSb.Stop(); };
        LoadedAllSb.Begin();
        if(LowPerformanceMode) LoadedAllSb.SkipToFill();
        LoadingState = LoadingStateEnum.Loaded;
    }

    public async Task<bool> LoadDetailTask(CancellationToken token)
    {
        try
        {
            await MoeItem.TryGetDetail(token);
            return true;
        }
        catch (Exception e)
        {
            Ex.Log($"{MoeItem.ThumbnailUrlInfo.Url} LoadDetailTask fail : {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 异步加载图片
    /// </summary>
    public async Task<bool> LoadDisplayImageAsync(CancellationToken token)
    {
        await using var stream = await MoeItem.TryLoadThumbnailStreamAsync(token);
        if (stream == null) return false;
        BitmapImage GetBitmapFunc()
        {
            return UiUtility.GetBitmapImageFromStream(stream);
        }

        var bitimg = await Task.Run(GetBitmapFunc, token);
        if (bitimg == null) return false;
        PreviewImage.Source = bitimg;
        if (!Settings.IsLowPerformanceMode)
        {
            ImageBgBorder.Background = new ImageBrush(bitimg) { Stretch = Stretch.UniformToFill };
        }

        return true;
    }
        

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
        
}