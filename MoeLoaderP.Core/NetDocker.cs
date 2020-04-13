using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 统一网络操作客户端
    /// </summary>
    public class NetDocker
    {
        public HttpClientHandler HttpClientHandler { get; set; }
        public ProgressMessageHandler ProgressMessageHandler { get; set; }
        public HttpClient Client { get; set; }
        public Settings Settings { get; set; }

        public NetDocker()
        {
            HttpClientHandler = new HttpClientHandler();
            ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
            Client = new HttpClient(ProgressMessageHandler);
        }

        public NetDocker(Settings settings, string cookieurl = null)
        {
            Settings = settings;
            HttpClientHandler = new HttpClientHandler { Proxy = Proxy };
            if (cookieurl != null)
            {
                var cookie = new CookieContainer();
                var cookies = cookie.GetCookies(new Uri(cookieurl));
                HttpClientHandler.CookieContainer.Add(cookies);
            }
            ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
            Client = new HttpClient(ProgressMessageHandler);
            const string agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.132 Safari/537.36";
            Client.DefaultRequestHeaders.UserAgent.ParseAdd(agent);
            Client.Timeout = TimeSpan.FromSeconds(40);

        }

        public void SetReferer(string rfurl)
        {
            if (string.IsNullOrWhiteSpace(rfurl)) return;
            Client.DefaultRequestHeaders.Referrer = new Uri(rfurl);
        }

        public void SetTimeOut(double sec)
        {
            Client.Timeout = TimeSpan.FromSeconds(sec);
        }

        public NetDocker CloneWithOldCookie()
        {
            var net = new NetDocker(Settings)
            {
                HttpClientHandler =
                {
                    CookieContainer = HttpClientHandler.CookieContainer
                }
            };
            return net;
        }


        public IWebProxy Proxy
        {
            get
            {
                switch (Settings.ProxyMode)
                {
                    case Settings.ProxyModeEnum.None: return new WebProxy();
                    case Settings.ProxyModeEnum.Custom:
                        {
                            try
                            {
                                var strs = Settings.ProxySetting.Split(':');
                                var port = int.Parse(strs[1]);
                                var address = IPAddress.Parse(strs[0]);
                                var porxy = new WebProxy(address.ToString(), port);
                                return porxy;
                            }
                            catch (Exception e)
                            {
                                Extend.Log(e);
                                return WebRequest.DefaultWebProxy;
                            }
                        }
                    case Settings.ProxyModeEnum.Ie: return WebRequest.DefaultWebProxy;
                }
                return null;
            }
        }

        public async Task<dynamic> GetJsonAsync(string api, CancellationToken token = new CancellationToken(), Pairs parapairs = null)
        {
            var query = GetPairsString(parapairs);
            try
            {
                var response = await Client.GetAsync($"{api}{query}", token);
                var s = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject(s);
            }
            catch (Exception e)
            {
                Extend.Log(e);
                return null;
            }
        }
        public async Task<HtmlDocument> GetHtmlAsync(string api, CancellationToken token = new CancellationToken(), Pairs parapairs = null)
        {
            var query = GetPairsString(parapairs);
            var doc = new HtmlDocument();
            try
            {
                var response = await Client.GetAsync($"{api}{query}", token);
                var s = await response.Content.ReadAsStringAsync();
                doc.LoadHtml(s);
                return doc;
            }
            catch (Exception e)
            {
                Extend.Log(e);
                return null;
            }
        }
        public static string GetPairsString(Pairs pairs)
        {
            var query = string.Empty;
            var i = 0;
            if (pairs == null) return query;
            foreach (var para in pairs.Where(para => !string.IsNullOrEmpty(para.Value)))
            {
                query += string.Format("{2}{0}={1}", para.Key, para.Value, i > 0 ? "&" : "?");
                i++;
            }

            return query;
        }
    }


    public class Pairs : List<KeyValuePair<string, string>>
    {
        public void Add(string str1, string str2)
        {
            Add(new KeyValuePair<string, string>(str1, str2));
        }
    }
}
