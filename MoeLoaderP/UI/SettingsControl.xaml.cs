using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using MoeLoader.Core;

namespace MoeLoader.UI
{
    /// <summary>
    /// SettingsControl.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private Settings Settings { get; set; }

        public SettingsControl()
        {
            InitializeComponent();

            FileNamePatternTextBox.ToolTip = "【以下必须是小写英文】\r\n%site 站点名\r\n%id 编号\r\n%tag 标签\r\n%desc 描述\r\n%author 作者名\r\n%date 上载时间\r\n%imgp[3] 图册页数[页数总长度(补0)]\r\n\r\n" +
                                             "<!< 裁剪符号【注意裁剪符号 <!< 只能有一个】\r\n表示从 <!< 左边所有名称进行过长裁剪、避免路径过长问题\r\n建议把裁剪符号写在 标签%tag 或 描述%desc 后面";

            FNRsite.Click += FileNameFormatButtonOnClick;
            FNRid.Click += FileNameFormatButtonOnClick;
            FNRtag.Click += FileNameFormatButtonOnClick;
            FNRdesc.Click += FileNameFormatButtonOnClick;
            FNRauthor.Click += FileNameFormatButtonOnClick;
            FNRdate.Click += FileNameFormatButtonOnClick;
            FNRimgp.Click += FileNameFormatButtonOnClick;
            FNRcut.Click += FileNameFormatButtonOnClick;

            SaveFolderBrowseButton.Click += SaveFolderBrowseButtonOnClick;
            ClearHistoryButton.Click += ClearHistoryButtonOnClick;
        }

        private void ClearHistoryButtonOnClick(object sender, RoutedEventArgs e)
        {
            Settings.HistoryKeywords.Clear();
        }

        public void Init(Settings settings)
        {
            Settings = settings;
            DataContext = Settings;
        }

        private void SaveFolderBrowseButtonOnClick(object sender, RoutedEventArgs e)
        {
            var diaglog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = false
            };
            var result = diaglog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                Settings.ImageSavePath = diaglog.FileNames.ToArray()[0];
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            //if (txtProxy.Text.Trim().Length > 0)
            //{
            //    string add = txtProxy.Text.Trim();
            //    bool right = false;
            //    if (System.Text.RegularExpressions.Regex.IsMatch(add, @"^.+:(\d+)$"))
            //    {
            //        int port;
            //        if (int.TryParse(add.Substring(add.IndexOf(':') + 1), out port))
            //        {
            //            if (port > 0 && port < 65535)
            //            {
            //                // todo MainWindow.Proxy = txtProxy.Text.Trim();
            //                right = true;
            //            }
            //        }
            //    }
            //    if (!right)
            //    {
            //        MessageBox.Show(this, "代理地址格式不正确，应类似于 127.0.0.1:1080 形式",
            //            AppRes.ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
            //        return;
            //    }
            //}
            //else
            //    MainWindow.Proxy = "";



            //if (rtNoProxy.IsChecked.Value)
            //{
            //    MainWindow.ProxyType = ProxyType.None;
            //}
            //else if (rtSystem.IsChecked.Value)
            //{
            //    MainWindow.ProxyType = ProxyType.System;
            //}
            //else
            //{
            //    MainWindow.ProxyType = ProxyType.Custom;
            //}

            //MainWindow.BossKey = (System.Windows.Forms.Keys)Enum.Parse(typeof(System.Windows.Forms.Keys), txtBossKey.Text);
            // MainWindow.namePatter = txtPattern.Text.Replace(";", "；").Trim();
            
            
        }

        public bool VerifyFileNamePattern()
        {
            if (FileNamePatternTextBox.Text.Trim().Length > 0)
            {
                foreach (char rInvalidChar in System.IO.Path.GetInvalidFileNameChars())
                {
                    if (!rInvalidChar.Equals('<') && FileNamePatternTextBox.Text.Contains(rInvalidChar.ToString()))
                    {
                        MessageBox.Show( "文件命名格式不正确，不能含有 \\ / : * ? \" > | 等路径不支持的字符",
                            AppRes.AppDisplayName, MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                }
            }

            return true;
        }

        
        /// <summary>
        /// 插入格式到规则文本框
        /// </summary>
        private void FileNameFormatButtonOnClick(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            string format = btn.Content.ToString();
            int selectstart = FileNamePatternTextBox.SelectionStart;

            if (string.IsNullOrWhiteSpace(FileNamePatternTextBox.SelectedText))
            {
                if (format.Contains("imgp"))
                    FileNamePatternTextBox.Text = FileNamePatternTextBox.Text.Insert(selectstart, format.Replace("n", "3"));
                else
                    FileNamePatternTextBox.Text = FileNamePatternTextBox.Text.Insert(selectstart, format);
            }
            else
            {
                if (format.Contains("imgp"))
                    FileNamePatternTextBox.SelectedText = format.Replace("n", "3");
                else
                    FileNamePatternTextBox.SelectedText = format;
                FileNamePatternTextBox.SelectionLength = 0;
            }
            FileNamePatternTextBox.SelectionStart = selectstart + format.Length;
            FileNamePatternTextBox.Focus();
        }

        
    }
}
