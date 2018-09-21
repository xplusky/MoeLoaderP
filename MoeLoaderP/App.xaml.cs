using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MoeLoader.Core;

namespace MoeLoader
{
    public partial class App
    {
        public App()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached) return;
            try
            {
                const string str = "非常抱歉，MoeLoader +1s 遇到致命错误，您可以将下面显示的错误信息发送至 plusky@126.com 以报告该问题。";
                MessageWindow.Show(e.Exception, str);
            }
            catch
            {
                MessageBox.Show("未知异常，应用程序将退出！");
            }
            Environment.Exit(0);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var settings = Settings.Load();
            var mainwin = new MainWindow(settings);
            mainwin.Show();
            StatStartupTimesAsync();
        }

        public async void StatStartupTimesAsync()
        {
            try
            {
                var client = new HttpClient();
                if (Debugger.IsAttached) await client.GetAsync("http://sae.leaful.com/func.php?arg=incr-moeloader-debug-startup");
                else await client.GetAsync("http://sae.leaful.com/func.php?arg=incr-moeloader-startup");
                Extend.Log("StatStartupTimesAsync ok");
            }
            catch (Exception ex)
            {
                Extend.Log(ex);
            }
        }
    }
}
