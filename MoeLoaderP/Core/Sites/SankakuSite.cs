using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// sankakucomplex.com fixed 20181001
    /// </summary>
    public class SankakuSite  : MoeSite
    {
        public override string HomeUrl => $"https://{SitePrefix}.sankakucomplex.com";

        public override string DisplayName => "SankakuComplex";

        public override string ShortName => "sankakucomplex";


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
            SubMenu.Add("Chan");
            if (isxmode) SubMenu.Add("Idol");
            DownloadTypes.Add("原图", 4);
        }

        public async Task LoginAsync(string channel, CancellationToken token)
        {
            if (channel == "chan")
            {
                _chanNet = new NetSwap(Settings, HomeUrl);
                const string loginhost = "https://capi-beta.sankakucomplex.com";
                //var accountIndex = new Random().Next(0, _user.Length);
                //var tempuser = _user[accountIndex];
                //var temppass = GetSankakuPwHash(_pass[accountIndex]);
                //var tempappkey = GetSankakuAppkey(tempuser);
                //var content = new FormUrlEncodedContent(new Dictionary<string, string>
                //{
                //    {"user[name]", tempuser},
                //    {"user[password]", _pass[accountIndex]},
                //    {"appkey", tempappkey}
                //});
                _chanNet.HttpClientHandler.UseCookies = true;
                var client = _chanNet.Client;
                //client.DefaultRequestHeaders.UserAgent.ParseAdd("SCChannelApp/2.4 (Android; black)");
                client.DefaultRequestHeaders.Referrer = new Uri(HomeUrl);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                //var respose = await client.PostAsync(new Uri($"{loginhost}/user/authenticate.json"), content, token);
                //if (respose.IsSuccessStatusCode)
                //{
                //    _isChanLogin = true;
                //}
                //else
                //{
                //    MessageBox.Show("chan登陆失败");
                //}
                _chanQuery = $"{loginhost}/";
                // login ok

                _isChanLogin = true;
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
                client.DefaultRequestHeaders.Referrer = new Uri(HomeUrl);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                var respose = await client.PostAsync(new Uri($"{loginhost}/user/authenticate.json"), content, token);
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

        public async Task GetDetailAction(string pageUrl, ImageItem img, NetSwap net, SearchPara para)
        {
            try
            {
                var subpage = await net.Client.GetStringAsync(pageUrl);
                var subdoc = new HtmlDocument();
                subdoc.LoadHtml(subpage);
                var imgNode = subdoc.DocumentNode.SelectSingleNode("//a[@id=\"image-link\"]");
                var url = $"https:{imgNode.Attributes["href"].Value}";
                img.Urls.Add(new UrlInfo("中图",4, url));
                img.Net = net.CreatNewWithRelatedCookie();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            var channel = SitePrefix;
            string query;
            HttpClient client;

            if (channel == "chan")
            {
                if (!_isChanLogin) await LoginAsync(channel, token);
                if (!_isChanLogin) return new ImageItems();
                //query = $"{_chanQuery}page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";
                query = $"{_chanQuery}post/index.content?page={para.PageIndex}";
                if (!string.IsNullOrWhiteSpace(para.Keyword))
                {
                    query = $"{_chanQuery}post/index.content?tags={para.Keyword}&page={para.PageIndex}";
                }
                client = _chanNet.Client;
                var response = await client.GetAsync(query, token);
                var text = await response.Content.ReadAsStringAsync();
                var list = new ImageItems();

                var dococument = new HtmlDocument();
                dococument.LoadHtml(text);
                var imageItems = dococument.DocumentNode.SelectNodes("//span");
                foreach (var item in imageItems)
                {
                    var img = new ImageItem(this,para);

                    int.TryParse(item.Attributes["id"]?.Value.Replace("p",""),out var idr);
                    img.Id = idr;
                    var anode = item.SelectSingleNode("a");
                    //https://capi-beta.sankakucomplex.com
                    img.DetailUrl = $"https://capi-beta.sankakucomplex.com{anode.Attributes["href"]?.Value}";
                    var imgNode = item.SelectSingleNode("a/img");
                    var detail = imgNode.Attributes["title"].Value;
                    if (!string.IsNullOrWhiteSpace(detail))
                    {
                        foreach (var i in detail.Split(' '))
                        {
                            if (i.Contains(":"))
                            {
                                var parts = i.Split(':');
                                switch (parts[0])
                                {
                                    case "Rating":
                                    {
                                        if (parts[1] != "Safe") img.IsExplicit = true;
                                        break;
                                    }
                                    case "Score":
                                    {
                                        if(double.TryParse(parts[1], out var scroe)) img.Score = scroe;
                                        break;
                                    }
                                    case "User":
                                    {
                                        img.Author = parts[1];
                                        break;
                                    }
                                    case "Size":
                                    {
                                        if(!parts[1].Contains("x"))break;
                                        var size = parts[1].Split('x');
                                        int.TryParse(size[0], out var width);
                                        int.TryParse(size[1], out var height);
                                        img.Width = width;
                                        img.Height = height;
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                img.Tags.Add(i);
                            }
                        }
                    }
                    img.Urls.Add(new UrlInfo("缩略图", 1, $"https:{imgNode.Attributes["src"]?.Value}", query));
                    img.GetDetailAction = async () => await GetDetailAction(img.DetailUrl, img, Net.CreatNewWithRelatedCookie(), para);
                    list.Add(img);
                }
                return list;

            }
            else if (channel == "idol")
            {
                if (!_isIdolLogin) await LoginAsync(channel, token);
                if (!_isIdolLogin) return new ImageItems();
                query = $"{_idolQuery}page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";
                client = _idolNet.Client;
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
                    var img = new ImageItem(this, para);
                    img.Width = (int)item.width;
                    img.Height = (int)item.height;
                    img.Id = (int)item.id;
                    img.Score = (int)item.fav_count;
                    img.Author = $"{item.uploader_name}";
                    img.DetailUrl = $"{HomeUrl}/post/show/{img.Id}";
                    foreach (var tag in item.tags)
                    {
                        img.Tags.Add($"{tag.name}");
                    }
                    img.IsExplicit = $"{item.rating}" == "e";
                    img.Net = Net.CreatNewWithRelatedCookie();
                    img.Urls.Add(new UrlInfo("缩略图", 1, $"{https}{item.preview_url}", img.DetailUrl));
                    img.Urls.Add(new UrlInfo("原图", 4, $"{https}{item.file_url}", img.DetailUrl));
                    imageitems.Add(img);
                }

                return imageitems;
            }
            else
                return null;
            // get images
            
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
