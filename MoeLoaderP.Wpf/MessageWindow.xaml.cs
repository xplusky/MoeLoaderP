using MoeLoaderP.Core;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MoeLoaderP.Wpf;

public partial class MessageWindow
{
    public MessageWindow()
    {
        InitializeComponent();
        OkButton.Click += delegate { Close(); };
        MouseLeftButtonDown += delegate { DragMove(); };
    }

    public static bool? ShowDialog(SearchedVisualPage vp)
    {
        var mw = Application.Current.MainWindow;
        var wnd = new MessageWindow
        {
            Owner = mw,
            MessageTextBlock = { Text = "原始内容" },
            MessageTextBox =
            {
                Visibility = Visibility.Collapsed
            },
            MessageTabControl =
            {
                Visibility = Visibility.Visible
            }
        };
        foreach (var rp in vp.RealPages)
        {
            if (rp.OriginString != null)
            {
                AddTabMessage(wnd, rp.CurrentPageNum.ToString(), rp.OriginString.ToString());
            }
        }
        wnd.MessageTextBoxExpander.IsExpanded = true;
        return wnd.ShowDialog();
    }

    public static void AddTabMessage(MessageWindow window, string header, string mes)
    {
        var tab = new TabItem
        {
            Header = header,
            Content = new TextBox
            {
                Text = mes,
                TextWrapping = TextWrapping.Wrap,
                Height = 300,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontSize = 11,
                IsReadOnly = true
            }
        };
        window.MessageTabControl.Items.Add(tab);
    }

    public static bool? ShowDialog(Exception ex, string mes = null, Window owner = null)
    {
        var wnd = new MessageWindow
        {
            MessageTextBlock = { Text = ex.Message },
            MessageTextBox = { Text = ex.ToString() },
            Owner = owner
        };
        Ex.Log(mes);
        return wnd.ShowDialog();
    }

    public static bool? ShowDialog(string messgage, string detail = null, Window owner = null, bool isExpanded = false)
    {
        var wnd = new MessageWindow();
        wnd.MessageTextBlock.Text = messgage;
        if (detail != null) wnd.MessageTextBox.Text = detail;
        if (isExpanded) wnd.MessageTextBoxExpander.IsExpanded = true;
        wnd.Owner = owner;
        return wnd.ShowDialog();
    }

}