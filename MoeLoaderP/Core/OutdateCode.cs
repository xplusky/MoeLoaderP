using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Media;
using MoeLoader.Core.Sites;

// 过时代码，逐步移除
namespace MoeLoader.Core
{
    /// <summary>
    /// session方式的HttpWeb连接
    /// </summary>
    public class MoeSession
    {
        private static CookieContainer m_Cookie = new CookieContainer();

        private static string[] UAs = {
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.21 (KHTML, like Gecko) Chrome/53.0.1271.64 Safari/537.21",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.117 Safari/537.36",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 11_0_2 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Mobile/15A421",
            };

        /// <summary>
        /// 提供UA
        /// </summary>
        public static string DefUA { get; } = UAs[new Random().Next(0, UAs.Length - 1)];

        public MoeSession()
        {
            ServicePointManager.DefaultConnectionLimit = 60;
        }
        //#############################   Header   #################################################
        private HttpWebRequest SetHeader(HttpWebRequest request, string url, IWebProxy proxy, SessionHeadersCollection shc)
        {
            request.Headers = shc;
            request.Proxy = proxy;
            request.Accept = shc.Accept;
            request.Referer = shc.Referer;
            request.Timeout = shc.Timeout;
            request.KeepAlive = shc.KeepAlive;
            request.UserAgent = shc.UserAgent;
            request.CookieContainer = m_Cookie;
            request.AllowAutoRedirect = shc.AllowAutoRedirect;
            request.AutomaticDecompression = shc.AutomaticDecompression;
            request.ContentType = shc.ContentType.Contains("auto") ? MimeMapping.GetMimeMapping(url) : shc.ContentType;
            request.ServicePoint.Expect100Continue = false;
            request.ServicePoint.UseNagleAlgorithm = false;
            request.ServicePoint.ConnectionLimit = int.MaxValue;

            return request;
        }
        //##############################################################################
        //#############################   GET   #################################################

        /// <summary>
        /// Get访问
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <param name="shc">Headers</param>
        /// <returns>网页内容</returns>
        public string Get(string url, IWebProxy proxy, string pageEncoding, SessionHeadersCollection shc)
        {
            string ret = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse reponse = null;
            try
            {
                SetHeader(request, url, proxy, shc);

                reponse = (HttpWebResponse)request.GetResponse();
                m_Cookie = request.CookieContainer;
                Stream rspStream = reponse.GetResponseStream();
                StreamReader sr = new StreamReader(rspStream, Encoding.GetEncoding(pageEncoding));
                ret = sr.ReadToEnd();
                sr.Close();
                rspStream.Close();
            }
            catch (Exception e)
            {
                ret = e.Message;
            }
            finally
            {
                if (reponse != null)
                {
                    reponse.Close();
                }
            }
            return ret;
        }

        /// <summary>
        /// Get访问,默认UTF-8编码
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="shc">Headers</param>
        /// <returns>网页内容</returns>
        public string Get(string url, IWebProxy proxy, SessionHeadersCollection shc)
        {
            return Get(url, proxy, "UTF-8", shc);
        }

        /// <summary>
        /// Get Response 取回响应, Please use Close()
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="rwtimeout">读写流超时ReadWriteTimeout</param>
        /// <param name="shc">Headers</param>
        /// <returns>WebResponse</returns>
        public WebResponse GetWebResponse(string url, IWebProxy proxy, int rwtimeout, SessionHeadersCollection shc)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            WebResponse reponse = null;
            try
            {
                SetHeader(request, url, proxy, shc);

                request.CookieContainer = m_Cookie;
                request.ReadWriteTimeout = rwtimeout;
                reponse = request.GetResponse();
                m_Cookie = request.CookieContainer;
            }
            catch { }
            return reponse;
        }
        /// <summary>
        /// Get Response 取回响应 Timeout 20s, Please use Close()
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="referer">来源</param>
        /// <returns>WebResponse</returns>
        public WebResponse GetWebResponse(string url, IWebProxy proxy, string referer)
        {
            SessionHeadersCollection shc = new SessionHeadersCollection();
            shc.Referer = referer;
            shc.Timeout = 20000;
            shc.ContentType = SessionHeadersValue.ContentTypeAuto;
            return GetWebResponse(url, proxy, shc.Timeout, shc);
        }

