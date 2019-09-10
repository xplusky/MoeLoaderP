using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;

namespace MoeLoader.Core
{
    /// <summary>
    /// 统一网络操作客户端
    /// </summary>
    public class NetSwap
    {
        public HttpClientHandler HttpClientHandler { get; set; }
        public ProgressMessageHandler ProgressMessageHandler { get; set; }
        public HttpClient Client { get; set; }
        public Settings Settings { get; set; }

        public NetSwap()
        {
            HttpClientHandler = new HttpClientHandler();
            ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
            Client = new HttpClient(ProgressMessageHandler);
        }

        public NetSwap(Settings settings,string cookieurl = null)
        {
            Settings = settings;
            HttpClientHandler = new HttpClientHandler {Proxy = settings.Proxy};
            if (cookieurl != null)
            {
                var cookie = new CookieContainer();
                var cookies = cookie.GetCookies(new Uri(cookieurl));
                HttpClientHandler.CookieContainer.Add(cookies);
            }
            ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
            Client = new HttpClient(ProgressMessageHandler);
            Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.92 Safari/537.36");
            Client.Timeout = TimeSpan.FromSeconds(20);

        }

        public void SetReferer(string rfurl)
        {
            if (string.IsNullOrWhiteSpace(rfurl)) return;
            Client.DefaultRequestHeaders.Referrer = new Uri(rfurl);
        }

        public void SetUserAgent(string ua)
        {
            Client.DefaultRequestHeaders.UserAgent.Clear();
            Client.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
        }

        public void SetTimeOut(double sec)
        {
            Client.Timeout = TimeSpan.FromSeconds(sec);
        }

        public NetSwap CreatNewWithRelatedCookie()
        {
            var net = new NetSwap(Settings) {HttpClientHandler = {CookieContainer = HttpClientHandler.CookieContainer}};
            return net;
        }
    }
}
