using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MoeLoader.Core;

namespace MoeLoader.UI
{
    /// <summary>
    /// 缩略图面板中的图片用户控件
    /// </summary>
    public partial class ImageControl
    {
        public ImageItem ImageItem { get; set; }
        public Settings Settings { get; set; }

        public event Action<ImageControl> ImageLoaded;

        public ImageControl(Settings settings , ImageItem item)
        {
            Settings = settings;
            ImageItem = item;
            DataContext = this;
            InitializeComponent();

            MouseEnter += OnMouseEnter;
            MouseLeave += (sender, args) => VisualStateManager.GoToState(this, nameof(NormalState), true);

            ScoreBorder.Visibility = item.Site.SurpportState.IsSupportScore ? Visibility.Visible : Visibility.Collapsed;
            ResolutionBorder.Visibility = item.Site.SurpportState.IsSupportResolution ? Visibility.Visible : Visibility.Collapsed;
            DetailPageLinkButton.Click += DetailPageLinkButtonOnClick;

        }

        private void DetailPageLinkButtonOnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(ImageItem.DetailUrl);
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            VisualStateManager.GoToState(this, nameof(MouseOverState), true);
        }

        public async Task LoadImageAsync()
        {
            var loadingsb = this.Sb("LoadingSb");
            var startloadingsb = this.Sb("LoadingStartSb");
            
            startloadingsb.Begin();
            loadingsb.Begin();

            // client
            var net = ImageItem.Net ?? new NetSwap(Settings);
            net.SetTimeOut(15);
            net.SetReferer(ImageItem.ThumbnailReferer);
            Exception loadex = null;
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                var getDetaiTask = ImageItem.GetDetailAsync();
                var response = await net.Client.GetAsync(ImageItem.ThumbnailUrl, cts.Token);
                var stream = await response.Content.ReadAsStreamAsync();
                var source = await Task.Run(() =>
                {
                    try
                    {
                        var bitm = new BitmapImage
                        {
                            CacheOption = BitmapCacheOption.OnLoad,
                            CreateOptions = BitmapCreateOptions.IgnoreImageCache
                        };
                        bitm.BeginInit();
                        bitm.StreamSource = stream;
                        bitm.EndInit();
                        bitm.Freeze();
                        return bitm;
                    }
                    catch (Exception ex)
                    {
                        App.Log(ex);
                        return null;
                    }
                }, cts.Token);
                if(source ==null) loadex = new Exception("imagesource is null");
                else PreviewImage.Source = source;
                await getDetaiTask;
            }
            catch (Exception ex)
            {
                loadex = ex;
            }

            if (loadex != null)
            {
                App.Log(loadex);
                var loadfsb = this.Sb("LoadFailSb");
                loadfsb.Completed += (sender, args) => loadingsb.Stop();
                loadfsb.Begin();
            }
            else
            {
                var loadsb = this.Sb("LoadedSb");
                loadsb.Completed += (sender, args) => loadingsb.Stop();
                loadsb.Begin();
            }

            // Loaded
            ImageLoaded?.Invoke(this);
        }
    }
}