        //########################################################################################

        //#############################   POST   #################################################

        /// <summary>
        /// Post访问,默认UTF-8编码
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="postData">Post数据</param>
        /// <param name="proxy">代理</param>
        /// <param name="shc">Headers</param>
        public string Post(string url, string postData, IWebProxy proxy, SessionHeadersCollection shc)
        {
            return Post(url, postData, proxy, "UTF-8", shc);
        }

        /// <summary>
        /// Post访问
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="postData">Post数据</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <param name="shc">Headers</param>
        /// <returns></returns>
        public string Post(string url, string postData, IWebProxy proxy, string pageEncoding, SessionHeadersCollection shc)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            HttpWebResponse response;

            byte[] bytesToPost = Encoding.GetEncoding(pageEncoding).GetBytes(postData);
            try
            {
                SetHeader(request, url, proxy, shc);

                request.Method = "POST";
                request.CookieContainer = m_Cookie;//设置上次访问页面的Cookie 保持Session
                request.ContentLength = bytesToPost.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bytesToPost, 0, bytesToPost.Length);//写入Post数据
                requestStream.Close();

                response = (HttpWebResponse)request.GetResponse();
                m_Cookie = request.CookieContainer;//访问后更新Cookie
                Stream responseStream = response.GetResponseStream();
                string resData = "";

                using (StreamReader resSR = new StreamReader(responseStream, Encoding.GetEncoding(pageEncoding)))
                {
                    resData = resSR.ReadToEnd();
                    resSR.Close();
                    responseStream.Close();
                }
                return resData;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        //########################################################################################
        //#############################   Cookies   #################################################

