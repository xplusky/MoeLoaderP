using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf
{
    /// <summary>
    /// EggWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EggWindow
    {

        //鼠标穿透相关
        private const int WsExTransparent = 0x00000020;
        private const int WsExToolwindow = 0x00000080;
        private const int GwlExstyle = -20;
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        public EggWindow()
        {
            Loaded+= OnLoaded;
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            MousePierce();
        }
        

        public void MousePierce()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var extendedStyle = GetWindowLong(hwnd, GwlExstyle);
            var result = SetWindowLong(hwnd, GwlExstyle, extendedStyle | WsExTransparent | WsExToolwindow);
            Ex.Log(result);
        }
    }


}
