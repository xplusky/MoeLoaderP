using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 扩展方法集合
    /// </summary>
    public static class Extend
    {
        public static string ToEncodedUrl(this string orgstr)
        {
            return HttpUtility.UrlEncode(orgstr,Encoding.UTF8);
        }

        public static string ToDecodedUrl(this string orgstr)
        {
            return HttpUtility.UrlDecode(orgstr,Encoding.UTF8);
        }

        public static void Go(this string url)
        {
            try
            {
                Process.Start(url);
            }
            catch 
            {
                // go fail
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        public static void Log(params object[] objs)
        {
            Debug.WriteLine($"{DateTime.Now:yyMMdd-HHmmss-ff}>>{objs.Aggregate((o, o1) => $"{o}\r\n{o1}")}");
        }

        public static Action<string, int> ShowMessageAction;

        /// <summary>
        /// 显示信息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="showPosition">
        /// 显示位置：0：下方弹出黄色气泡，停留10秒，
        /// 1：下方信息栏
        /// </param>
        public static void ShowMessage(string message, int showPosition = 0)
        {
            ShowMessageAction?.Invoke(message, showPosition);
        }

        public static string DisplayName => "MoeLoader +1s";
        public static string Name => "Leaful.MoeLoader";
        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public static DateTime CompileTime => File.GetLastWriteTime(ExeDir);
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

        public static string ExeDir => Directory.GetParent(Process.GetCurrentProcess().MainModule.FileName).FullName;

        public static string SysAppDataDir => Environment.GetEnvironmentVariable("APPDATA");

        public static string UserSkinDir
        {
            get
            {
                var path = Path.Combine(AppDataDir, "Skin");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string MoePicFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), DisplayName);
        public static string SaeUrl => "http://sae.leaful.com/moeloader/";
    }

}
