using System;
using System.Windows;

namespace MoeLoaderP.Wpf
{
    public partial class MessageWindow
    {
        public MessageWindow()
        {
            InitializeComponent();
            OkButton.Click += (sender, args) => Close(); 
            MouseLeftButtonDown += (sender, args) => DragMove();
        }
        
        public static bool? Show(Exception ex,string mes = null,Window owner = null)
        {
            var wnd = new MessageWindow();
            wnd.MessageTextBlock.Text = ex.Message;
            wnd.MessageTextBox.Text = ex.ToString();
            if (owner != null)
            {
                wnd.Owner = owner;
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            return wnd.ShowDialog();
        }

    }
}
