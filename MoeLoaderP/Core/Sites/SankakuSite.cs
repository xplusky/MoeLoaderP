using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// sankakucomplex.com fixed 20180924
    /// </summary>
    public class SankakuSite  : MoeSite
    {
        public override string HomeUrl => $"https://{SitePrefix}.sankakucomplex.com";

        public override string DisplayName => "Sankakucomplex";

        public override string ShortName => "sankakucomplex";

        public string Referer => $"{HomeUrl}/post/show/12345";

        public string SitePrefix => SubListIndex == 0 ? "chan" : "idol";

        public override NetSwap Net
        {
            get => SubListIndex == 0 ? _chanNet : _idolNet;
            set => base.Net = value;
        }

        private NetSwap _chanNet, _idolNet;
        private bool _isChanLogin, _isIdolLogin;
        private string _chanQuery, _idolQuery;

        public SankakuSite(bool isxmode)
        {
            SubMenu.Add("chan");
            if (isxmode) SubMenu.Add("idol");
        }

        public async Task LoginAsync(string channel)
        {
            if (channel == "chan")
            {
                _chanNet = new NetSwap(Settings, HomeUrl);
                const string loginhost = "https://capi-beta.sankakucomplex.com";
                var accountIndex = new Random().Next(0, _user.Length);
                var tempuser = _user[accountIndex];
                var temppass = GetSankakuPwHash(_pass[accountIndex]);
                var tempappkey = GetSankakuAppkey(tempuser);
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"user[name]", tempuser},
                    {"user[password]", _pass[accountIndex]},
                    {"appkey", tempappkey}
                });
                _chanNet.HttpClientHandler.UseCookies = true;
                var client = _chanNet.Client;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SCChannelApp/2.4 (Android; black)");
                client.DefaultRequestHeaders.Referrer = new Uri(Referer);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                var respose = await client.PostAsync(new Uri($"{loginhost}/user/authenticate.json"), content);
                if (respose.IsSuccessStatusCode)
                {
                    _isChanLogin = true;
                }
                else
                {
                    MessageBox.Show("chan登陆失败");
                }
                _chanQuery = $"{loginhost}/post/index.json?login={tempuser}&password_hash={temppass}&appkey={tempappkey}&";
                // login ok
            }
            if (channel == "idol")
            {
                _idolNet = new NetSwap(Settings, HomeUrl);
                const string loginhost = "https://iapi.sankakucomplex.com";
                var accountIndex = new Random().Next(0, _user.Length);
                var tempuser = _user[accountIndex];
                var temppass = GetSankakuPwHash(_pass[accountIndex]);
                var tempappkey = GetSankakuAppkey(tempuser);
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"login", tempuser},
                    {"password_hash", temppass},
                    {"appkey", tempappkey}
                });
                _idolNet.HttpClientHandler.UseCookies = true;
                var client = _idolNet.Client;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SCChannelApp/2.3 (Android; idol)");
                client.DefaultRequestHeaders.Referrer = new Uri(Referer);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                var respose = await client.PostAsync(new Uri($"{loginhost}/user/authenticate.json"), content);
                if (respose.IsSuccessStatusCode)
                {
                    _isIdolLogin = true;
                }
                else
                {
                    MessageBox.Show("idol登陆失败");
                }
                _idolQuery = $"{loginhost}/post/index.json?login={tempuser}&password_hash={temppass}&appkey={tempappkey}&";
            }
        }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            var channel = SitePrefix;
            string query;
            HttpClient client;
            switch (channel)
            {
                default:
                case "chan":
                {
                    if (!_isChanLogin) await LoginAsync(channel);
                    if (!_isChanLogin) return new ImageItems();
                    query = $"{_chanQuery}page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";
                    client = _chanNet.Client;
                    break;
                    }
                case "idol":
                {
                    if (!_isIdolLogin) await LoginAsync(channel);
                    if (!_isIdolLogin) return new ImageItems();
                    query = $"{_idolQuery}page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";
                    client = _idolNet.Client;
                    break;
                }
            }

            // get images
            var response = await client.GetAsync(query, token);
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(response.ReasonPhrase);
            }
            var jsonStr = await response.Content.ReadAsStringAsync();
            dynamic list = JsonConvert.DeserializeObject(jsonStr);
            var imageitems = new ImageItems();
            if (list == null) return imageitems;
            var https = "https:";
            foreach (var item in list)
            {
                var img = new ImageItem();
                img.ThumbnailUrl = $"{https}{item.preview_url}";
                img.FileUrl = $"{https}{item.file_url}";
                img.Width = (int)item.width;
                img.Height = (int)item.height;
                img.Id = (int)item.id;
                img.Score = (int)item.fav_count;
                img.Author = $"{item.uploader_name}";
                foreach (var tag in item.tags)
                {
                    img.Tags.Add($"{tag.name}");
                }
                img.IsExplicit = $"{item.rating}" == "e";
                img.Site = this;
                img.Net = Net;
                img.ThumbnailReferer = Referer;

                imageitems.Add(img);
            }

            return imageitems;
        }

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
