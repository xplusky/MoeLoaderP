using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf.ControlParts
{
    public partial class SettingsControl
    {
        private Settings Settings { get; set; }
        private string _tempCustomProxyText;
        public SettingsControl()
        {
            InitializeComponent();

            foreach (Button button in NameFormatPanel.Children)
            {
                button.Click += FileNameFormatButtonOnClick;
            }

            SaveFolderBrowseButton.Click += SaveFolderBrowseButtonOnClick;
            ClearHistoryButton.Click += ClearHistoryButtonOnClick;

            CustomProxyTextBox.GotFocus += (sender, args) => _tempCustomProxyText = CustomProxyTextBox.Text;
            CustomProxyTextBox.LostFocus += CustomProxyTextBlockOnLostFocus;
            ProxyModeComboBox.SelectionChanged += (sender, args) => CustomProxyTextBox.IsEnabled = ProxyModeComboBox.SelectedIndex == 1; 
            FileNameFormatTextBox.LostFocus += FileNameFormatTextBoxOnLostFocus;

        }

        public void Init(Settings settings)
        {
            Settings = settings;
            DataContext = Settings;
            CustomProxyTextBox.Text = Settings.ProxySetting;
        }

        private void FileNameFormatTextBoxOnLostFocus(object sender, RoutedEventArgs e)
        {
            var isBad = false;
            var output = FileNameFormatTextBox.Text.Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                if (!output.Contains(c)) continue;
                isBad = true;
                output = output.Replace($"{c}", "");
            }
            Settings.SaveFileNameFormat = output;
            if (isBad) Extend.ShowMessage("文件名包含非法字符，已自动去除");
        }

        private void CustomProxyTextBlockOnLostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var strs = CustomProxyTextBox.Text.Split(':');
                var port = int.Parse(strs[1]);
                var address = IPAddress.Parse(strs[0]);
                var _ = new WebProxy(address.ToString(), port);
                Settings.ProxySetting = CustomProxyTextBox.Text;
            }
            catch
            {
                Extend.ShowMessage(this.LangText("TextSettingsProxyModeErrorTip"));
                CustomProxyTextBox.Text = _tempCustomProxyText;
            }
        }

        private void ClearHistoryButtonOnClick(object sender, RoutedEventArgs e)
        {
            Settings.HistoryKeywords.Clear();
            Extend.ShowMessage("已清除历史记录");
        }
        
        private void SaveFolderBrowseButtonOnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = false
            };
            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                Settings.ImageSavePath = dialog.FileNames.ToArray()[0];
            }
        }
        
        /// <summary>
        /// 插入格式到规则文本框
        /// </summary>
        private void FileNameFormatButtonOnClick(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var format = btn.Content.ToString();
            var selectStart = FileNameFormatTextBox.SelectionStart;
            FileNameFormatTextBox.Text = FileNameFormatTextBox.Text.Insert(selectStart, format);
            FileNameFormatTextBox.SelectionStart = selectStart + format.Length;
            FileNameFormatTextBox.Focus();
        }

    }
}
