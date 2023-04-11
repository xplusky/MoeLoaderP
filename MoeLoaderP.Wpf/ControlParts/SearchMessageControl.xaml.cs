using System;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MoeLoaderP.Wpf.ControlParts;

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