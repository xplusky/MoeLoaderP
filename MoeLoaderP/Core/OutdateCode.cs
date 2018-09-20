using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MoeLoader.Core.Sites;

namespace MoeLoader.Core
{
    /// <summary>
    /// session方式的HttpWeb连接
    /// </summary>
    public class MoeSession
    {
        private static CookieContainer m_Cookie = new CookieContainer();

        private static string[] UAs = new string[]
        {
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.21 (KHTML, like Gecko) Chrome/53.0.1271.64 Safari/537.21",
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.117 Safari/537.36",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 11_0_2 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Mobile/15A421",
            };

        /// <summary>
        /// 提供UA
        /// </summary>
        public static string DefUA { get; } = UAs[new Random().Next(0, UAs.Length - 1)];

        /// <summary>
        /// Cookie集合
        /// </summary>
        public CookieContainer CookieContainer
        {
            get => m_Cookie;
            set => m_Cookie = value;
        }

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
        /// Get访问,便捷
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <returns>网页内容</returns>
        public string Get(string url, IWebProxy proxy, string pageEncoding)
        {
            return Get(url, proxy, pageEncoding, new SessionHeadersCollection());
        }

        /// <summary>
        /// Get访问,便捷,默认UTF-8编码
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <returns>网页内容</returns>
        public string Get(string url, IWebProxy proxy)
        {
            return Get(url, proxy, "UTF-8", new SessionHeadersCollection());
        }

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

        /// <summary>
        /// Create HttpWebRequest
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="proxy">代理</param>
        /// <param name="shc">Headers</param>
        /// <returns>HttpWebRequest</returns>
        public HttpWebRequest CreateWebRequest(string url, IWebProxy proxy, SessionHeadersCollection shc)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                SetHeader(request, url, proxy, shc);

