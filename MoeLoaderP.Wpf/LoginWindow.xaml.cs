using CefSharp;
using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Cookie = CefSharp.Cookie;

namespace MoeLoaderP.Wpf
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow
    {
        private Settings Setting { get; set; }

        private MoeSite Site { get; set; }

        public LoginWindow()
        {
            InitializeComponent();
        }

        public void Init(Settings setting, MoeSite site)
        {
            Setting = setting;
            Site = site;
            MainBrower.IsBrowserInitializedChanged += MainBrowerOnIsBrowserInitializedChanged;
            MainBrower.Loaded += MainBrowerOnLoaded;
            AuthButton.Click += AuthButtonOnClick;
            GoToLoginPageButton.Click += GoToLoginPageButtonOnClick;
        }

        private void GoToLoginPageButtonOnClick(object sender, RoutedEventArgs e)
        {
            MainBrower.Address = Site.LoginPageUrl;
        }

        private string _cookies;
        private async void AuthButtonOnClick(object sender, RoutedEventArgs e)
        {
            AuthLoadingBorder.Visibility = Visibility.Visible;
            AuthMesTextBlock.Text = "认证中，请稍后";
            var b = false;
            while (b == false || _cookies.IsEmpty())
            {
                var cookieManager = Cef.GetGlobalCookieManager();
                var visitor = new CookieVisitor();
                visitor.SendCookie += cookie =>
                {
                    _cookies += cookie.Domain.TrimStart('.') + "^" + cookie.Name + "^" + cookie.Value + ";";
                };
                cookieManager.VisitAllCookies(visitor);
                await Task.Delay(100);
                b = true;
            }

            Dispatcher?.BeginInvoke(new Action(async () =>
            {
                Extend.Log(_cookies);

                if (!Site.VerifyCookie(_cookies))
                {
                    AuthMesTextBlock.Text = "认证失败，请确认登录成功";
                    await Task.Delay(4000);
                    AuthLoadingBorder.Visibility = Visibility.Collapsed;
                    return;
                }
                AuthMesTextBlock.Text = "认证成功，4秒后将关闭窗口";
                Site.CurrentSiteSetting.LoginCookie = _cookies;
                await Task.Delay(4000);
                Close();
            }));
        }

        private void MainBrowerOnIsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Setting.ProxyMode == Settings.ProxyModeEnum.Custom
                && MainBrower.IsBrowserInitialized
                && Cef.IsInitialized)
            {
                //只能在WebBrowser UI呈现后获取 Request 上下文
                Cef.UIThreadTaskFactory.StartNew(delegate
                {
                    //获取 Request 上下文
                    var rc = MainBrower.GetBrowser().GetHost().RequestContext;
                    var dict = new Dictionary<string, object>
                    {
                        {"mode", "fixed_servers"}
                    };
                    var add = Setting.ProxySetting;
                     dict.Add("server", add);
                    //设置代理
                    rc.SetPreference("proxy", dict, out var error);
                    //如果 error 不为空则表示设置失败。
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        MessageBox.Show(error, "Tip", MessageBoxButton.OK);
                    }
                });
            }
        }

        private void MainBrowerOnLoaded(object sender, RoutedEventArgs e)
        {
            MainBrower.Address = Site.LoginPageUrl;
        }

        public class CookieVisitor : ICookieVisitor
        {
            public event Action<Cookie> SendCookie;
            // ReSharper disable once RedundantAssignment
            public bool Visit(Cookie cookie, int count, int total, ref bool deleteCookie)
            {
                deleteCookie = false;
                SendCookie?.Invoke(cookie);

                return true;
            }

            public void Dispose()
            {
                //throw new NotImplementedException();
            }
        }

       
    }
}
