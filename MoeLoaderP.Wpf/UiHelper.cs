using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using MoeLoaderP.Core;
using draw = System.Drawing;

namespace MoeLoaderP.Wpf;

public static class UiFunc
{
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

    public static void CopyToClipboard(this string text)
    {
        try
        {
            //Clipboard.SetText(text);
            Clipboard.SetDataObject(text);
            Ex.ShowMessage("已复制到剪贴板");
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
public class PopopHelper
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
        DependencyProperty.RegisterAttached("PopupPlacementTarget", typeof(DependencyObject), typeof(PopopHelper), new PropertyMetadata(null, OnPopupPlacementTargetChanged));

    private static void OnPopupPlacementTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != null)
        {
            DependencyObject popupPopupPlacementTarget = e.NewValue as DependencyObject;
            Popup pop = d as Popup;

            Window w = Window.GetWindow(popupPopupPlacementTarget);
            if (null != w)
            {
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
    }
}