using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;


namespace MoeLoaderP.Wpf.ControlParts
{
    /// <summary>
    /// 缩略图面板中的图片用户控件
    /// </summary>
    public partial class MoeItemControl : IDisposable
    {
        public MoeItem MoeItem { get; set; }

        public Settings Settings { get; set; }

        public event Action<MoeItemControl> ImageLoadEnd;

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
            MoeItemOnPropertyChanged(this, new PropertyChangedEventArgs(nameof(MoeItem.IsFav)));
            SiteOnPropertyChanged(this, new PropertyChangedEventArgs(nameof(MoeSite.IsUserLogin)));
        }

        private void MoeItemOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MoeItem.IsFav))
            {
                FavTextBlock.Foreground = MoeItem.IsFav ? Brushes.DeepPink : Brushes.White;
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
        
        public async Task TryLoad()
        {
            var low = Settings.IsLowPerformanceMode;
            DetailAndImgLoadCts?.Cancel();
            DetailAndImgLoadCts = new CancellationTokenSource();
            var loadingsb = this.Sb("LoadingSb");
            loadingsb.Begin();
            this.Sb("LoadingStartSb").Begin();
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
                if (low)
                {
                    this.Sb("LoadedImageSb").Begin();
                    this.Sb("LoadedImageSb").SkipToFill();
                }
                else
                {
                    this.Sb("LoadedImageSb").Begin();
                }
                
            }
            
            else
            {
                this.Sb("LoadFailSb").Begin();
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
            
            var loadedsb = this.Sb("LoadedAllSb");
            loadedsb.Completed += delegate { loadingsb.Stop(); };
            loadedsb.Begin();
            ImageLoadEnd?.Invoke(this);
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
            var url = MoeItem.ThumbnailUrlInfo.Url;
            if (stream == null) return false;
            BitmapImage GetBitmapFunc()
            {
                return UiFunc.GetBitmapImageFromStream(stream);
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
}