using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MoeLoaderP.Core;
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
        }


        private void ImageCheckBoxOnClick(object sender, RoutedEventArgs e)
        {
            var wnd = Application.Current.MainWindow;
            if(!Keyboard.IsKeyDown(Key.LeftAlt))return;
            MessageWindow.ShowDialog("原始内容",MoeItem.OriginString,wnd,true);
        }

        private async void RefreshButtonOnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadImageAndDetailTask();
            }
            catch (Exception ex)
            {
                Ex.Log($"刷新错误：{ex.Message}");
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
            if (MoeItem.GetDetailTaskFunc != null)
            {
                detailTask = MoeItem.TryGetDetail();
            }

            // 等待图片加载和详情页分析完成
            var e = await imgTask;
            if (detailTask != null) await detailTask;

            var loadedsb = this.Sb("LoadedAllSb");
            loadedsb.Completed += delegate { loadingsb.Stop(); };
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
            var b = MoeItem.Net == null;
            var net = b ? new NetOperator(Settings) : MoeItem.Net.CreateNewWithOldCookie();
            net.SetTimeOut(15);
            net.SetReferer(MoeItem.ThumbnailUrlInfo.Referer);
            Exception loadEx = null;
            try
            {

                var url = MoeItem.ThumbnailUrlInfo.Url;
                //var ext = Path.GetExtension(url);
                //if (ext.Equals(".gif", StringComparison.OrdinalIgnoreCase) && OperatingSystem.IsWindowsVersionAtLeast(7))
                //{
                //    AnimationBehavior.SetSourceUri(PreviewImage, new Uri(url));
                //    ImageBgBorder.Background = Brushes.WhiteSmoke;
                //}
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var response = await net.Client.GetAsync(url, cts.Token);
                await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
                var source = await Task.Run(() =>
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
                            loadEx = ex;
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
                Ex.Log(loadEx.Message,loadEx.StackTrace);
                Ex.Log($"{MoeItem.ThumbnailUrlInfo.Url} 图片加载失败");
            }

            return loadEx;
        }
    }
}