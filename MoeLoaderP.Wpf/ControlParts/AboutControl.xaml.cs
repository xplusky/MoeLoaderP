using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf.ControlParts;

/// <summary>
/// AboutControl.xaml 的交互逻辑
/// </summary>
public partial class AboutControl
{
    public AboutControl()
    {
        Initialized += OnInitialized;
        InitializeComponent();
    }

    private async void OnInitialized(object sender, EventArgs e)
    {
        await CheckUpdateAsync();
        await CheckThankListAsync();
    }


    public void Init()
    {
        NewVersionPanel.Visibility = Visibility.Collapsed;
        AboutVersionTextBlock.Text = $"版本：{App.Version.ToString(3)} ({App.CompileTime:yyyy/MM/dd})";
        AboutDonateLink.MouseLeftButtonUp += delegate { AboutDonateImageGrid.Visibility = Visibility.Visible; };
        AboutDonateImage.MouseLeftButtonUp += delegate { AboutDonateImageGrid.Visibility = Visibility.Collapsed; };
        AboutDonateWexinLink.MouseLeftButtonUp += delegate
        {
            AboutDonateWeixinImageGrid.Visibility = Visibility.Visible;
        };
        AboutDonateWeixinImage.MouseLeftButtonUp += delegate
        {
            AboutDonateWeixinImageGrid.Visibility = Visibility.Collapsed;
        };
        AboutHomeLinkButton.Click += delegate { "http://leaful.com/moeloader-p/?tab=1".GoUrl(); };
        AboutReportButton.Click += delegate { "http://leaful.com/moeloader-p/?tab=2".GoUrl(); };
    }

    public async Task CheckUpdateAsync()
    {
        var json = await new NetOperator().GetJsonAsync($"{App.SaeUrl}/moeloader/update.json");
        if (json == null) return;
        if (Version.Parse($"{json.NetVersion}") > App.Version)
        {
            Ex.ShowMessage($"软件新版提示：{json.NetVersion}({json.RealeseDate})；更新内容：{json.RealeseNotes}；更新请点“关于”按钮");
            NewVersionTextBlock.Text = $"新版提示：{json.NetVersion}({json.RealeseDate})；更新内容：{json.RealeseNotes}";
            NewVersionPanel.Visibility = Visibility.Visible;
            NewVersionDownloadButton.Click += delegate { $"{json.UpdateUrl}".GoUrl(); };
        }
    }

    public async Task CheckThankListAsync()
    {
        var json = await new NetOperator().GetJsonAsync($"{App.SaeUrl}/thanklist.json");
        if (json == null) return;
        foreach (var user in json)
        {
            var text = $"{user.name}";
            var texblock = new TextBlock
            {
                FontSize = 12,
                Margin = new Thickness(4),
                Foreground = Brushes.White,
                Text = text
            };
            var button = new Button
            {
                Template = (ControlTemplate)FindResource("MoeTagButtonControlTemplate"),
                Content = texblock,
                Margin = new Thickness(1),
                ToolTip = $"{text} {user.tip}"
            };
            button.Click += delegate { $"{user.url}".GoUrl(); };
            ThanksUserWrapPanel.Children.Add(button);
        }
    }
}