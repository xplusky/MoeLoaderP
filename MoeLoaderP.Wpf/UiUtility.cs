using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MoeLoaderP.Core;
using MoeLoaderP.Wpf.ControlParts;
using draw = System.Drawing;
using SourceChord.FluentWPF;

namespace MoeLoaderP.Wpf;

public static class UiUtility
{
    public static void SetWindowFluent(this Window window, Settings settings)
    {
        try
        {
            var disc = RuntimeInformation.OSDescription;
            if(!disc.Contains("Windows", StringComparison.OrdinalIgnoreCase))return;
            var ver = disc.Split(' ')[2];
            var version = Version.Parse(ver);
            if (version.Major < 10) return;
            AcrylicWindow.SetEnabled(window, true);
            AcrylicWindow.SetAcrylicWindowStyle(window, AcrylicWindowStyle.NoIcon);
            AcrylicWindow.SetAcrylicAccentState(window, settings.IsEnableAcrylicStyle ? AcrylicAccentState.Default : AcrylicAccentState.Disabled);
            settings.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(Settings.IsEnableAcrylicStyle))
                {
                    AcrylicWindow.SetAcrylicAccentState(window, settings.IsEnableAcrylicStyle ? AcrylicAccentState.Default : AcrylicAccentState.Disabled);
                }
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

    }
    public static string LangText(this FrameworkElement el, string key)
    {
        var text = el.TryFindResource(key) as string;
        return string.IsNullOrWhiteSpace(text) ? text : "{N/A}";
    }

    public static void AddEasyDoubleAnime(this Storyboard sb, DependencyObject target, double fromValue, double toValue, double timeSec, string property)
    {
        var path = "";
        switch (property)
        {
            case "opacity":
                path = "(UIElement.Opacity)";
                break;
            case "width":
                path = "(FrameworkElement.Width)";
                break;
            case "scale-x":
                path = "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)";
                break;
            case "scale-y":
                path = "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)";
                break;
            case "x":
                path = "(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.X)";
                break;
            case "y":
                path = "(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.Y)";
                break;
            case "margin":
                path = "(FrameworkElement.Margin)";
                break;
        }

        var el = (UIElement)target;
        if (el.RenderTransform == null)
        {
            var group = new TransformGroup {Children = {[3] = new TranslateTransform()}};
            el.RenderTransform = group;
        }


        var animation = new DoubleAnimationUsingKeyFrames
        {
            KeyFrames =
            {
                new EasingDoubleKeyFrame(fromValue, KeyTime.FromTimeSpan(TimeSpan.Zero)),
                new EasingDoubleKeyFrame(toValue, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(timeSec)))
                {
                    EasingFunction = new ExponentialEase {EasingMode = EasingMode.EaseOut}
                }
            },
        };
        Storyboard.SetTargetProperty(animation, new PropertyPath(path));
        Storyboard.SetTarget(animation, target);
        sb.Children.Add(animation);
    }

    public static void AddEasyThicknessAnime(this Storyboard sb, DependencyObject target, Thickness fromValue, Thickness toValue, double timeSec, string property)
    {
        var path = "";
        switch (property)
        {
            case "margin":
                path = "(FrameworkElement.Margin)";
                break;
                   
        }

        var el = (UIElement)target;
        if (el.RenderTransform == null)
        {
            var group = new TransformGroup { Children = { [3] = new TranslateTransform() } };
            el.RenderTransform = group;
        }


        var animation = new ThicknessAnimationUsingKeyFrames()
        {
            KeyFrames =
            {
                new EasingThicknessKeyFrame(fromValue, KeyTime.FromTimeSpan(TimeSpan.Zero)),
                new EasingThicknessKeyFrame(toValue, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(timeSec)))
                {
                    EasingFunction = new ExponentialEase {EasingMode = EasingMode.EaseOut}
                }
            },
        };
        Storyboard.SetTargetProperty(animation, new PropertyPath(path));
        Storyboard.SetTarget(animation, target);
        sb.Children.Add(animation);
    }

    /// <summary>
    /// 获取当前FrameworkElement的Storyboard资源
    /// </summary>
    public static Storyboard Sb(this FrameworkElement element, string key)
    {
        return (Storyboard)element.Resources[key];
    }

    /// <summary>
    /// 设置popup显示动画（右键菜单出现动画等）
    /// </summary>
    public static Storyboard EnlargeShowSb(this FrameworkElement target)
    {
        var sb = new Storyboard();
        sb.AddEasyDoubleAnime(target, 0, 1, 0.3, "opacity");
        return sb;
    }

    public static Storyboard FadeHideSb(this FrameworkElement target)
    {
        var sb = new Storyboard();
        sb.AddEasyDoubleAnime(target, 1, 0, 0.3, "opacity");
        return sb;
    }

    public static Storyboard HorizonEnlargeShowSb(this FrameworkElement target,double width)
    {
        var sb = new Storyboard();
        sb.AddEasyDoubleAnime(target, 0, width, 0.3, "width");
        sb.AddEasyThicknessAnime(target,  new Thickness(0), new Thickness(2, 0, 2, 0), 0.3, "margin");
        return sb;
    }
        
    public static Storyboard HorizonLessenShowSb(this FrameworkElement target)
    {
        var sb = new Storyboard();
        sb.AddEasyDoubleAnime(target, target.ActualWidth, 0, 0.3, "width");
        sb.AddEasyThicknessAnime(target, new Thickness(2,0,2,0), new Thickness(0), 0.3, "margin");
        return sb;
    }

    public static void SetBgPos(this Viewbox vb,string filename)
    {
        var pairs = filename.Split(' ');
        foreach (var pair in pairs)
        {
            var pair2 = pair.Split('=');
            if(pair2.Length!=2)continue;
            switch (pair2[0])
            {
                case "width":
                    var w = pair2[1].ToInt();
                    if (w > 0) vb.Width = w;
                    break;
                case "height":
                    var h = pair2[1].ToInt();
                    if (h > 0) vb.Height =h;
                    break;
                case "ha":
                    if (pair2[1] == "left") vb.HorizontalAlignment = HorizontalAlignment.Left;
                    if (pair2[1] == "right") vb.HorizontalAlignment = HorizontalAlignment.Right;
                    if (pair2[1] == "center") vb.HorizontalAlignment = HorizontalAlignment.Center;
                    break;
            }
        }
    }

    public static void GoState(this FrameworkElement fe, params string[] stateNames)
    {
        foreach (var stateName in stateNames)
        {
            VisualStateManager.GoToState(fe, stateName, true);
        }
    }

    public static void GoElementState(this FrameworkElement fe, params string[] stateNames)
    {
        foreach (var stateName in stateNames)
        {
            VisualStateManager.GoToElementState(fe, stateName, true);
        }
    }

    public static void CopyToClipboard(this string text,bool isLog=true)
    {
        try
        {
            //Clipboard.SetText(text);
            Clipboard.SetDataObject(text);
            if (isLog)
            {
                Ex.ShowMessage("已复制到剪贴板");
            }
            
        }
        catch (Exception ex)
        {
            Ex.Log(ex.Message);
            Ex.ShowMessage("复制失败");
        }
    }

    public static BitmapImage GetBitmapImageFromStream(Stream stream)
    {
        try
        {
            return SafeLoadBitmapImage(stream);
        }
        catch (IOException)
        {
            try
            {
                return PngLoadBitmapImage(stream);
            }
            catch
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    public static BitmapImage SafeLoadBitmapImage(Stream ms)
    {
        var bitimg = new BitmapImage
        {
            CacheOption = BitmapCacheOption.OnLoad,
            CreateOptions = BitmapCreateOptions.IgnoreColorProfile
        };
        bitimg.BeginInit();
        bitimg.StreamSource = ms;
        bitimg.EndInit();
        bitimg.Freeze();
        //ms.Dispose();
        return bitimg;
    }

    public static BitmapImage PngLoadBitmapImage(Stream stream)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var bitmap = new draw.Bitmap(stream);
            var ms = new MemoryStream();
            bitmap.Save(ms, draw.Imaging.ImageFormat.Png);
            return SafeLoadBitmapImage(ms);
        }
        return null;
    }
}