                request.CookieContainer = m_Cookie;
                request.ReadWriteTimeout = 20000;
            }
            catch { }
            return request;
        }

        //########################################################################################

        //#############################   POST   #################################################
        /// <summary>
        /// Post访问,便捷
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="postData">Post数据</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        public string Post(string url, string postData, IWebProxy proxy, string pageEncoding)
        {
            return Post(url, postData, proxy, pageEncoding, new SessionHeadersCollection());
        }

        /// <summary>
        /// Post访问,自定义UA
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="postData">Post数据</param>
        /// <param name="proxy">代理</param>
        /// <param name="pageEncoding">编码</param>
        /// <param name="UA">User-Agent</param>
        public string Post(string url, string postData, IWebProxy proxy, string pageEncoding, string UA)
        {
            SessionHeadersCollection shc = new SessionHeadersCollection();
            shc.UserAgent = UA;
            return Post(url, postData, proxy, pageEncoding, shc);
        }

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
        /// 取CookieContainer中所有站点Cookies
        /// </summary>
        /// <returns>全部Cookie值</returns>
        public string GetAllCookies()
        {
            return _GetCookieValue(m_Cookie);
        }

        /// <summary>
        /// 取CookieContainer中所有站点Cookies 自定CookieContainer
        /// </summary>
        /// <param name="cc">CookieContainer</param>
        /// <returns></returns>
        public string GetAllCookies(CookieContainer cc)
        {
            return _GetCookieValue(cc);
        }

        /// <summary>
        /// 取此CookieContainer中指定站点Cookies
        /// </summary>
        /// <param name="url">域名</param>
        /// <returns></returns>
        public string GetURLCookies(string url)
        {
            return m_Cookie.GetCookieHeader(new Uri(url));
        }

        /// <summary>
        /// 取CookieContainer中指定站点Cookies 自定CookieContainer
        /// </summary>
        /// <param name="url">域名</param>
        /// <param name="cc">CookieContainer</param>
        /// <returns></returns>
        public string GetURLCookies(string url, CookieContainer cc)
        {
            return cc.GetCookieHeader(new Uri(url));
        }

        /// <summary>
        /// 取Cookie中键的值 当前访问的网站
        /// </summary>
        /// <param name="CookieKey">Cookie键</param>
        /// <returns>Cookie键对应值</returns>
        public string GetCookieValue(string CookieKey)
        {
            return _GetCookieValue(CookieKey, m_Cookie, 1);
        }

        /// <summary>
        /// 写出指定CookieContainer到文件
        /// </summary>
        /// <param name="file">文件保存路径</param>
        /// <param name="cc">CookieContainer</param>
        public static void WriteCookiesToFile(string file, CookieContainer cc)
        {
            using (Stream stream = File.Create(file))
            {
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, cc);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
        }
        /// <summary>
        /// 写出当前CookieContainer到文件
        /// </summary>
        /// <param name="file">文件保存路径</param>
        public static void WriteCookiesToFile(string file)
        {
            WriteCookiesToFile(file, m_Cookie);
        }

        /// <summary>
        /// 从文件读入Cookies
        /// </summary>
        /// <param name="file">Cookies文件</param>
        public static void ReadCookiesFromFile(string file)
        {
            try
            {
                using (Stream stream = File.Open(file, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    m_Cookie = (CookieContainer)formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// 取Cookie中键的值 自定CookieContainer
        /// </summary>
        /// <param name="CookieKey">Cookie键</param>
        /// <param name="cc">Cookie集合对象</param>
        /// <returns>Cookie键对应值</returns>
        public string GetCookieValue(string CookieKey, CookieContainer cc)
        {
            return _GetCookieValue(CookieKey, cc, 1);
        }

        /// <summary>
        /// 私有处理Cookie集合的方法 默认取全部Cookie值
        /// </summary>
        /// <param name="cc">Cookie集合对象</param>
        /// <returns></returns>
        private static string _GetCookieValue(CookieContainer cc)
        {
            return _GetCookieValue("", cc, 0);
        }

        /// <summary>
        /// 私有处理Cookie集合的方法
        /// </summary>
        /// <param name="CookieKey">Cookie键</param>
        /// <param name="cc">Cookie集合对象</param>
        /// <param name="mode">处理方式 0取所有站点全部值 1取指定键的值</param>
        /// <returns>Cookie对应值</returns>
        private static string _GetCookieValue(string CookieKey, CookieContainer cc, int mode)
        {
            try
            {
                List<Cookie> lstCookies = new List<Cookie>();
                System.Collections.Hashtable table = (System.Collections.Hashtable)cc.GetType().InvokeMember("m_domainTable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField |
                    System.Reflection.BindingFlags.Instance, null, cc, new object[] { });

                foreach (object pathList in table.Values)
                {
                    System.Collections.SortedList lstCookieCol = (System.Collections.SortedList)pathList.GetType().InvokeMember("m_list",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField
                        | System.Reflection.BindingFlags.Instance, null, pathList, new object[] { });
                    foreach (CookieCollection colCookies in lstCookieCol.Values)
                        foreach (Cookie c1 in colCookies) lstCookies.Add(c1);
                }

                string ret = "", uri = "";
                switch (mode)
                {
                    default:
                        foreach (Cookie cookie in lstCookies)
                        {
                            if (uri != cookie.Domain)
                            {
                                uri = cookie.Domain;
                                ret += string.IsNullOrWhiteSpace(ret) ? "" : "$";
                                ret += uri + ";";
                            }

                            ret += cookie.Name + "=" + cookie.Value + ";";

                        }
                        break;
                    case 1:
                        var model = lstCookies.Find(p => p.Name == CookieKey);
                        if (model != null)
                        {
                            ret = model.Value;
                        }
                        ret = string.Empty;
                        break;
                }
                return ret;
            }
            catch (Exception e)
            {
                return e.Message;
            }
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




    /*
     *  by YIU
     *  Last 180619
     */
    public class DataHelpers
    {
        [DllImport("shell32.dll")]
        public static extern int FindExecutableA(string lpFile, string lpDirectory, StringBuilder lpResult);

        /// <summary>
        /// 查找字节数组,失败未找到返回-1
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        public static int SearchBytes(byte[] bytes, byte[] search)
        {
            try
            {
                var i = bytes.Select((t, index) =>
                new { t = t, index = index }).FirstOrDefault(t =>
                bytes.Skip(t.index).Take(search.Length).SequenceEqual(search)).index;
                return i;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary> 
        /// MemoryStream 保存到文件
        /// </summary> 
        public static void MemoryStreamToFile(MemoryStream stream, string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(stream.ToArray());
            bw.Close();
            fs.Close();
        }

        #region image数据保存
        public enum ImageFormat { JPG, BMP, PNG, GIF }
        /// <summary>
        /// 将图片保存到文件
        /// </summary>
        /// <param name="bitmap">BitmapSource</param>
        /// <param name="format">图像类型</param>
        /// <param name="fileName">保存文件名</param>
        public static void ImageToFile(BitmapSource bitmap, ImageFormat format, string fileName)
        {
            BitmapEncoder encoder;

            switch (format)
            {
                case ImageFormat.JPG:
                    encoder = new JpegBitmapEncoder();
                    break;
                case ImageFormat.PNG:
                    encoder = new PngBitmapEncoder();
                    break;
                case ImageFormat.BMP:
                    encoder = new BmpBitmapEncoder();
                    break;
                case ImageFormat.GIF:
                    encoder = new GifBitmapEncoder();
                    break;
                default:
                    throw new InvalidOperationException();
            }

            FileStream fs = new FileStream(fileName, FileMode.Create);
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(fs);
            fs.Dispose();
            fs.Close();
        }

        /// <summary>
        /// 将图片保存到文件
        /// </summary>
        /// <param name="bitmap">BitmapSource</param>
        /// <param name="format">图像类型</param>
        /// <param name="fileName">保存文件名</param>
        public static void ImageToFile(BitmapSource bitmap, string format, string fileName)
        {
            ImageFormat ifo = ImageFormat.JPG;
            switch (format)
            {
                case "jpg":
                    break;
                case "png":
                    ifo = ImageFormat.PNG;
                    break;
                case "bmp":
                    ifo = ImageFormat.BMP;
                    break;
                case "gif":
                    ifo = ImageFormat.GIF;
                    break;
                default:
                    throw new Exception("ImageFormat incorrect type");
            }
            ImageToFile(bitmap, ifo, fileName);
        }
        #endregion

        /// <summary>
        /// 获取文件关联的程序
        /// </summary>
        /// <param name="extension">File extension,文件后缀</param>
        /// <returns></returns>
        public static string GetFileExecutable(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return null;

            string tmpFile = Path.GetTempPath() + "t." + extension;
            StringBuilder Executable = new StringBuilder(260);

            try
            {
                File.WriteAllBytes(tmpFile, new byte[] { });
                FindExecutableA(tmpFile, null, Executable);
                File.Delete(tmpFile);
                return Executable.ToString();
            }
            catch
            {
                File.Delete(tmpFile);
                return null;
            }

        }
    }


    public class DataConverter
    {
        /// <summary>
        /// 本地Stream转一段字节数组
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="length">指定转换长度</param>
        /// <returns></returns>
        public static byte[] LocalStreamToByte(Stream stream, long length)
        {
            byte[] bytes = new byte[length < 1 ? 1 : length];
            stream.Read(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);
            return bytes;
        }

        /// <summary>
        /// 十六进制字符串转字节数组
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] strHexToByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
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
