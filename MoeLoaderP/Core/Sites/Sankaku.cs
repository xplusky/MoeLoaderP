using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MoeLoader.Core.Site;

namespace MoeLoader.Core.Sites
{
    public class Sankaku : BooruSite
    {
        public override string HomeUrl => $"https://{SitePrefix}.sankakucomplex.com";

        public override string DisplayName => "sankakucomplex.com new";

        public override string ShortName => "sankakucomplex";

        public override string Referer => $"{HomeUrl}/post/show/12345";

        public string SitePrefix => SubListIndex == 0 ? "chan" : "idol";

        public override bool NeedLogin => true;

        public Sankaku(bool isxmode)
        {
            SubMenu.Add("chan");
            if (isxmode) SubMenu.Add("idol");
        }

        public override string GetHintQuery(SearchPara para) => $"{HomeUrl}/tag/autosuggest?tag={para.Keyword}";

        public override string GetPageQuery(SearchPara para) 
        {
            return $"{HomeUrl}/post/index.json?login={_tempuser}&password_hash={_temppass}&appkey={_tempappkey}&page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";
        }


        private string _tempuser, _temppass, _tempappkey, _ua, _pageurl, _cookie = "";

        public override async Task LoginAsync()
        {
            if (SitePrefix == "chan")
            {
                _ua = "SCChannelApp/2.4 (Android; black)";
            }
            else if (SitePrefix == "idol")
            {
                _ua = "SCChannelApp/2.3 (Android; idol)";
            }

            var subdomain = SitePrefix.Substring(0, 1);

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
                    FormUrlEncodedContent content;
                    if (subdomain.Contains("capi"))
                        content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            {"user[name]", _tempuser},
                            {"user[password]", _pass[index]},
                            {"appkey", _tempappkey}
                        });
                    else
                    content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        {"login", _tempuser},
                        {"password_hash", _temppass},
                        {"appkey", _tempappkey}
                    });
                    //Post登录取Cookie
                    var net = new MoeNet(Settings);
                    var client = net.Client;
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(_ua);
                    client.DefaultRequestHeaders.Referrer = new Uri(Referer);
                    //_shc.Accept = SessionHeadersValue.AcceptAppJson;
                    //_shc.ContentType = SessionHeadersValue.ContentTypeFormUrlencoded;

                    var respose = await client.PostAsync(new Uri($"{loginhost}/user/authenticate.json"), content);
                    _cookie = net.HttpClientHandler.CookieContainer.GetCookieHeader(new Uri(loginhost));

                    if (SitePrefix == "idol" && !_cookie.Contains("sankakucomplex_session"))
                        throw new Exception("获取登录Cookie失败");
                    else
                        _cookie = subdomain + ".sankaku;" + _cookie;

                    _pageurl = $"{loginhost}/post/index.json?login={_tempuser}&password_hash={_temppass}&appkey={_tempappkey}&page={{0}}&limit={{1}}&tags={{2}}";

                    //登录成功才能初始化Booru类型站点
                    IsLogin = true;
                }
                catch (Exception e)
                {
                    throw new Exception($"自动登录失败: {e.Message}");
                }
            }
        }

        private readonly Random _rand = new Random();
        private readonly string[] _user = { "girltmp", "mload006", "mload107", "mload482", "mload367", "mload876", "mload652", "mload740", "mload453", "mload263", "mload395" };
        private readonly string[] _pass = { "girlis2018", "moel006", "moel107", "moel482", "moel367", "moel876", "moel652", "moel740", "moel453", "moel263", "moel395" };


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