/// <summary>
/// Popup帮助类，解决Popup设置StayOpen="True"时，移动窗体或者改变窗体大小时，Popup不随窗体移动的问题
/// https://www.cnblogs.com/wuyaxiansheng/p/12660280.html
/// </summary>
public class PopupHelper
{
    public static DependencyObject GetPopupPlacementTarget(DependencyObject obj)
    {
        return (DependencyObject)obj.GetValue(PopupPlacementTargetProperty);
    }

    public static void SetPopupPlacementTarget(DependencyObject obj, DependencyObject value)
    {
        obj.SetValue(PopupPlacementTargetProperty, value);
    }

    public static readonly DependencyProperty PopupPlacementTargetProperty =
        DependencyProperty.RegisterAttached("PopupPlacementTarget", typeof(DependencyObject), typeof(PopupHelper), new PropertyMetadata(null, OnPopupPlacementTargetChanged));

    private static void OnPopupPlacementTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue == null) return;
        var pop = d as Popup;

        if (e.NewValue is not DependencyObject popupPopupPlacementTarget) return;
        var w = Window.GetWindow(popupPopupPlacementTarget);
        if (null == w) return;
        //让Popup随着窗体的移动而移动
        w.LocationChanged += delegate
        {
            if (pop?.IsOpen == false) return;
            var mi = typeof(Popup).GetMethod("UpdatePosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            try
            {
                mi?.Invoke(pop, null);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        };
        //让Popup随着窗体的Size改变而移动位置
        w.SizeChanged += delegate
        {
            if (pop?.IsOpen == false) return;
            var mi = typeof(Popup).GetMethod("UpdatePosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            try
            {
                mi?.Invoke(pop, null);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        };
    }
}

public class ChooseBoxHelper
{
    public Border ChooseBoxBorder { get; set; }
    public Canvas ChooseCanvasRoot { get; set; }
    public WrapPanel ImageItemsWrapPanel { get; set; }
    public ScrollViewer ImageItemsScrollViewer { get; set; }

    private DispatcherTimer _padTimer;
    private Point _chooseStartPoint;
    private bool _isChoosing;
    private int _lineUpDown;
    public void InitSelectBox(Grid root, Border choosebox, Canvas choosecanvasroot, ScrollViewer sc, WrapPanel imageWrapPanel)
    {
        ChooseBoxBorder = choosebox;
        ChooseCanvasRoot = choosecanvasroot;
        ImageItemsScrollViewer = sc;
        ImageItemsWrapPanel = imageWrapPanel;
        _padTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _padTimer.Tick += PadTimerOnTick;
        ImageItemsWrapPanel.PreviewMouseLeftButtonDown += ImageItemsWrapPanelOnPreviewMouseLeftButtonDown;
        ImageItemsWrapPanel.PreviewMouseMove += ImageItemsWrapPanelOnPreviewMouseMove;
        ImageItemsWrapPanel.PreviewMouseLeftButtonUp += ImageItemsWrapPanelOnPreviewMouseLeftButtonUp;
    }
    private void PadTimerOnTick(object sender, EventArgs e)
    {
        if (_lineUpDown < 0)
        {
            ImageItemsScrollViewer.LineUp();
        }
        else if (_lineUpDown > 0)
        {
            ImageItemsScrollViewer.LineDown();
        }
    }

    private void ImageItemsWrapPanelOnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isChoosing) return;
        _isChoosing = false;
        CalculateChosenItem();
        ChooseBoxBorder.Visibility = Visibility.Collapsed;
        _padTimer.Stop();
        _lineUpDown = 0;
    }

    public void CalculateChosenItem()
    {
        // ChooseBox 边界
        var xl = Canvas.GetLeft(ChooseBoxBorder);
        var xr = Canvas.GetLeft(ChooseBoxBorder) + ChooseBoxBorder.Width;
        var yt = Canvas.GetTop(ChooseBoxBorder);
        var yb = Canvas.GetTop(ChooseBoxBorder) + ChooseBoxBorder.Height;
        var ctrlDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftAlt);
        foreach (MoeItemControl child in ImageItemsWrapPanel.Children)
        {
            var isIn = false;
            var pointLeftTop = child.TranslatePoint(new Point(), ImageItemsWrapPanel);
            var pointRightTop = child.TranslatePoint(new Point(child.Width, 0), ImageItemsWrapPanel);
            var pointRightBottom = child.TranslatePoint(new Point(child.Width, child.Height), ImageItemsWrapPanel);
            var pointLeftBottom = child.TranslatePoint(new Point(0, child.Height), ImageItemsWrapPanel);
            var pointCenter = child.TranslatePoint(new Point(child.Width / 2, child.Height / 2), ImageItemsWrapPanel);
            var plist = new List<Point> { pointLeftTop, pointRightTop, pointRightBottom, pointLeftBottom, pointCenter };

            foreach (var point in plist)
            {
                if (point.X > xl && point.X < xr && point.Y > yt && point.Y < yb)
                {
                    isIn = true;
                    break;
                }
            }
            if (isIn) child.ImageCheckBox.IsChecked = !ctrlDown;
        }
    }

    private void ImageItemsWrapPanelOnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Released)
        {
            if (_isChoosing) ImageItemsWrapPanelOnPreviewMouseLeftButtonUp(sender, null);
            return;
        }
        if (!_isChoosing) return;
        if (ChooseBoxBorder.Visibility == Visibility.Collapsed) ChooseBoxBorder.Visibility = Visibility.Visible;
        var moveP = e.GetPosition(ChooseCanvasRoot);
        var vx = moveP.X - _chooseStartPoint.X;
        if (vx < 0)
        {
            Canvas.SetLeft(ChooseBoxBorder, moveP.X);
            ChooseBoxBorder.Width = -vx;
        }
        else
        {
            Canvas.SetLeft(ChooseBoxBorder, _chooseStartPoint.X);
            ChooseBoxBorder.Width = vx;
        }

        var vy = moveP.Y - _chooseStartPoint.Y;
        if (vy < 0)
        {
            Canvas.SetTop(ChooseBoxBorder, moveP.Y);
            ChooseBoxBorder.Height = -vy;
        }
        else
        {
            Canvas.SetTop(ChooseBoxBorder, _chooseStartPoint.Y);
            ChooseBoxBorder.Height = vy;
        }
        _isChoosing = true;

        if (!_padTimer.IsEnabled) _padTimer.Start();
        var pointToViewer = e.GetPosition(ImageItemsScrollViewer);
        if (pointToViewer.Y < 0) _lineUpDown = -1;
        else if (pointToViewer.Y > ImageItemsScrollViewer.ActualHeight) _lineUpDown = 1;
        else _lineUpDown = 0;
    }

    private void ImageItemsWrapPanelOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isChoosing = true;
        _chooseStartPoint = e.GetPosition(ChooseCanvasRoot);
        ChooseBoxBorder.Width = 0;
        ChooseBoxBorder.Height = 0;
        Canvas.SetLeft(ChooseBoxBorder, _chooseStartPoint.X);
        Canvas.SetTop(ChooseBoxBorder, _chooseStartPoint.Y);
    }
}