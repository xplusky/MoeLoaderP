using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http.Handlers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf
{
    /// <summary>
    /// PreviewWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PreviewWindow
    {
        public MoeItem CurrentMoeItem { get; set; }
        public Settings Settings { get; set; }
        public BitmapImage PreviewBitmapImage { get; set; }
        private double ImageScale => (double)PreviewBitmapImage.PixelWidth / PreviewBitmapImage.PixelHeight;
        public CancellationTokenSource Cts { get; set; }
        private double PicWidth { get; set; }
        private double PicHeight { get; set; }
        
        public PreviewWindow()
        {
            InitializeComponent();
            MouseWheel += OnMouseWheel;
            LargeImageThumb.DragDelta += LargeImageThumbOnDragDelta;
            LargeImage.ClearValue(MarginProperty);
            MouseLeftButtonDown += (_, _) => DragMove();
        }

        public async void Init(MoeItem moeitem,ImageSource imgSource)
        {
            CurrentMoeItem = moeitem;
            Settings = moeitem.Site.Settings;
            
            var info = CurrentMoeItem.Urls.GetPreview();
            if(info == null )return;
            DisplayItemInfo();
            RootGrid.GoElementState(nameof(LoadingBarShowState));
            ImageLoadProgressBar.Value = 0;
            await LoadImageAsync();
            RootGrid.GoElementState(nameof(LoadingBarHideState));
            InitImagePosition();
        }

        public void DisplayItemInfo()
        {
            var i = CurrentMoeItem;
            InfoTitleTextBlock.Text = i.Title;
            InfoIDTextBlock.Text = $"{i.Id}";
            InfoUploaderTextBlock.Text = i.Uploader;
            InfoScoreTextBlock.Text = $"{i.Score}";
            InfoResolutionTextBlock.Text = $"{i.Width}x{i.Height}";
            InfoDateTextBlock.Text = i.DateString;
            TagsWrapPanel.Children.Clear();
            foreach (var iTag in i.Tags)
            {
                var tagTb = new TextBlock();
                tagTb.Text = iTag;
                tagTb.FontSize = 14;
                tagTb.Foreground = (SolidColorBrush)FindResource("HightLightFontColorBrush");
                tagTb.Margin = new Thickness(0,0,8,8);
                TagsWrapPanel.Children.Add(tagTb);
            }

        }

        private void LargeImageThumbOnDragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = (Thumb)sender;
            var nTop = Canvas.GetTop(thumb) + e.VerticalChange;
            var nLeft = Canvas.GetLeft(thumb) + e.HorizontalChange;

            // 防止Thumb控件被拖出容器。  
            if (LargeImage.Height <= ImageCanvas.ActualHeight)
            {
                if (nTop <= 0) nTop = 0;
                if (nTop >= ImageCanvas.ActualHeight - thumb.Height) nTop = ImageCanvas.ActualHeight - thumb.Height;
            }

            if (LargeImage.Width <= ImageCanvas.ActualWidth)
            {
                if (nLeft <= 0) nLeft = 0;
                if (nLeft >= ImageCanvas.ActualWidth - thumb.Width) nLeft = ImageCanvas.ActualWidth - thumb.Width;
            }

            Canvas.SetTop(thumb, nTop);
            Canvas.SetLeft(thumb, nLeft);
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if(!LargeImageThumb.IsMouseOver) return;
            
            var delta = e.Delta / 500d;
            var mousePosToImage = e.GetPosition(LargeImage);

            if (delta > 0 && LargeImage.Width > 2 * ImageCanvas.ActualWidth) return;
            if (delta < 0 && LargeImage.Width < ImageCanvas.ActualWidth / 4) return;

            LargeImage.Width *= 1d + delta;
            LargeImage.Height = LargeImage.Width / ImageScale;

            var movex = mousePosToImage.X;
            var movey = mousePosToImage.Y;

            // 判断鼠标在元素外
            if (movex > LargeImage.ActualWidth || movex < 0) movex = LargeImage.Width / 2;
            if (movey > LargeImage.ActualHeight || movey < 0) movey = LargeImage.Height / 2;

            //  图片不大于窗格时保证在窗格内  
            var nTop = Canvas.GetTop(LargeImageThumb) - delta * movey;
            var nLeft = Canvas.GetLeft(LargeImageThumb) - delta * movex;
            if (LargeImage.Height <= ImageCanvas.ActualHeight)
            {
                if (nTop <= 0) nTop = 0;
                if (nTop >= ImageCanvas.ActualHeight - LargeImage.Height) nTop = ImageCanvas.ActualHeight - LargeImage.Height;
            }

            if (LargeImage.Width <= ImageCanvas.ActualWidth)
            {
                if (nLeft <= 0) nLeft = 0;
                if (nLeft >= ImageCanvas.ActualWidth - LargeImage.Width) nLeft = ImageCanvas.ActualWidth - LargeImage.Width;
            }

            Canvas.SetLeft(LargeImageThumb, nLeft);
            Canvas.SetTop(LargeImageThumb, nTop);
        }

        public void InitImagePosition()
        {
            if (PreviewBitmapImage == null) return;
            // 设置原始宽高
            LargeImage.Width = PreviewBitmapImage.PixelWidth;
            LargeImage.Height = PreviewBitmapImage.PixelHeight;
            // 调整大小适合窗口
            if (LargeImage.Width > ImageCanvas.ActualWidth)
            {
                LargeImage.Width = ImageCanvas.ActualWidth;
                LargeImage.Height = LargeImage.Width / ImageScale;
            }
            if (LargeImage.Height > ImageCanvas.ActualHeight)
            {
                LargeImage.Height = ImageCanvas.ActualHeight;
                LargeImage.Width = LargeImage.Height * ImageScale;
            }
            // 位置居中
            Canvas.SetLeft(LargeImageThumb, ImageCanvas.ActualWidth / 2 - LargeImage.Width / 2);
            Canvas.SetTop(LargeImageThumb, ImageCanvas.ActualHeight / 2 - LargeImage.Height / 2);
        }

        public void SetImage(BitmapImage img)
        {
            var sb = LargeImage.FadeHideSb();
            sb.Completed += (_, _) =>
            {
                LargeImage.Source = img;
                LargeImage.EnlargeShowSb().Begin();
            };
            sb.Begin();
        }
        
        /// <summary>
        /// 异步加载图片
        /// </summary>
        public async Task<Exception> LoadImageAsync()
        {
            // client
            var net = CurrentMoeItem.Net ?? new NetOperator(Settings);
            //net.SetTimeOut(30);
            net.SetReferer(CurrentMoeItem.ThumbnailUrlInfo.Referer);
            net.ProgressMessageHandler.HttpReceiveProgress += ProgressMessageHandlerOnHttpReceiveProgress;
            Exception loadEx = null;
            try
            {
                Cts?.Cancel();
                Cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await net.Client.GetAsync(CurrentMoeItem.Urls.GetPreview().Url, Cts.Token);
                await using var stream = await response.Content.ReadAsStreamAsync();
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
                }, Cts.Token);

                if (source != null)
                {
                    PreviewBitmapImage = source;
                    SetImage(source);
                }
            }
            catch (Exception ex)
            {
                loadEx = ex;
            }

            if (loadEx == null)
            {

                //this.Sb("LoadedImageSb").Begin();
            }
            else
            {
                //this.Sb("LoadFailSb").Begin();
                Ex.Log(loadEx.Message, loadEx.StackTrace);
                Ex.Log($"{CurrentMoeItem.ThumbnailUrlInfo.Url} 图片加载失败");
            }

            return loadEx;
        }

        private void ProgressMessageHandlerOnHttpReceiveProgress(object sender, HttpProgressEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ImageLoadProgressBar.Value = e.ProgressPercentage;
            }));

        }
    }

    public class PreviewWindowPagingItem : BindingObject
    {
        public string Index { get; set; }
    }
}
