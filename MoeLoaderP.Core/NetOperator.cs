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
using HtmlAgilityPack;
using MoeLoaderP.Core.Sites;
using Newtonsoft.Json;

namespace MoeLoaderP.Core;

/// <summary>
///     统一网络操作客户端，下载、显示等
/// </summary>
public class NetOperator
{
    public NetOperator()
    {
        HttpClientHandler = new HttpClientHandler();
        ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
        Client = new HttpClient(ProgressMessageHandler);
    }

    public NetOperator(Settings settings,MoeSite site, double timeout = 40)
    {
        Settings = settings;
        Site = site;
        HttpClientHandler = new HttpClientHandler {Proxy = Proxy};
        //if (cookieurl != null)
        //{
        //    var cookie = new CookieContainer();
        //    var cookies = cookie.GetCookies(new Uri(cookieurl));
        //    HttpClientHandler.CookieContainer.Add(cookies);
        //}
        //const string agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.132 Safari/537.36";
        const string agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.99 Safari/537.36";
        ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
        Client = new HttpClient(ProgressMessageHandler);
        Client.DefaultRequestHeaders.UserAgent.ParseAdd(agent);
        Client.Timeout = TimeSpan.FromSeconds(timeout);
    }

    public HttpClientHandler HttpClientHandler { get; set; }
    public ProgressMessageHandler ProgressMessageHandler { get; set; }
    public HttpClient Client { get; set; }
    public Settings Settings { get; set; }

    public MoeSite Site { get; set; }

    public IWebProxy Proxy
    {
        get
        {
            switch (GetProxyMode(Settings,Site.SiteSettings))
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

    public static Settings.ProxyModeEnum GetProxyMode(Settings settings,IndividualSiteSettings individualSiteSettings)
    {
        return individualSiteSettings.SiteProxy switch
        {
            Settings.ProxyModeEnum.Default => settings.ProxyMode,
            _ => individualSiteSettings.SiteProxy,
        };
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

    public NetOperator CloneWithCookie()
    {
        var net = new NetOperator(Settings,Site)
        {
            HttpClientHandler =
            {
                CookieContainer = HttpClientHandler.CookieContainer
            }
        };
        return net;
    }


    public async Task<HttpResponseMessage> GetAsync(string api, bool showSearchMessage = true,
        CancellationToken token = default)
    {
        if (showSearchMessage) Ex.ShowMessage($"正在获取 {api}", pos: Ex.MessagePos.Searching);

        return await Client.GetAsync(api, token);
    }

    public async Task<string> GetStringAsync(string api, Pairs parapairs = null, bool showSearchMessage = true,
        CancellationToken token = default)
    {
        var query = parapairs.ToPairsString();
        try
        {
            var q = $"{api}{query}";
            var response = await GetAsync(q, showSearchMessage, token);
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

    public async Task<dynamic> GetJsonAsync(string api, Pairs parapairs = null, bool showSearchMessage = true,
        CancellationToken token = default)
    {
        var query = parapairs.ToPairsString();
        try
        {
            var response = await GetAsync($"{api}{query}", showSearchMessage, token);

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

    public async Task<HtmlDocument> GetHtmlAsync(string api, Pairs parapairs = null, bool showSearchMessage = true,
        CancellationToken token = default)
    {
        var query = parapairs.ToPairsString();
        var doc = new HtmlDocument();
        try
        {
            var response = await GetAsync($"{api}{query}", showSearchMessage, token);
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

    public async Task<XmlDocument> GetXmlAsync(string api, Pairs pairs = null, bool showSearchMessage = true,
        CancellationToken token = default)
    {
        var query = pairs.ToPairsString();
        var xml = new XmlDocument();
        try
        {
            var response = await GetAsync($"{api}{query}", showSearchMessage, token);
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

    public async Task<XDocument> GetXDocAsync(string api, Pairs pairs = null, bool showSearchMessage = true,
        CancellationToken token = default)
    {
        var query = pairs.ToPairsString();
        XDocument xml;
        try
        {
            var response = await GetAsync($"{api}{query}", showSearchMessage, token);
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

    public async Task<dynamic> PostAsync(string api, Pairs pairs = null, CancellationToken token = default)
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