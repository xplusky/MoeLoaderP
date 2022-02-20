using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf;

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

public class EggWindowHelper
{
    public EggWindow InstanceWnd { get; set; }
    public MainWindow MainWindow { get; set; }
    private int LogoClickCount { get; set; }
    

    public void Init(MainWindow mainWindow)
    {
        MainWindow = mainWindow;
        MainWindow.KeyDown += MainWindowOnKeyDown;
        MainWindow.LogoImageButton.Click += delegate { ClickEggImage(); };
    }

    private void MainWindowOnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            
            case Key.F7:
                if (MainWindow.Settings.IsShowEggWindowOnce) // Settings.IsShowEggWindowOnce
                {
                    if (InstanceWnd is null) ShowEggWindow();
                    else CloseEggWindow();
                }

                break;
        }
    }

    public void ShowEggWindow()
    {
        if (InstanceWnd is null)
        {
            InstanceWnd = new EggWindow();
            InstanceWnd.Show();
        }
        else
        {
            InstanceWnd.Activate();
        }
    }

    public void CloseEggWindow()
    {
        InstanceWnd?.Close();
        InstanceWnd = null;
    }

    void ShowMessage(string mes)
    {
        MainWindow.ShowMessage(mes);
    }

    public void ClickEggImage()
    {
        LogoClickCount++;
        switch (LogoClickCount)
        {
            case 10:
                ShowMessage("你好！");
                break;
            case 15:
                ShowMessage("你好像很无聊！");
                break;
            case 20:
                ShowMessage("你这样真的好吗？？！");
                break;
            case 30:
                ShowMessage("再点！再点就把你喝掉~~");
                break;
            case 40:
                ShowMessage("你好像真的好无聊 啊ᕦ(･ㅂ･)ᕤ");
                break;
            case 50:
                ShowMessage("你想和我说话吗(・ω<) ﾃﾍﾍﾟﾛ");
                break;
            case 60:
                ShowMessage("我不想和你说(╬￣皿￣)");
                break;
            case 70:
                ShowMessage("再点就要爆炸了！！＜(▰˘◡˘▰)");
                break;
            case 80:
                ShowMessage("你不信吗？？？？(*｀Ω´*)v");
                break;
            case > 90 and < 120 when LogoClickCount % 3 == 0:
                ShowMessage($"{(121 - LogoClickCount) / 3}!!");
                break;
            case 130:
                ShowMessage("嘻嘻~我和你说说话");
                break;
            case 140:
                ShowMessage("其实后面还有哦！！");
                break;
            case 150:
                ShowMessage("其实，我对你…………");
                break;
            case 160:
                ShowMessage("非常讨厌！！！(ノ｀Д)ノ");
                break;
            case 170:
                ShowMessage("不过看你真的很无聊！我就给你点惊喜吧！");
                break;
            case 180:
                ShowMessage("看招！！！！！！");
                break;
            case 190:
                ShowMessage("嘿！！！！开始啦！！！看看我的无敌雪景！！你以后可以按F6来开启和关闭雪景啦");
                MainWindow.Settings.IsShowEggWindowOnce = true;
                ShowEggWindow();
                break;
        }

        if (LogoClickCount > 120)
        {
            var files = $@"{App.ExeDir}\Assets\Egg".GetDirFiles();
            var rndfile = files[new Random().Next(0, files.Length - 1)];
            MainWindow.Player.Source = new Uri(rndfile.FullName, UriKind.Absolute);
            MainWindow.Player.Stop();
            MainWindow.Player.Play();
        }
    }

}