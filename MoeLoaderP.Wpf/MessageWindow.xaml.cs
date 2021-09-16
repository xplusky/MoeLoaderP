using System;
using System.Windows;

namespace MoeLoaderP.Wpf
{
    public partial class MessageWindow
    {
        public MessageWindow()
        {
            InitializeComponent();
            OkButton.Click += (_, _) => Close(); 
            MouseLeftButtonDown += (_, _) => DragMove();
        }
        
        public static bool? Show(Exception ex,string mes = null)
        {
            var wnd = new MessageWindow
            {
                MessageTextBlock = {Text = ex.Message}, 
                MessageTextBox = {Text = ex.ToString()}
            };
            return wnd.ShowDialog();
        }

        public static bool? Show(string messgage,string detail=null,Window owner=null)
        {
            var wnd = new MessageWindow();
            wnd.MessageTextBlock.Text = messgage;
            if (detail != null) wnd.MessageTextBox.Text = detail;
            wnd.Owner = owner;
            return wnd.ShowDialog();
        }

        public static bool? Debug(string messgage, string detail = null)
        {
            var wnd = new MessageWindow();
            wnd.MessageTextBlock.Text = messgage;
            if (detail != null) wnd.MessageTextBox.Text = detail;
            wnd.MessageTextBoxExpander.IsExpanded = true;
            return wnd.ShowDialog();
        }

    }
}
