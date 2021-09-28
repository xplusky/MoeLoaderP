using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 统一网络操作客户端
    /// </summary>
    public class NetOperator
    {
        public HttpClientHandler HttpClientHandler { get; set; }
        public ProgressMessageHandler ProgressMessageHandler { get; set; }
        public HttpClient Client { get; set; }
        public Settings Settings { get; set; }

        public NetOperator()
        {
            HttpClientHandler = new HttpClientHandler();
            ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
            Client = new HttpClient(ProgressMessageHandler);
        }

        public NetOperator(Settings settings, double timeout = 40)
        {
            Settings = settings;
            HttpClientHandler = new HttpClientHandler { Proxy = Proxy };
            //if (cookieurl != null)
            //{
            //    var cookie = new CookieContainer();
            //    var cookies = cookie.GetCookies(new Uri(cookieurl));
            //    HttpClientHandler.CookieContainer.Add(cookies);
            //}
            const string agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.132 Safari/537.36";
            ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
            Client = new HttpClient(ProgressMessageHandler);
            Client.DefaultRequestHeaders.UserAgent.ParseAdd(agent);
            Client.Timeout = TimeSpan.FromSeconds(timeout);
        }

        public void SetReferer(string rfurl)
        {
            if (rfurl.IsEmpty()) return;
            Client.DefaultRequestHeaders.Referrer = new Uri(rfurl);
        }

        public void SetTimeOut(double sec)
        {
            Client.Timeout = TimeSpan.FromSeconds(sec);
        }

        public void SetCookie(CookieContainer cc)
        {
            if (cc != null) HttpClientHandler.CookieContainer = cc;
        }

        public NetOperator CreateNewWithOldCookie()
        {
            var net = new NetOperator(Settings)
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
                                var port = strs[1].ToInt();
                                if (port == 0) return WebRequest.DefaultWebProxy;
                                var address = IPAddress.Parse(strs[0]);
                                var proxy = new WebProxy(address.ToString(), port);
                                return proxy;
                            }
                            catch (Exception e)
                            {
                                Ex.Log(e);
                                return WebRequest.DefaultWebProxy;
                            }
                        }
                    case Settings.ProxyModeEnum.Ie: return WebRequest.DefaultWebProxy;
                }
                return null;
            }
        }

        public async Task<string> GetStringAsync(string api, CancellationToken token = default, Pairs parapairs = null)
        {
            var query = parapairs.ToPairsString();
            try
            {
                var q = $"{api}{query}";
                var response = await Client.GetAsync(q, token);
                var s = await response.Content.ReadAsStringAsync(token);
                return s;
            }
            catch (Exception e)
            {
                Ex.Log(e);
                Ex.ShowMessage(e.Message);
                return null;
            }
        }

        public async Task<dynamic> GetJsonAsync(string api, CancellationToken token = default, Pairs parapairs = null)
        {
            var query = parapairs.ToPairsString();
            try
            {
                var response = await Client.GetAsync($"{api}{query}", token);
                var s = await response.Content.ReadAsStringAsync(token);
                return JsonConvert.DeserializeObject(s);
            }
            catch (Exception e)
            {
                Ex.Log(e);
                Ex.ShowMessage(e.Message);
                return null;
            }
        }
        public async Task<HtmlDocument> GetHtmlAsync(string api, CancellationToken token = default, Pairs parapairs = null)
        {
            var query = parapairs.ToPairsString();
            var doc = new HtmlDocument();
            try
            {
                var response = await Client.GetAsync($"{api}{query}", token);
                var s = await response.Content.ReadAsStringAsync(token);
                doc.LoadHtml(s);
                return doc;
            }
            catch (Exception e)
            {
                Ex.ShowMessage(e.Message);
                Ex.Log(e);
                return null;
            }
        }

        public async Task<XmlDocument> GetXmlAsync(string api, CancellationToken token = default, Pairs pairs = null)
        {
            var query = pairs.ToPairsString();
            var xml = new XmlDocument();
            try
            {
                var response = await Client.GetAsync($"{api}{query}", token);
                var s = await response.Content.ReadAsStringAsync(token);
                xml.LoadXml(s);
            }
            catch (Exception e)
            {
                Ex.ShowMessage(e.Message);
                Ex.Log(e);
                return null;
            }
            return xml;
        }

        public async Task<XDocument> GetXDocAsync(string api, CancellationToken token = default, Pairs pairs = null)
        {
            var query = pairs.ToPairsString();
            XDocument xml ;
            try
            {
                var response = await Client.GetAsync($"{api}{query}", token);
                var s = await response.Content.ReadAsStreamAsync(token);
                xml = XDocument.Load(s);
            }
            catch (Exception e)
            {
                Ex.ShowMessage(e.Message);
                Ex.Log(e);
                return null;
            }
            return xml;
        }

        public async Task<dynamic> PostAsync(string api, CancellationToken token = default, Pairs pairs = null)
        {
            var query = pairs.ToPairsString();
            var content = new StringContent($"{query}", Encoding.UTF8, "application/x-www-form-urlencoded");
            try
            {
                var response = await Client.PostAsync(api, content, token);
                var str = await response.Content.ReadAsStringAsync(token);
                var json = JsonConvert.DeserializeObject<dynamic>(str);
                return json;
            }
            catch (Exception e)
            {
                Ex.ShowMessage(e.Message);
                Ex.Log(e);
                return null;
            }
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
