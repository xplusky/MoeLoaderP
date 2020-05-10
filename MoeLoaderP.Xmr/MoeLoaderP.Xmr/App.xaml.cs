using System;
using System.IO;
using MoeLoaderP.Core;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MoeLoaderP.Xmr.Services;
using MoeLoaderP.Xmr.Views;

namespace MoeLoaderP.Xmr
{
    public partial class App : Application
    {
        public static string AppDataDir => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static string SettingJsonFile => Path.Combine(AppDataDir, "Settings.json");
        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();

            var settings = Settings.Load(SettingJsonFile);

            MainPage = new MainPage();
            
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
