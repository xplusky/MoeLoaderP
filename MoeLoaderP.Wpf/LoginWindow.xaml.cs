using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;

namespace MoeLoaderP.Wpf;

/// <summary>
/// LoginWindow.xaml 的交互逻辑
/// </summary>
public partial class LoginWindow
{
    private MoeSite Site { get; set; }
        
    public LoginWindow()
    {
        InitializeComponent();

        AuthLoadingBorder.Visibility = Visibility.Collapsed;
    }

    public CoreWebView2Environment Environment { get; set; }

    public async Task Init(Settings setting, MoeSite site)
    {
        Site = site;
        DataContext = setting;
        Closing += OnClosing;
        this.SetWindowFluent(setting);
        try
        {
            MainBrowser.CoreWebView2InitializationCompleted += MainBrowserOnCoreWebView2InitializationCompleted;
            if (MainBrowser == null) return;
            var option = new CoreWebView2EnvironmentOptions();
            switch (NetOperator.GetProxyMode(setting,site.SiteSettings))
            {
                case Settings.ProxyModeEnum.None:
                    option.AdditionalBrowserArguments = "--no-proxy-server";
                    break;
                case Settings.ProxyModeEnum.Custom:
                    option.AdditionalBrowserArguments = $"--proxy-server=http://{setting.ProxySetting}";
                    break;
                case Settings.ProxyModeEnum.Ie:
                    break;
            }
            Environment = await CoreWebView2Environment.CreateAsync(null,App.AppDataDir,option);
                
            AuthButton.Click += AuthButtonOnClick;
            GoToLoginPageButton.Click += GoToLoginPageButtonOnClick;
            var _ = MainBrowser.EnsureCoreWebView2Async(Environment);
        }
        catch(Exception ex)
        {
            var result = MessageBox.Show(this, "未找到WebView2组件，需要下载吗？（需要Webview2组件才能显示网页登录界面）", App.DisplayName, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                "https://go.microsoft.com/fwlink/p/?LinkId=2124703".GoUrl();
                
            }
            Ex.Log(ex);
            Close();
        }

    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        MainBrowser.Dispose();
    }

    private void MainBrowserOnCoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        var wv = MainBrowser.CoreWebView2;
        wv?.Navigate(Site.LoginPageUrl);
    }

    private void GoToLoginPageButtonOnClick(object sender, RoutedEventArgs e)
    {
        var wv = MainBrowser.CoreWebView2;
        wv?.Navigate(Site.LoginPageUrl);
    }
        
    private async void AuthButtonOnClick(object sender, RoutedEventArgs e)
    {
        AuthLoadingBorder.Visibility = Visibility.Visible;
        AuthTextBlock.Text = "认证中，请稍候";
        AuthLoadingBorder.Background = Brushes.Gray;

        var cookies = new List<CoreWebView2Cookie>();
        foreach (var url in Site.GetCookieUrls())
        {
            var wcookies = await MainBrowser.CoreWebView2.CookieManager.GetCookiesAsync(url);
            cookies.AddRange(wcookies);
        }
                
        var ccol = new CookieCollection();
        foreach (var cookie in cookies)
        {
            var sc = cookie.ToSystemNetCookie();
            ccol.Add(sc);
        }

        var b = Site.VerifyCookie(ccol);

        if (!b)
        {
            AuthTextBlock.Text = "认证失败，请确认登录成功";
            AuthLoadingBorder.Background = Brushes.Red;
            await Task.Delay(4000);
            AuthLoadingBorder.Visibility = Visibility.Collapsed;
            return;
        }

        AuthLoadingBorder.Background = Brushes.Green;
        for (var i = 4; i > 0; i--)
        {
            AuthTextBlock.Text = $"认证成功，{i}秒后将关闭窗口"; 
            await Task.Delay(1000);
        }

        Site.Net = null;
        Site.SiteSettings.LoginCookies = ccol;
        Close();
    }
}