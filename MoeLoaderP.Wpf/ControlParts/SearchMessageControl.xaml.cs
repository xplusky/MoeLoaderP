using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MoeLoaderP.Wpf.ControlParts
{
    /// <summary>
    /// SearchMessageControl.xaml 的交互逻辑
    /// </summary>
    public partial class SearchMessageControl
    {
        public SearchMessageControl()
        {
            InitializeComponent();
        }

        public void Set(string mes,bool isHighlight=false)
        {
            MessageTextBlock.Text = mes;
            if (isHighlight) MessageTextBlock.Foreground = Brushes.Red;
            BgGrid.Height = 0;
        }

        public async void ShowOneTime(double sec)
        {
            this.Sb("ShowSb").Begin();
            await Task.Delay(TimeSpan.FromSeconds(sec));
            this.Sb("HideSb").Begin();
        }
    }
}
