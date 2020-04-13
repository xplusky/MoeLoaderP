using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf.ControlParts
{
    /// <summary>
    /// AboutControl.xaml 的交互逻辑
    /// </summary>
    public partial class AboutControl
    {
        public AboutControl()
        {
            Initialized += async (sender, args) => 
            {
                await CheckUpdateAsync();
                await CheckThankListAsync();
            };
            InitializeComponent();
        }

        public void Init()
        {
            NewVersionPanel.Visibility = Visibility.Collapsed;
            AboutVersionTextBlock.Text = $"版本：{App.Version.ToString(3)} ({App.CompileTime:yyyy/MM/dd})";
            AboutDonateLink.MouseLeftButtonUp += (sender, args) => AboutDonateImageGrid.Visibility = Visibility.Visible;
            AboutDonateImage.MouseLeftButtonUp += (sender, args) => AboutDonateImageGrid.Visibility = Visibility.Collapsed;
            AboutDonateWexinLink.MouseLeftButtonUp += (sender, args) => AboutDonateWeixinImageGrid.Visibility = Visibility.Visible;
            AboutDonateWeixinImage.MouseLeftButtonUp += (sender, args) => AboutDonateWeixinImageGrid.Visibility = Visibility.Collapsed;
            AboutHomeLinkButton.Click += (sender, args) => "http://leaful.com/moeloader-p/?tab=1".GoUrl();
            AboutReportButton.Click += (sender, args) => "http://leaful.com/moeloader-p/?tab=2".GoUrl();
        }

        public async Task CheckUpdateAsync()
        {
            var json = await new NetDocker().GetJsonAsync($"{App.SaeUrl}/moeloader/update.json");
            if (json == null) return;
            if (Version.Parse($"{json.NetVersion}") > App.Version)
            {
                Extend.ShowMessage($"软件新版提示：{json.NetVersion}({json.RealeseDate})；更新内容：{json.RealeseNotes}；更新请点“关于”按钮");
                NewVersionTextBlock.Text = $"新版提示：{json.NetVersion}({json.RealeseDate})；更新内容：{json.RealeseNotes}";
                NewVersionPanel.Visibility = Visibility.Visible;
                NewVersionDownloadButton.Click += (sender, args) => $"{json.UpdateUrl}".GoUrl();
            }
        }

        public async Task CheckThankListAsync()
        {
            var json = await new NetDocker().GetJsonAsync($"{App.SaeUrl}/thanklist.json");
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
                button.Click += (sender, args) =>
                {
                    $"{user.url}".GoUrl();
                };
                ThanksUserWrapPanel.Children.Add(button);
            }
        }
    }
}
