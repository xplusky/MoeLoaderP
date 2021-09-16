using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MoeLoaderP.Wpf.ControlParts
{
    /// <summary>
    /// 设置框，右键菜单等
    /// </summary>
    public class MoeFloatBorder : Border
    {
        public MoeFloatBorder()
        {
            
            BorderBrush = FindResource("MoeButtonStrokeBrush") as SolidColorBrush;
            Background = FindResource("MoeImageBorderBrush") as LinearGradientBrush;
            Margin = new Thickness(10);
            Padding = new Thickness(8);
            DropShadowEffect dse = new() { BlurRadius = 10, ShadowDepth = 0, Opacity = 0.65};
            Effect = dse;
            CornerRadius = new CornerRadius(8);
            BorderThickness = new Thickness(1);
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.EnlargeShowSb().Begin();
        }
    }
}
