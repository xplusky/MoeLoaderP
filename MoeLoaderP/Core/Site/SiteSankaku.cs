using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using MoeLoader.Core.Sites;

namespace MoeLoader.Core.Site
{
    public class SiteSankaku : MoeSite
    {
        private SiteBooru _booru;
        private readonly MoeSession _sweb = new MoeSession();
        private readonly SessionHeadersCollection _shc = new SessionHeadersCollection();
        private readonly Random _rand = new Random();
        private readonly string[] _user = { "girltmp", "mload006", "mload107", "mload482", "mload367", "mload876", "mload652", "mload740", "mload453", "mload263", "mload395" };
        private readonly string[] _pass = { "girlis2018", "moel006", "moel107", "moel482", "moel367", "moel876", "moel652", "moel740", "moel453", "moel263", "moel395" };
        private string  _tempuser, _temppass, _tempappkey, _ua, _pageurl;
        private static string _cookie = "";

        public override string HomeUrl => $"https://{SitePrefix}.sankakucomplex.com";
        public override string DisplayName => "sankakucomplex.com";
        public override string ShortName => "sankakucomplex";
        public override string Referer => $"{HomeUrl}/post/show/12345";
        public virtual string SubReferer => "*";

        /// <summary>
        /// sankakucomplex site
        /// </summary>
        public SiteSankaku()
        {
            CookieRestore();
            _shc.Timeout = 16000;
            SurpportState.IsSupportScore = false;

            SubMenu.Add("chan");
            SubMenu.Add("idol");
        }

        public string  SitePrefix => SubListIndex == 0 ? "chan" : "idol";

        /// <summary>
        /// 取页面源码 来自官方APP处理方式
        /// </summary>
        /// <param name="page"></param>
        /// <param name="count"></param>
        /// <param name="keyWord"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            if (SitePrefix == "chan")
            {
                _ua = "SCChannelApp/2.4 (Android; black)";
            }
            else if (SitePrefix == "idol")
            {
                _ua = "SCChannelApp/2.3 (Android; idol)";
            }
            else return null;

            Login(proxy);
            return _booru.GetPageString(page, count, keyWord, proxy);
        }

        public override List<ImageItem> GetImages(string pageString, IWebProxy proxy)
        {
            return _booru.GetImages(pageString, proxy);
        }
        
        public override List<AutoHintItem> GetAutoHintItems(string word, IWebProxy proxy)
        {
            var re = new List<AutoHintItem>();

            //https://chan.sankakucomplex.com/tag/autosuggest?tag=*****&locale=en
            var url = string.Format(HomeUrl + "/tag/autosuggest?tag={0}", word);
            _shc.ContentType = SessionHeadersValue.AcceptAppJson;
            var json = _sweb.Get(url, proxy, _shc);
            var array = (new JavaScriptSerializer()).DeserializeObject(json) as object[];

            if (array.Count() > 1)
            {
                if (array[1].GetType().FullName.Contains("Object[]"))
                {
                    var i = 2;
                    foreach (var names in array[1] as object[])
                    {
                        var name = names.ToString();
                        var count = array[i].ToString();
                        i++;
                        re.Add(new AutoHintItem() { Word = name, Count = count });
                    }
                }
            }

            return re;
        }
        
        /// <summary>
        /// 还原Cookie
        /// </summary>
        private void CookieRestore()
        {
            if (!string.IsNullOrWhiteSpace(_cookie)) return;

            var ck = _sweb.GetURLCookies(HomeUrl);
            if (!string.IsNullOrWhiteSpace(ck))
                _cookie = ck;
        }

        /// <summary>
        /// 这破站用API需要登录！(╯‵□′)╯︵┻━┻
        /// 两个图站的账号还不通用(╯‵□′)╯︵┻━┻
        /// </summary>
        /// <param name="proxy"></param>
        private void Login(IWebProxy proxy)
        {
            var subdomain = SitePrefix.Substring(0, 1) ;

            subdomain += subdomain.Contains("c") ? "api-beta" : "api";
            var loginhost = $"https://{subdomain}.sankakucomplex.com";

            if (!_cookie.Contains(subdomain + ".sankaku"))
            {
                try
                {
                    _cookie = "";
                    var index = _rand.Next(0, _user.Length);
                    _tempuser = _user[index];
                    _temppass = GetSankakuPwHash(_pass[index]);
                    _tempappkey = GetSankakuAppkey(_tempuser);
                    var post = "";

                    if (subdomain.Contains("capi"))
                        post = "user[name]=" + _tempuser + "&user[password]=" + _pass[index] + "&appkey=" + _tempappkey;
                    else
                        post = "login=" + _tempuser + "&password_hash=" + _temppass + "&appkey=" + _tempappkey;

                    //Post登录取Cookie
                    _shc.UserAgent = _ua;
                    _shc.Referer = Referer;
                    _shc.Accept = SessionHeadersValue.AcceptAppJson;
                    _shc.ContentType = SessionHeadersValue.ContentTypeFormUrlencoded;
                    _sweb.Post( $"{loginhost}/user/authenticate.json", post, proxy, _shc);
                    _cookie = _sweb.GetURLCookies(loginhost);

                    if (SitePrefix == "idol" && !_cookie.Contains("sankakucomplex_session"))
                        throw new Exception("获取登录Cookie失败");
                    else
                        _cookie = subdomain + ".sankaku;" + _cookie;

                    _pageurl = $"{loginhost}/post/index.json?login={_tempuser}&password_hash={_temppass}&appkey={_tempappkey}&page={{0}}&limit={{1}}&tags={{2}}";

                    //登录成功才能初始化Booru类型站点
                    _shc.Referer = Referer;
                    _booru = new SiteBooru(HomeUrl, _pageurl, null, DisplayName, ShortName, false, BooruProcessor.SourceType.JSONSku, _shc);
                }
                catch (Exception e)
                {
                    throw new Exception($"自动登录失败: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 计算用于登录等账号操作的AppKey
        /// </summary>
        /// <param name="user">用户名</param>
        /// <returns></returns>
        private static string GetSankakuAppkey(string user)
        {
            return Sha1($"sankakuapp_{user.ToLower()}_Z5NE9YASej", Encoding.Default).ToLower();
        }

        /// <summary>
        /// 计算密码sha1
        /// </summary>
        /// <param name="password">密码</param>
        /// <returns></returns>
        private static string GetSankakuPwHash(string password)
        {
            return Sha1($"choujin-steiner--{password}--", Encoding.Default).ToLower();
        }

        /// <summary>
        /// SHA1加密
        /// </summary>
        /// <param name="content">字符串</param>
        /// <param name="encode">编码</param>
        /// <returns></returns>
        private static string Sha1(string content, Encoding encode)
        {
            try
            {
                System.Security.Cryptography.SHA1 sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                var bytesIn = encode.GetBytes(content);
                var bytesOut = sha1.ComputeHash(bytesIn);
                var result = BitConverter.ToString(bytesOut);
                result = result.Replace("-", "");
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("SHA1Error:" + ex.Message);
            }
        }
    }
}
