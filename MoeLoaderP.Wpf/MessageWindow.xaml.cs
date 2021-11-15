using MoeLoaderP.Core;
using System;
using System.Windows;

namespace MoeLoaderP.Wpf
{
    public partial class MessageWindow
    {
        public MessageWindow()
        {
            InitializeComponent();
            OkButton.Click += delegate { Close(); };
            MouseLeftButtonDown += delegate { DragMove(); };
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
}
