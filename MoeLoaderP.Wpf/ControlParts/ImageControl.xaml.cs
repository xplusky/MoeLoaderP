using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf.ControlParts
{
    /// <summary>
    /// 缩略图面板中的图片用户控件
    /// </summary>
    public partial class ImageControl
    {
        public MoeItem ImageItem { get; set; }
        public Settings Settings { get; set; }

        public event Action<ImageControl> ImageLoadEnd;

        public ImageControl(Settings settings , MoeItem item)
        {
            Settings = settings;
            ImageItem = item;
            DataContext = this;

            InitializeComponent();

            MouseEnter += (sender, args) => VisualStateManager.GoToState(this, nameof(MouseOverState), true);
            MouseLeave += (sender, args) => VisualStateManager.GoToState(this, nameof(NormalState), true);
            ResolutionBorder.Visibility = item.Site.SupportState.IsSupportResolution ? Visibility.Visible : Visibility.Collapsed;
            DetailPageLinkButton.Click += (sender, args) => ImageItem.DetailUrl.GoUrl();
            RefreshButton.Click += RefreshButtonOnClick;
            ImageCheckBox.Click += ImageCheckBoxOnClick;
        }

        private void ImageCheckBoxOnClick(object sender, RoutedEventArgs e)
        {
            if(!Keyboard.IsKeyDown(Key.LeftAlt))return;
            MessageWindow.Debug("原始内容",ImageItem.OriginString);
        }

        private async void RefreshButtonOnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadImageAndDetailTask();
            }
            catch (Exception ex)
            {
                Extend.Log($"刷新错误：{ex.Message}");
            }
        }

        public async Task LoadImageAndDetailTask()
        {
            var loadingsb = this.Sb("LoadingSb");
            loadingsb.Begin();
            this.Sb("LoadingStartSb").Begin();

            // 同时进行图片加载和详情页分析
            var imgTask = LoadImageAsync();
            Task detailTask = null;
            if (ImageItem.GetDetailTaskFunc != null)
            {
                detailTask = ImageItem.GetDetailTaskFunc();
            }

            // 等待图片加载和详情页分析完成
            var e = await imgTask;
            if (detailTask != null) await detailTask;

            var loadedsb = this.Sb("LoadedAllSb");
            loadedsb.Completed += (sender, args) =>
            {
                loadingsb.Stop();
            };
            loadedsb.Begin();

            // Load end
            if(e == null) RefreshButton.Visibility = Visibility.Collapsed;
            ImageLoadEnd?.Invoke(this);
        }

        /// <summary>
        /// 异步加载图片
        /// </summary>
        public async Task<Exception> LoadImageAsync()
        {
            // client
            var net = ImageItem.Net ?? new NetDocker(Settings);
            net.SetTimeOut(15);
            net.SetReferer(ImageItem.ThumbnailUrlInfo.Referer);
            Exception loadEx = null;
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var response = await net.Client.GetAsync(ImageItem.ThumbnailUrlInfo.Url, cts.Token);
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var source = await Task.Run(() =>
                    {
                        try
                        {
                            var bitimg = new BitmapImage();
                            bitimg.CacheOption = BitmapCacheOption.OnLoad;
                            bitimg.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                            bitimg.BeginInit();
                            bitimg.StreamSource = stream;
                            bitimg.EndInit();
                            bitimg.Freeze();
                            return bitimg;
                        }
                        catch (IOException)
                        {
                            try
                            {
                                var bitmap = new Bitmap(stream);
                                var ms = new MemoryStream();
                                bitmap.Save(ms, ImageFormat.Png);
                                var bitimg = new BitmapImage();
                                bitimg.CacheOption = BitmapCacheOption.OnLoad;
                                bitimg.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                                bitimg.BeginInit();
                                bitimg.StreamSource = ms;
                                bitimg.EndInit();
                                bitimg.Freeze();
                                //ms.Dispose();
                                return bitimg;
                            }
                            catch (Exception e)
                            {
                                loadEx = e;
                                return null;
                            }
                        }
                        catch (Exception ex)
                        {
                            loadEx = ex;
                            return null;
                        }
                    }, cts.Token);

                    if (source != null) PreviewImage.Source = source;
                }
            }
            catch (Exception ex)
            {
                loadEx = ex;
            }

            if (loadEx == null)
            {
                
                this.Sb("LoadedImageSb").Begin();
            }
            else
            {
                this.Sb("LoadFailSb").Begin();
                Extend.Log(loadEx.Message,loadEx.StackTrace);
                Extend.Log($"{ImageItem.ThumbnailUrlInfo.Url} 图片加载失败");
            }

            return loadEx;
        }
    }
}