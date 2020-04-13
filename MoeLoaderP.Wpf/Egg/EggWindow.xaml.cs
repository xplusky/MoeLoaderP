using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace MoeLoaderP.Wpf.Egg
{
    /// <summary>
    /// EggWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EggWindow
    {

        //鼠标穿透相关
        const int WsExTransparent = 0x00000020;
        const int WsExToolwindow = 0x00000080;
        const int GwlExstyle = -20;
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        public EggWindow()
        {
            SourceInitialized += OnSourceInitialized;
            InitializeComponent();
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            MousePierce();
        }

        public void MousePierce()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GwlExstyle);
            SetWindowLong(hwnd, GwlExstyle, extendedStyle | WsExTransparent | WsExToolwindow);
        }
    }


}
