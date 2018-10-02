using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Media.Animation;

namespace MoeLoader.Core
{
    /// <summary>
    /// 扩展方法集合
    /// </summary>
    public static class Ext
    {
        /// <summary>
        /// 获取当前FrameworkElement的Storyboard资源
        /// </summary>
        public static Storyboard Sb(this FrameworkElement element,string key)
        {
            return (Storyboard) element.Resources[key];
        }

        /// <summary>
        /// 设置popup显示动画（右键菜单出现动画等）
        /// </summary>
        public static Storyboard LargenShowSb(this FrameworkElement target)
        {
            var sb = new Storyboard();
            sb.AddEasyDoubleAnime(target, 0, 1, 0.3, "opacity");
            sb.AddEasyDoubleAnime(target, 0.9, 1, 0.3, "scale-x");
            sb.AddEasyDoubleAnime(target, 0.9, 1, 0.3, "scale-y");
            return sb;
        }

        public static string ToEncodedUrl(this string orgstr)
        {
            return HttpUtility.UrlEncode(orgstr,Encoding.UTF8);
        }

        public static string ToDecodedUrl(this string orgstr)
        {
            return HttpUtility.UrlDecode(orgstr,Encoding.UTF8);
        }

        public static void Go(this string url)
        {
            try
            {
                Process.Start(url);
            }
            catch 
            {
                // go fail
            }
        }

        public static string LangText(this FrameworkElement el, string key, string arg1 = null,string arg2 = null)
        {
            var text = el.TryFindResource(key) as string;
            return string.IsNullOrWhiteSpace(text) ? text : "{N/A}";
        }

        public static void AddEasyDoubleAnime(this Storyboard sb, DependencyObject target, double fromValue, double toValue, double timeSec, string property)
        {
            var path="";
            switch (property)
            {
                case "opacity":
                    path = "(UIElement.Opacity)";
                    break;
                case "scale-x":
                    path = "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)";
                    break;
                case "scale-y":
                    path = "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)";
                    break;
            }
            var animation = new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = {
                    new EasingDoubleKeyFrame(fromValue,KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new EasingDoubleKeyFrame(toValue,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(timeSec)))
                    {
                        EasingFunction = new ExponentialEase{ EasingMode = EasingMode.EaseOut}
                    }
                },
            };
            Storyboard.SetTargetProperty(animation, new PropertyPath(path));
            Storyboard.SetTarget(animation, target);
            sb.Children.Add(animation);
        }
    }

}
