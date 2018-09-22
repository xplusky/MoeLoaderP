using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MoeLoader.Core
{
    /// <summary>
    /// 静态常用资源
    /// </summary>
    public static class Res
    {
        public static string AppDisplayName => "MoeLoader +1s";
        public static string AppName => "Leaful.MoeLoader";
        public static Version AppVersion => Assembly.GetExecutingAssembly().GetName().Version;

        public static string AppSettingJsonFilePath => Path.Combine(AppDataDir, "Settings.json");

        public static string AppDataDir
        {
            get
            {
                var path = Path.Combine(SysAppDataDir, AppName);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string AppDir => Directory.GetParent(Process.GetCurrentProcess().MainModule.FileName).FullName;

        public static string AppTempDir => Path.Combine(Path.GetTempPath(), AppName);

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

        public static string MoePicFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), AppDisplayName);

        public static string AppSaeUrl => "http://sae.leaful.com/moeloader/";
    }
}
