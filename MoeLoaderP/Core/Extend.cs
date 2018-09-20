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
        /// 纪录日志
        /// </summary>
        public static void Log(params object[] objs)
        {
            Debug.WriteLine($"{DateTime.Now:yyMMdd-HHmmss-ff}>>{objs.Aggregate((o, o1) => $"{o}\r\n{o1}")}");
        }

        public static Storyboard Sb(this FrameworkElement element,string key)
        {
            return (Storyboard) element.Resources[key];
        }

        /// <summary>
        /// 设置popup显示动画
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Storyboard LargenShowSb(this FrameworkElement target)
        {
            var sb = new Storyboard();
            // opacity
            var opacitykeyframes = new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = {
                    new EasingDoubleKeyFrame(0d,KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new EasingDoubleKeyFrame(1d,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3d)))
                    {
                        EasingFunction = new ExponentialEase{ EasingMode = EasingMode.EaseOut}
                    }
                },
            };
            Storyboard.SetTargetProperty(opacitykeyframes, new PropertyPath("(UIElement.Opacity)"));
            Storyboard.SetTarget(opacitykeyframes,target);
            sb.Children.Add(opacitykeyframes);

            // scale x
            var scalexframes = new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = {
                    new EasingDoubleKeyFrame(0.9d,KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new EasingDoubleKeyFrame(1d,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3d)))
                    {
                        EasingFunction = new ExponentialEase{ EasingMode = EasingMode.EaseOut}
                    }
                },
            };
            Storyboard.SetTargetProperty(scalexframes, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            Storyboard.SetTarget(scalexframes, target);
            sb.Children.Add(scalexframes);

            // scale y
            var scaleyframes = new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = {
                    new EasingDoubleKeyFrame(0.9d,KeyTime.FromTimeSpan(TimeSpan.Zero)),
                    new EasingDoubleKeyFrame(1d,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3d)))
                    {
                        EasingFunction = new ExponentialEase{ EasingMode = EasingMode.EaseOut}
                    }
                },
            };
            Storyboard.SetTargetProperty(scaleyframes, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            Storyboard.SetTarget(scaleyframes, target);
            sb.Children.Add(scaleyframes);

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
    }


}
