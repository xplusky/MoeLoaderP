using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using MoeLoader.Core;

namespace MoeLoader.UI
{
    public partial class SettingsControl
    {
        private Settings Settings { get; set; }

        public SettingsControl()
        {
            InitializeComponent();

            FNRsite.Click += FileNameFormatButtonOnClick;
            FNRid.Click += FileNameFormatButtonOnClick;
            FNRtag.Click += FileNameFormatButtonOnClick;
            FNRdesc.Click += FileNameFormatButtonOnClick;
            FNRauthor.Click += FileNameFormatButtonOnClick;
            FNRdate.Click += FileNameFormatButtonOnClick;
            FNRimgp.Click += FileNameFormatButtonOnClick;

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
            if (FileNamePatternTextBox.Text.Trim().Length <= 0) return true;
            if (!Path.GetInvalidFileNameChars().Any(rInvalidChar => !rInvalidChar.Equals('<') && FileNamePatternTextBox.Text.Contains(rInvalidChar.ToString()))) return true;
            MessageBox.Show( "文件命名格式不正确，不能含有 \\ / : * ? \" > | 等路径不支持的字符",Res.AppDisplayName, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        
        /// <summary>
        /// 插入格式到规则文本框
        /// </summary>
        private void FileNameFormatButtonOnClick(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var format = btn.Content.ToString();
            var selectstart = FileNamePatternTextBox.SelectionStart;

            if (string.IsNullOrWhiteSpace(FileNamePatternTextBox.SelectedText))
            {
                FileNamePatternTextBox.Text = FileNamePatternTextBox.Text.Insert(selectstart, format.Contains("imgp") ? format.Replace("n", "3") : format);
            }
            else
            {
                FileNamePatternTextBox.SelectedText = format.Contains("imgp") ? format.Replace("n", "3") : format;
                FileNamePatternTextBox.SelectionLength = 0;
            }
            FileNamePatternTextBox.SelectionStart = selectstart + format.Length;
            FileNamePatternTextBox.Focus();
        }

        
    }
}
