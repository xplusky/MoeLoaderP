using System;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Windows;
using System.Windows.Media.Animation;

namespace MoeLoader.Core
{
    /// <summary>
    /// 扩展方法集合
    /// </summary>
    public static class Extend
    {
        /// <summary>
        /// 获取当前FrameworkElement的Storyboard资源
        /// </summary>
        /// <param name="element"></param>
        /// <param name="key"></param>
        /// <returns></returns>
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
            // opacity
            sb.Children.Add(EasyDoubleTimeLine(target, 0, 1, 0.3, "(UIElement.Opacity)"));
            // scale x
            sb.Children.Add(EasyDoubleTimeLine(target, 0.9, 1, 0.3, "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            // scale y
            sb.Children.Add(EasyDoubleTimeLine(target, 0.9, 1, 0.3, "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            return sb;
        }

        public static string ToEncodedUrl(this string orgstr)
        {
            return HttpUtility.UrlEncode(orgstr);
        }

        public static string ToDecodedUrl(this string orgstr)
        {
            return HttpUtility.UrlDecode(orgstr);
        }

        private static DoubleAnimationUsingKeyFrames EasyDoubleTimeLine(DependencyObject target,double fromValue, double toValue, double timeSec,string path)
        {
            var frames = new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = {
                    new EasingDoubleKeyFrame(fromValue,KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new EasingDoubleKeyFrame(toValue,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(timeSec)))
                    {
                        EasingFunction = new ExponentialEase{ EasingMode = EasingMode.EaseOut}
                    }
                },
            };
            Storyboard.SetTargetProperty(frames,new PropertyPath(path));
            Storyboard.SetTarget(frames,target);
            return frames;
        }
    }


}
