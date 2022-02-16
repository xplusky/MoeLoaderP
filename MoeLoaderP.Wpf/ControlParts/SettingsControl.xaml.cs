using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Dialogs;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf.ControlParts;

public partial class SettingsControl
{
    private string _tempCustomProxyText;

    public SettingsControl()
    {
        InitializeComponent();

        SaveFolderBrowseButton.Click += SaveFolderBrowseButtonOnClick;
        ClearHistoryButton.Click += ClearHistoryButtonOnClick;

        CustomProxyTextBox.GotFocus += delegate { _tempCustomProxyText = CustomProxyTextBox.Text; };
        CustomProxyTextBox.LostFocus += CustomProxyTextBlockOnLostFocus;

        ProxyModeComboBox.SelectionChanged += delegate { CustomProxyTextBox.IsEnabled = ProxyModeComboBox.SelectedIndex == 1; };
        FileNameFormatTextBox.LostFocus += FileNameFormatTextBoxOnLostFocus;
        FileNameFormatTextBox.GotKeyboardFocus += delegate { SetRenameButtons(FileNameFormatButtonsPanel, FileNameFormatTextBox); };
        FileNameFormatTextBox.LostKeyboardFocus += delegate { RemoveRenameButtons(); };
        FileNameFormatResetButton.Click += delegate { Settings.SaveFileNameFormat = Settings.SaveFileNameFormatDefaultValue; };
        SortFolderNameFormatTextBox.LostFocus += SortFolderNameFormatTextBoxOnLostFocus;
        SortFolderNameFormatTextBox.GotKeyboardFocus += delegate { SetRenameButtons(SubDirNameFormatButtonsPanel, SortFolderNameFormatTextBox); };
        SortFolderNameFormatTextBox.LostKeyboardFocus += delegate { RemoveRenameButtons(); };
        SortFolderNameFormatResetButton.Click += delegate { Settings.SortFolderNameFormat = Settings.SortFolderNameFormatDefaultValue; };

        OpenCustomSiteDirButton.Click += delegate { App.CustomSiteDir.GoDirectory(); };
        BgImageOpenDirButton.Click += delegate { App.BackgroundImagesDir.GoDirectory(); };
    }

    private Settings Settings { get; set; }

    private TextBox LastGotFocusTextBox { get; set; }

    public static void ChangeBgImage()
    {
        if (Application.Current.MainWindow is not MainWindow mw) return;
        var files = App.BackgroundImagesDir.GetDirFiles()?
            .Where(info => info.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (files is null) return;
        if (files.Length == 0) return;
        var rndfile = files[new Random().Next(0, files.Length)];
        mw.BgImage.Source = new BitmapImage(new Uri(rndfile.FullName, UriKind.Absolute));
        mw.BgGridViewBox.Width = 670;
        mw.BgGridViewBox.Height = 530;
        mw.BgGridViewBox.HorizontalAlignment = HorizontalAlignment.Right;
        try
        {
            var par = rndfile.Name.Remove(rndfile.Name.LastIndexOf(".", StringComparison.Ordinal));
            mw.BgGridViewBox.SetBgPos(par);
        }
        catch (Exception e)
        {
            Ex.Log(e);
        }
    }

    public void RemoveRenameButtons()
    {
        SubDirNameFormatButtonsPanel.Children.Clear();
        FileNameFormatButtonsPanel.Children.Clear();
    }

    public void SetRenameButtons(WrapPanel panel, TextBox last)
    {
        LastGotFocusTextBox = last;
        var pairs = MoeDownloader.GenRenamePairs();
        foreach (var (key, value) in pairs)
        {
            var button = new Button
            {
                Template = FindResource("MoeButtonControlTemplate") as ControlTemplate,
                Height = 24,
                ToolTip = value,
                Margin = new Thickness(2),
                Content = new TextBlock
                {
                    Text = key,
                    Margin = new Thickness(4, 0, 4, 0)
                },
                Focusable = false
            };
            button.Click += delegate { FileNameFormatButtonOnClick(value); };
            panel.Children.Add(button);
        }
    }


    private void SortFolderNameFormatTextBoxOnLostFocus(object sender, RoutedEventArgs e)
    {
        var isBad = false;
        var output = SortFolderNameFormatTextBox.Text.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            if (!output.Contains(c)) continue;
            if (c == '\\') continue;
            isBad = true;
            output = output.Replace($"{c}", "");
        }

        Settings.SortFolderNameFormat = output;
        if (isBad) Ex.ShowMessage("路径名包含非法字符，已自动去除");
    }

    public void Init(Settings settings)
    {
        Settings = settings;
        DataContext = Settings;
        CustomProxyTextBox.Text = Settings.ProxySetting;

        BgImageChangeButton.Click += BgImageChangeButton_Click;
        ChangeBgImage();
    }

    private void BgImageChangeButton_Click(object sender, RoutedEventArgs e)
    {
        ChangeBgImage();
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
        if (isBad) Ex.ShowMessage("文件名包含非法字符，已自动去除");
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
            Ex.ShowMessage(this.LangText("TextSettingsProxyModeErrorTip"));
            CustomProxyTextBox.Text = _tempCustomProxyText;
        }
    }

    private void ClearHistoryButtonOnClick(object sender, RoutedEventArgs e)
    {
        foreach (var setting in Settings.AllSitesSettings) setting.Value.History.Clear();
        Ex.ShowMessage("已清除所有历史记录");
    }

    private void SaveFolderBrowseButtonOnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Multiselect = false
        };
        var result = dialog.ShowDialog();
        if (result == CommonFileDialogResult.Ok) Settings.ImageSavePath = dialog.FileNames.ToArray()[0];
    }

    /// <summary>
    ///     插入格式到规则文本框
    /// </summary>
    private void FileNameFormatButtonOnClick(string value)
    {
        var tb = LastGotFocusTextBox;
        if (tb == null) return;
        var selectStart = tb.SelectionStart;
        if (value != null)
        {
            tb.Text = tb.Text.Insert(selectStart, value);
            tb.SelectionStart = selectStart + value.Length;
        }
    }
}