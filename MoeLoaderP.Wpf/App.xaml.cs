using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf;

public partial class App
{
    public static string DisplayName => "MoeLoader +1s";
    public static string Name => "Leaful.MoeLoader";
    public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
    public static DateTime CompileTime => File.GetLastWriteTime(ExeFullPath);
    public static string SettingJsonFilePath => Path.Combine(AppDataDir, "Settings.json");

    public static string AppDataDir
    {
        get
        {
            var path = Path.Combine(SysAppDataDir, Name);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string ExeDir
    {
        get
        {
            var mainModuleFileName = Process.GetCurrentProcess().MainModule?.FileName;
            if (mainModuleFileName != null) return Directory.GetParent(mainModuleFileName)?.FullName;
            return null;
        }
    }

    public static string ExeFullPath => Process.GetCurrentProcess().MainModule?.FileName;

    public static string SysAppDataDir => Environment.GetEnvironmentVariable("APPDATA");

    public static string MoePicFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), DisplayName);
    public static string SaeUrl => "http://sae.leaful.com";

    public static string CustomSiteDir => Path.Combine(AppDataDir, "CustomSites");
    public static string BackgroundImagesDir => Path.Combine(AppDataDir, "Background");

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (Debugger.IsAttached) return;
        const string str = "非常抱歉，MoeLoader +1s 遇到未经处理的异常，程序即将退出T-T！你闲得无聊的话可以将下面显示的错误信息发送至 plusky@126.com 以报告该问题。能不能修好就不知道了 :-)";
        var mainWnd = Current.MainWindow;
        MessageWindow.ShowDialog(e.Exception, str, mainWnd);
        Environment.Exit(0);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Settings settings;
        try
        {
            settings = Settings.Load(SettingJsonFilePath);
            if (settings.ImageSavePath.IsEmpty())
            {
                settings.ImageSavePath = MoePicFolder;
            }
        }
        catch (Exception ex)
        {
            //if (Debugger.IsAttached) throw;
            Ex.Log(ex);
            var result = MessageBox.Show("启动失败，是否尝试清除设置？", "错误", MessageBoxButton.YesNo, MessageBoxImage.Error);
            if (result == MessageBoxResult.Yes)
            {
                settings = new Settings
                {
                    ImageSavePath = MoePicFolder
                };
            }
            else
            {
                settings = null;
                Current.Shutdown();
            }
        }

        var mainWin = new MainWindow();
        mainWin.Init(settings);
        mainWin.Show();
        StatStartupTimesAsync();
    }

    public static async void StatStartupTimesAsync()
    {
        try
        {
            var client = new HttpClient();
            if (Debugger.IsAttached) await client.GetAsync("http://sae.leaful.com/func.php?arg=incr-moeloader-debug-startup");
            else await client.GetAsync("http://sae.leaful.com/func.php?arg=incr-moeloader-startup");
            Ex.Log("StatStartupTimesAsync ok");
        }
        catch (Exception ex)
        {
            Ex.Log(ex);
        }
    }

}