        /// <summary>
        /// 取此CookieContainer中指定站点Cookies
        /// </summary>
        /// <param name="url">域名</param>
        /// <returns></returns>
        public string GetURLCookies(string url)
        {
            return m_Cookie.GetCookieHeader(new Uri(url));
        }
    }

    //########################################################################################
    //#############################   Class   #################################################
    /// <summary>
    /// Provide some header value
    /// </summary>
    public static class SessionHeadersValue
    {
        /// <summary>
        /// text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8
        /// </summary>
        public static string AcceptDefault = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";

        /// <summary>
        /// text/html
        /// </summary>
        public static string AcceptTextHtml = "text/html";

        /// <summary>
        /// text/xml
        /// </summary>
        public static string AcceptTextXml = "text/xml";

        /// <summary>
        /// application/json
        /// </summary>
        public static string AcceptAppJson = "application/json";

        /// <summary>
        /// application/xml
        /// </summary>
        public static string AcceptAppXml = "application/xml";

        /// <summary>
        /// gzip, deflate
        /// </summary>
        public static string AcceptEncodingGzip = "gzip, deflate";

        /// <summary>
        /// Automatic recognition
        /// </summary>
        public static string ContentTypeAuto = "auto";

        /// <summary>
        /// application/x-www-form-urlencoded
        /// </summary>
        public static string ContentTypeFormUrlencoded = "application/x-www-form-urlencoded";

        /// <summary>
        /// multipart/form-data
        /// </summary>
        public static string ContentTypeFormData = "multipart/form-data";
    }

    /// <summary>
    ///  The Ready HeaderCollection Class, 可以直接设置一些常用的Header值
    /// </summary>
    public class SessionHeadersCollection : WebHeaderCollection
    {
        public SessionHeadersCollection()
        {
            Accept = SessionHeadersValue.AcceptDefault;
            AcceptEncoding = null;
            AcceptLanguage = "zh-CN,zh,zh-TW;q=0.7,en,*;q=0.5";
            AllowAutoRedirect = true;
            AutomaticDecompression = DecompressionMethods.None;
            ContentType = SessionHeadersValue.ContentTypeFormUrlencoded;
            KeepAlive = false;
            Referer = null;
            Timeout = 9000;
            UserAgent = MoeSession.DefUA;
        }

        /// <summary>
        /// SessionHeadersValue.AcceptDefault
        /// </summary>
        public string Accept { get; set; }

        /// <summary>
        /// Null
        /// </summary>
        public string AcceptEncoding
        {
            get => Get("Accept-Encoding");
            set => Set(HttpRequestHeader.AcceptEncoding, value);
        }

        /// <summary>
        /// zh-CN,zh,zh-TW;q=0.7,en,*;q=0.5
        /// </summary>
        public string AcceptLanguage
        {
            get => Get("Accept-Language");
            set => Set(HttpRequestHeader.AcceptLanguage, value);
        }

        /// <summary>
        /// True 跟随重定向
        /// </summary>
        public bool AllowAutoRedirect { get; set; }

        /// <summary>
        /// None 压缩类型
        /// </summary>
        public DecompressionMethods AutomaticDecompression { get; set; }

        /// <summary>
        /// x-www-form-urlencoded, Use SessionHeadersValue class
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        ///  False
        /// </summary>
        public bool KeepAlive { get; set; }

        /// <summary>
        /// 引用页
        /// </summary>
        public string Referer { get; set; }

        /// <summary>
        /// 9000
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// UA
        /// </summary>
        public string UserAgent { get; set; }
    }

    /// <summary>
    /// 带有超时的WebClient
    /// </summary>
    public class MyWebClient : WebClient
    {
        //private Calculagraph _timer;
        private int _timeOut = 25;

        /// <summary>
        /// 构造WebClient
        /// </summary>
        public MyWebClient()
        {
            Headers["User-Agent"] = MoeSession.DefUA;
            ServicePointManager.DefaultConnectionLimit = 60;
        }

        /// <summary>
        /// 过期时间 in second
        /// </summary>
        public int Timeout
        {
            get => _timeOut;
            set
            {
                if (value < 1)
                    _timeOut = 10;
                _timeOut = value;
            }
        }

        /// <summary>
        /// 重写GetWebRequest，添加WebRequest对象超时时间
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.Timeout = 1000 * Timeout;
            request.ReadWriteTimeout = 1000 * Timeout;
            request.ServicePoint.Expect100Continue = false;
            request.ServicePoint.UseNagleAlgorithm = false;
            request.ServicePoint.ConnectionLimit = int.MaxValue;
            return request;
        }

    }
    
    public class PreLoader
    {
        public PreLoader(IWebProxy proxy)
        {
            Proxy = proxy;

        }
        private IWebProxy Proxy { get; set; }

        private static int cacheimgcount = 6;
        /// <summary>
        /// 缓存的图片数量,最大20,最少1
        /// </summary>
        public static int CachedImgCount
        {
            get => cacheimgcount;
            set
            {
                if (value > 20) value = 20;
                else if (value < 1) value = 1;

                cacheimgcount = value;
            }
        }

        //预先加载的url
        private int prePage, preCount;
        private string preWord;
        private MoeSite preSite;
        private string preFetchedPage;
        //预加载的页面内容
        public string GetPreFetchedPage(int page, int count, string word, MoeSite site)
        {
            if (page == prePage && count == preCount && word == preWord && site == preSite)
            {
                return preFetchedPage;
            }
            else return null;
        }

        //预加载的缩略图
        private Dictionary<string, ImageSource> preFetchedImg = new Dictionary<string, ImageSource>(CachedImgCount);

        /// <summary>
        /// 预加载的缩略图
        /// </summary>
        /// <param name="url">缩略图url</param>
        /// <returns>缩略图，或者 null 若未加载</returns>
        public ImageSource PreFetchedImg(string url)
        {
            if (preFetchedImg.ContainsKey(url))
                return preFetchedImg[url];
            else return null;
        }

        //private System.Net.HttpWebRequest req;
        private List<HttpWebRequest> imgReqs = new List<HttpWebRequest>(CachedImgCount);

        /// <summary>
        /// 反馈图片列表预加载完成事件,用于判断是否有下一页
        /// return imgCount
        /// </summary>
        public event EventHandler PreListLoaded;

        /// <summary>
        /// do in a separate thread
        /// 下载缩略图线程
        /// </summary>
        /// <param name="page"></param>
        /// <param name="count"></param>
        /// <param name="word"></param>
        public void PreFetchPage(int page, int count, string word, MoeSite site)
        {
            (new Thread(new ThreadStart(() =>
            {
                try
                {
                    preFetchedPage = site.GetPageString(page, count, word, Proxy);
                    prePage = page;
                    preCount = count;
                    preWord = word;
                    preSite = site;
                    var imgs = site.GetImages(preFetchedPage, Proxy);

                    //获得所有图片列表后反馈得到的数量
                    PreListLoaded(imgs.Count, null);
                    if (imgs.Count < 1)
                        return;

                    var sweb = new MoeSession();
                    var shc = new SessionHeadersCollection();
                    shc.Accept = null;
                    shc.ContentType = SessionHeadersValue.ContentTypeAuto;
                    shc.Add("Accept-Ranges", "bytes");
                    shc.Referer = site.Referer;

                    // imgs = site.FilterImg(imgs, MainWindow.MainW.MaskInt, MainWindow.MainW.MaskRes,MainWindow.MainW.LastViewed, MainWindow.MainW.MaskViewed, true, false);

                    //预加载缩略图
                    foreach (var req1 in imgReqs)
                    {
                        req1?.Abort();
                    }
                    preFetchedImg.Clear();
                    imgReqs.Clear();

                    //prefetch one by one
                    var cacheCount = CachedImgCount < imgs.Count ? CachedImgCount : imgs.Count;
                    for (var i = 0; i < cacheCount; i++)
                    {
                        var res = sweb.GetWebResponse(imgs[i].PreviewUrl, Proxy, 9000, shc);
                        var str = res.GetResponseStream();

                        if (!preFetchedImg.ContainsKey(imgs[i].PreviewUrl))
                        {
                            // preFetchedImg.Add(imgs[i].PreviewUrl, MainWindow.MainW.CreateImageSrc(str));
                        }
                    }
                }
                catch
                {
                    //Console.WriteLine("useless");
                }
            }))).Start();
        }

        /// <summary>
        /// 异步下载结束
        /// </summary>
        /// <param name="req"></param>
        //private void RespCallback(IAsyncResult re)
        //{
        //    System.Net.WebResponse res = null;
        //    try
        //    {
        //        //res = req.EndGetResponse(re);
        //        System.IO.StreamReader sr = new System.IO.StreamReader(res.GetResponseStream(), Encoding.UTF8);
        //        //PreFetchedPage = sr.ReadToEnd();
        //        //PreFetchUrl = (string)re.AsyncState;

        //        //预加载缩略图
        //        foreach (System.Net.HttpWebRequest req1 in imgReqs)
        //        {
        //            if (req1 != null) req1.Abort();
        //        }
        //        //foreach (string key in preFetchedImg.Keys)
        //        //{
        //        //    preFetchedImg[key].Close();
        //        //}
        //        preFetchedImg.Clear();
        //        imgReqs.Clear();
        //        ImgSrcProcessor processor = new ImgSrcProcessor(MainWindow.MainW.MaskInt, MainWindow.MainW.MaskRes, PreFetchUrl, MainWindow.MainW.SrcType, MainWindow.MainW.LastViewed, MainWindow.MainW.MaskViewed);
        //        processor.processComplete += new EventHandler(processor_processComplete);
        //        processor.ProcessSingleLink();
        //    }
        //    catch (Exception ex)
        //    {
        //        //System.Windows.MessageBox.Show(ex.ToString());
        //    }
        //    finally
        //    {
        //        if (res != null)
        //            res.Close();
        //    }
        //}

        //void processor_processComplete(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (sender != null)
        //        {
        //            List<Img> imgs = sender as List<Img>;

        //            //prefetch one by one
        //            int count = CachedImgCount < imgs.Count ? CachedImgCount : imgs.Count;
        //            //System.Windows.MessageBox.Show(count.ToString());
        //            for (int i = 0; i < count; i++)
        //            {
        //                System.Net.HttpWebRequest req = System.Net.WebRequest.Create(imgs[i].PreUrl) as System.Net.HttpWebRequest;
        //                imgReqs.Add(req);
        //                req.Proxy = proxy;

        //                req.UserAgent = SessionClient.DefUA;
        //                req.Referer = MainWindow.IsNeedReferer(imgs[i].PreUrl);

        //                System.Net.WebResponse res = req.GetResponse();
        //                System.IO.Stream str = res.GetResponseStream();

        //                preFetchedImg.Add(imgs[i].PreUrl, MainWindow.MainW.CreateImageSrc(str));
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //System.Windows.MessageBox.Show(ex.ToString());
        //    }
        //}
    }
}
