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
            const string str = "非常抱歉，MoeLoader +1s 遇到未经处理的异常，程序即将退出T-T！你闲得无聊的话可以将下面显示的错误信息发送至 plusky@126.com 以报告该问题。能不能改好就不知道了 :-)";
            MessageWindow.Show(e.Exception, str);
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
                Log("StatStartupTimesAsync ok");
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        public static void Log(params object[] objs)
        {
            Debug.WriteLine($"{DateTime.Now:yyMMdd-HHmmss-ff}>>{objs.Aggregate((o, o1) => $"{o}\r\n{o1}")}");
        }
    }
}
