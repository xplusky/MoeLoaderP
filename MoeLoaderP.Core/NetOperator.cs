using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
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
    public HttpClientHandler HttpClientHandler { get; set; }
    public SocketsHttpHandler SocketsHttpHandler { get; set; }
    public ProgressMessageHandler ProgressMessageHandler { get; set; }
    public HttpClient Client { get; set; }
    public Settings Settings { get; set; }
    public MoeSite Site { get; set; }

    public double Timeout { get; set; }
    public NetOperator()
    {
        
        HttpClientHandler = new HttpClientHandler();
        ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
        Client = new HttpClient(ProgressMessageHandler);
    }

    public void InitProgressMessageHandler()
    {
        switch (GetProxyMode(Settings, Site.SiteSettings))
        {
            case Settings.ProxyModeEnum.None:
            {
                HttpClientHandler = new HttpClientHandler();
                ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
                break;
            }
            case Settings.ProxyModeEnum.Custom:
            {
                
                try
                {
                    switch (Settings.ProxyConnectMode)
                    {
                        case Settings.ProxyConnectModeEnum.Http:
                            HttpClientHandler = new HttpClientHandler
                            {
                                Proxy = new WebProxy($"http://{Settings.ProxySetting}")
                            };
                            ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
                            break;
                        case Settings.ProxyConnectModeEnum.Socks:
                            ProgressMessageHandler = new ProgressMessageHandler(new SocketsHttpHandler
                            {
                                Proxy = new WebProxy($"socks5://{Settings.ProxySetting}")
                            });
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                }
                catch (Exception e)
                {
                    Ex.Log(e);
                    ProgressMessageHandler = new ProgressMessageHandler(new HttpClientHandler());
                }
                break;
            }
            case Settings.ProxyModeEnum.Ie:
                ProgressMessageHandler = new ProgressMessageHandler(new HttpClientHandler { Proxy = WebRequest.DefaultWebProxy });
                
                break;
        }
    }

    public NetOperator(Settings settings,MoeSite site, double timeout = 40)
    {
        Settings = settings;
        Site = site;
        Timeout = timeout;

        InitProgressMessageHandler();

        Client = new HttpClient(ProgressMessageHandler);
        
        //var agent = Agents[new Random().Next(0, Agents.Length - 1)];
        var header = Client.DefaultRequestHeaders;
        //header.UserAgent.ParseAdd(agent);
        //header.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        //header.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
        if (site is DanbooruSite)
        {
            header.TryAddWithoutValidation("User-Agent", "gdl/1.24.5");
        }
        else
        {
            header.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");
        }
        //header.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");
        //header.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
        //header.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.8,zh-TW;q=0.7,zh-HK;q=0.5,en-US;q=0.3,en;q=0.2");
        //header.TryAddWithoutValidation("Connection", "keep-alive");
        //header.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
        //header.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        //header.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
        //header.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
        //header.TryAddWithoutValidation("Sec-Fetch-User", "?1");
        //header.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");


        Client.Timeout = TimeSpan.FromSeconds(Timeout);
    }
    

    //public static string[] Agents =
    //{
    //    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36",
    //    //"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.99 Safari/537.36",
    //};

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
        var cc = HttpClientHandler.CookieContainer;
        var net = new NetOperator(Settings,Site)
        {
            HttpClientHandler = 
            {
                CookieContainer = cc
            }
        };
        return net;
    }


    public async Task<HttpResponseMessage> GetAsync(string api, bool showSearchMessage = true,int retrytimes = 1,bool useGzip = false, CancellationToken token = default)
    {
        if (showSearchMessage) Ex.ShowMessage($"正在获取 {api}", pos: Ex.MessagePos.Searching);
        
        var times = retrytimes;
        while (times>=0)
        {
            try
            {
                if (useGzip)
                {
                    Client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
                }
                return await Client.GetAsync(api, token);
            }
            catch (Exception)
            {
                if (times == 0) throw;
                times--;
            }
        }

        return null;
    }

    public async Task<string> GetStringAsync(string api, Pairs parapairs = null, bool showSearchMessage = true, CancellationToken token = default)
    {
        var query = parapairs.ToPairsString();
        try
        {
            var q = $"{api}{query}";
            var response = await GetAsync(q, showSearchMessage, token: token);
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

    public async Task<dynamic> GetJsonAsync(string api, Pairs parapairs = null, bool showSearchMessage = true, bool saveListOriginalString = false, bool tryWebView = false,
        CancellationToken token = default)
    {
        var query = parapairs.ToPairsString();
        var response = await GetAsync($"{api}{query}", showSearchMessage, token: token);
            var res = response.Headers;
            var s = await response.Content.ReadAsStringAsync(token);
        if (saveListOriginalString)
        {
            Ex.LogListOriginalString = s;
        }
        try
        {
            dynamic deo = JsonConvert.DeserializeObject(s);
            
            return deo;
        }
        catch (JsonReaderException e)
        {
            if (Ex.IsHtml(s))
            {
                Ex.Log($"IsHtml:{api}{query}");
                return null;
            }
        }
        catch (Exception e)
        {
            Ex.Log(e);
            Ex.ShowMessage(e.Message);
            return null;
        }

        return null;
    }

    //public  void InitWebView()
    //{
    //    WebView = new WebView2();
    //    try
    //    {
    //        WebView.CoreWebView2InitializationCompleted += WebViewOnCoreWebView2InitializationCompleted;
            
    //        var option = new CoreWebView2EnvironmentOptions();
    //        switch (GetProxyMode(Settings, Site.SiteSettings))
    //        {
    //            case Settings.ProxyModeEnum.None:
    //                option.AdditionalBrowserArguments = "--no-proxy-server";
    //                break;
    //            case Settings.ProxyModeEnum.Custom:
    //                option.AdditionalBrowserArguments = $"--proxy-server=http://{Settings.ProxySetting}";
    //                break;
    //            case Settings.ProxyModeEnum.Ie:
    //                break;
    //        }
    //        Environment = await CoreWebView2Environment.CreateAsync(null, App.AppDataDir, option);

    //        AuthButton.Click += AuthButtonOnClick;
    //        GoToLoginPageButton.Click += GoToLoginPageButtonOnClick;
    //        var _ = MainBrowser.EnsureCoreWebView2Async(Environment);
    //    }
    //    catch (Exception ex)
    //    {
    //        var result = MessageBox.Show(this, "未找到WebView2组件，需要下载吗？（需要Webview2组件才能显示网页登录界面）", App.DisplayName, MessageBoxButton.YesNo, MessageBoxImage.Question);
    //        if (result == MessageBoxResult.Yes)
    //        {
    //            "https://go.microsoft.com/fwlink/p/?LinkId=2124703".GoUrl();

    //        }
    //        Ex.Log(ex);
    //        Close();
    //    }
    //}
    

    public async Task<HtmlDocument> GetHtmlAsync(string api, Pairs parapairs = null, bool showSearchMessage = true, CancellationToken token = default)
    {
        var query = parapairs.ToPairsString();
        var doc = new HtmlDocument();
        try
        {
            //HttpClientHandler.AutomaticDecompression = DecompressionMethods.All;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var response = await GetAsync($"{api}{query}", showSearchMessage, token:token);
            var strHTML = string.Empty;
            //if (true)
            //{
            //    var stm = response.Content.ReadAsStreamAsync(token).Result;
                
            //    var gzip = new GZipStream(stm, CompressionMode.Decompress);//解压缩
            //    using StreamReader reader = new StreamReader(gzip, Encoding.UTF8);
            //    strHTML = await reader.ReadToEndAsync();
            //}
            var s = (await response.Content.ReadAsStringAsync(token));
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

    public async Task<XmlDocument> GetXmlAsync(string api, Pairs pairs = null, bool showSearchMessage = true, CancellationToken token = default)
    {
        var query = pairs.ToPairsString();
        var xml = new XmlDocument();
        try
        {
            var response = await GetAsync($"{api}{query}", showSearchMessage, token: token);
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

    public async Task<XDocument> GetXDocAsync(string api, Pairs pairs = null, bool showSearchMessage = true,bool tryWebView = false,
        CancellationToken token = default)
    {
        var query = pairs.ToPairsString();
        XDocument xml;
        try
        {
            var response = await GetAsync($"{api}{query}", showSearchMessage, token: token);
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