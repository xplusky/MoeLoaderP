using System;
using System.Windows;

namespace MoeLoader
{
    /// <summary>
    /// ExceptionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ExceptionWindow
    {
        public ExceptionWindow()
        {
            InitializeComponent();
            
        }

        public ExceptionWindow(string message) : this()
        {
            MessageTestBox.Text = message ?? "Null";
        }

        public ExceptionWindow(Exception ex) : this()
        {
            MessageTestBox.Text = ex.ToString();
        }

    }
}
