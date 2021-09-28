using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;
using draw= System.Drawing;

namespace MoeLoaderP.Wpf.ControlParts
{
    /// <summary>
    /// 缩略图面板中的图片用户控件
    /// </summary>
    public partial class MoeItemControl
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
            DetailAndImgLoadCts?.Cancel();
            DetailAndImgLoadCts = new CancellationTokenSource();
            var loadingsb = this.Sb("LoadingSb");
            loadingsb.Begin();
            this.Sb("LoadingStartSb").Begin();
            Task<bool> imgTask = null;
            Task<bool> detailTask = null;
            imgTask = LoadDisplayImageAsync(DetailAndImgLoadCts.Token);
            detailTask = LoadDetailTask(DetailAndImgLoadCts.Token);

            bool imgB = false, detailB = false;
            try
            {
                imgB = await imgTask;
            }
            catch {}
            if (imgB) { this.Sb("LoadedImageSb").Begin(); }
            else
            {
                this.Sb("LoadFailSb").Begin();
                RefreshButton.Visibility = Visibility.Visible;
            }
            try
            {
                detailB = await detailTask;
            }
            catch {}
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
            var b = MoeItem.Net == null;
            var net = b ? new NetOperator(Settings) : MoeItem.Net.CreateNewWithOldCookie();
            net.SetTimeOut(20);
            net.SetReferer(MoeItem.ThumbnailUrlInfo.Referer);
            var url = MoeItem.ThumbnailUrlInfo.Url;
            var response = await net.Client.GetAsync(url, token);
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            var source = await Task.Run(delegate
            {
                try
                {
                    return UiFunc.SaveLoadBitmapImage(stream);
                }
                catch (IOException)
                {
                    try
                    {
                        var bitmap = new draw.Bitmap(stream);
                        var ms = new MemoryStream();
                        bitmap.Save(ms, draw.Imaging.ImageFormat.Png);
                        return UiFunc.SaveLoadBitmapImage(ms);
                    }
                    catch (Exception ex)
                    {
                        Ex.Log($"{url} Image Bitmap Load Fail", ex.Message);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Ex.Log($"{url} Image SaveLoadBitmapImage Load Fail",ex.Message);
                    return null;
                }
            }, token);

            if (source != null)
            {
                PreviewImage.Source = source;
                return true;
            }

            return false;
        }
    }
}