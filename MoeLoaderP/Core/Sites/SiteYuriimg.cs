using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// yuriimg.com
    /// Last change 180417
    /// </summary>
    class SiteYuriimg : MoeSite
    {
        private MoeSession Sweb = new MoeSession();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        private static string cookie = "";
        private string user = "mluser1";
        private string pass = "ml1yuri";
        public override string HomeUrl => "http://yuriimg.com";
        public override string ShortName => "yuriimg";
        public override string DisplayName => "yuriimg.com";
        public override string Referer => "http://yuriimg.com";
        public virtual string SubReferer => ShortName;

        public SiteYuriimg()
        {
            CookieRestore();
            SurpportState.IsSupportAutoHint = false;
        }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            Login(proxy);
            //http://yuriimg.com/post/?.html
            var url = HomeUrl + "/post/" + page + ".html";
            // string url = "http://yuriimg.com/show/ge407xd5o.jpg";

            if (keyWord.Length > 0)
            {
                //http://yuriimg.com/search/index/tags/?/p/?.html
                url = HomeUrl + "/search/index/tags/" + keyWord + "/p/" + page + ".html";
            }

            shc.Remove("Accept-Ranges");
            shc.Accept = SessionHeadersValue.AcceptTextHtml;
            shc.ContentType = SessionHeadersValue.AcceptTextHtml;
            var pageString = Sweb.Get(url, proxy, shc);

            return pageString;
        }

        public override List<ImageItem> GetImages(string pageString, IWebProxy proxy)
        {
            shc.Add("Accept-Ranges", "bytes");
            shc.ContentType = SessionHeadersValue.ContentTypeAuto;
            var list = new List<ImageItem>();

            var dococument = new HtmlDocument();
            dococument.LoadHtml(pageString);
            var imageItems = dococument.DocumentNode.SelectNodes("//*[@class='image-list cl']");
            if (imageItems == null)
            {
                return list;
            }
            foreach (var imageItem in imageItems)
            {
                var imgNode = imageItem.SelectSingleNode("./div[1]/img");
                var tags = imgNode.Attributes["alt"].Value;
                var item = new ImageItem()
                {
                    Height = Convert.ToInt32(imageItem.SelectSingleNode(".//div[@class='image']").Attributes["data-height"].Value),
                    Width = Convert.ToInt32(imageItem.SelectSingleNode(".//div[@class='image']").Attributes["data-width"].Value),
                    Author = imageItem.SelectSingleNode("//small/a").InnerText,
                    IsExplicit = false,
                    TagsText = tags,
                    Description = tags,
                    ThumbnailUrl = imgNode.Attributes["data-original"].Value.Replace("!single","!320px"),
                    //JpegUrl = SiteUrl + imgNode.Attributes["data-viewersss"].Value,
                    Id = StringToInt(imgNode.Attributes["id"].Value),
                    DetailUrl = HomeUrl + imgNode.Attributes["data-href"].Value,
                    Score = Convert.ToInt32(imageItem.SelectSingleNode(".//span[@class='num']").InnerText)
                };

                item.GetDetailAction = () =>
                {
                    var i = item;
                    var p = Settings.Proxy;
                    var html = Sweb.Get(i.DetailUrl, proxy, shc);

                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    var showIndexs = doc.DocumentNode.SelectSingleNode("//div[@class='logo']");
                    var imgDownNode = showIndexs.SelectSingleNode("//div[@class='img-control']");
                    var nodeHtml = showIndexs.OuterHtml;
                    i.Date = TimeConvert(nodeHtml);

                    if (nodeHtml.Contains("pixiv page"))
                    {
                        i.Source = showIndexs.SelectSingleNode(".//a[@target='_blank']").Attributes["href"].Value;
                    }
                    else
                    {
                        i.Source = Regex.Match(nodeHtml, @"(?<=源地址).*?(?=</p>)").Value.Trim();
                    }
                    i.PreviewUrl = doc.DocumentNode.SelectSingleNode("//figure[@class=\'show-image\']/img").Attributes["src"].Value;
                    if (Regex.Matches(imgDownNode.OuterHtml, "href").Count > 1)
                    {
                        i.OriginalUrl = HomeUrl + imgDownNode.SelectSingleNode("./a[1]").Attributes["href"].Value;
                        i.FileSize = Regex.Match(imgDownNode.SelectSingleNode("./a[1]").InnerText, @"(?<=().*?(?=))").Value;
                    }
                    else
                    {
                        i.OriginalUrl = HomeUrl + imgDownNode.SelectSingleNode("./a").Attributes["href"].Value;
                        i.FileSize = Regex.Match(imgDownNode.SelectSingleNode("./a").InnerText, @"(?<=().*?(?=))").Value;
                    }
                    i.JpegUrl = i.PreviewUrl.Length > 0 ? i.PreviewUrl : i.OriginalUrl;
                };
                list.Add(item);
            }

            return list;
        }
        
        private int StringToInt(string id)
        {
            var str = id.Trim();                            // 去掉字符串首尾处的空格
            var charBuf = str.ToArray();                    // 将字符串转换为字符数组
            var charToASCII = new ASCIIEncoding();
            var TxdBuf = new byte[charBuf.Length];          // 定义发送缓冲区；
            TxdBuf = charToASCII.GetBytes(charBuf);
            var idOut = BitConverter.ToInt32(TxdBuf, 0);
            return idOut;
        }

        /// <summary>
        /// 还原Cookie
        /// </summary>
        private void CookieRestore()
        {
            if (!string.IsNullOrWhiteSpace(cookie)) return;

            var ck = Sweb.GetURLCookies(HomeUrl);
            if (!string.IsNullOrWhiteSpace(ck))
                cookie = ck;
        }

        private void Login(IWebProxy proxy)
        {
            //第二次上传账户密码,使cookie可以用于登录
            if (!cookie.Contains("otome_"))
            {
                try
                {
                    var loginUrl = "http://yuriimg.com/account/login";

                    /*
                     * 开始边界符
                     * 分隔边界符
                     * 结束边界符
                     * Post数据
                     */
                    string
                        boundary = "---------------" + DateTime.Now.Ticks.ToString("x"),
                        pboundary = "--" + boundary,
                        endBoundary = "--" + boundary + "--\r\n",
                        postData = pboundary + "\r\nContent-Disposition: form-data; name=\"username\"\r\n\r\n"
                        + user + "\r\n" + pboundary
                        + "\r\nContent-Disposition: form-data; name=\"password\"\r\n\r\n"
                        + pass + "\r\n" + endBoundary;

                    var retData = "";

                    cookie = "";
                    shc.Referer = Referer;
                    shc.AllowAutoRedirect = false;
                    shc.Accept = SessionHeadersValue.AcceptAppJson;
                    shc.AcceptEncoding = SessionHeadersValue.AcceptEncodingGzip;
                    shc.ContentType = SessionHeadersValue.ContentTypeFormData + "; boundary=" + boundary;
                    shc.AutomaticDecompression = DecompressionMethods.GZip;
                    shc.Remove("Accept-Ranges");

                    retData = Sweb.Post(loginUrl, postData, proxy, shc);
                    cookie = Sweb.GetURLCookies(HomeUrl);

                    if (retData.Contains("-2"))
                    {
                        throw new Exception("密码错误");
                    }
                    else if ((!cookie.Contains("otome_")))
                    {
                        throw new Exception("登录时出错");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message.TrimEnd("。".ToCharArray()) + "自动登录失败");
                }
            }
        }
        private string TimeConvert(string html)
        {
            var date = Regex.Match(html, @"(?<=<span>).*?(?=</span>)").Value;
            if (date.Contains("时前"))
            {
                date = DateTime.Now.AddHours(-Convert.ToDouble(Regex.Match(date, @"\d+").Value)).ToString("yyyy-MM-dd hh.mm");
            }
            else if (date.Contains("天前"))
            {
                date = DateTime.Now.AddDays(-Convert.ToDouble(Regex.Match(date, @"\d+").Value)).ToString("yyyy-MM-dd hh.mm");
            }
            else if (date.Contains("月前"))
            {
                date = DateTime.Now.AddMonths(-Convert.ToInt32(Regex.Match(date, @"\d+").Value)).ToString("yyyy-MM-dd hh.mm");
            }
            return date;
        }
    }
}