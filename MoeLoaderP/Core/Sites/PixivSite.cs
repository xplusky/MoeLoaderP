using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace MoeLoader.Core.Sites
{
    // todo 需要优化
    public class PixivSite : MoeSite
    {
        //标签, 完整标签, 作者id, 日榜, 周榜, 月榜, 作品id
        public enum PixivSrcType { Tag, TagFull, Author, Day, Week, Month, Pid }

        public override string HomeUrl => "https://www.pixiv.net";

        public override string DisplayName => "Pixiv";

        public override string ShortName => "pixiv";
        public string Referer => _referer;
        public virtual string SubReferer => ShortName + ",pximg";
        
        private readonly string[] _user = { "moe1user", "moe3user", "a-rin-a" };
        private readonly string[] _pass = { "630489372", "1515817701", "2422093014" };
        private static string _tempPage = null;
        private string _referer = "https://www.pixiv.net/";

        private bool IsLogin { get; set; }

        /// <summary>
        /// pixiv.net site
        /// </summary>
        public PixivSite()
        {

            SurpportState.IsSupportResolution = false;
            SurpportState.IsSupportScore = false;

            SubMenu.Add("最新/标签"); // 0
            SubMenu.Add("作者"); // 1
            SubMenu.Add("本日排行"); // 2
            SubMenu.Add("本周排行"); // 3
            SubMenu.Add("本月排行"); // 4
            SubMenu.Add("完整标签"); // 5
            SubMenu.Add("作品ID"); // 6
        }

        private async Task LoginAsync()
        {
            var hdoc = new HtmlDocument();
            
            const string loginpost = "https://accounts.pixiv.net/api/login?lang=zh";
            const string loginurl = "https://accounts.pixiv.net/login?lang=zh&source=pc&view_type=page&ref=";

            var index = new Random().Next(0, _user.Length);

            
            Net.Client.DefaultRequestHeaders.Referrer = new Uri(Referer);
            // todo POST取登录Cookie
            //请求1 获取post_key
            var data = await Net.Client.GetStringAsync(loginurl);
            hdoc.LoadHtml(data);
            var postKey = hdoc.DocumentNode.SelectSingleNode("//input[@name='post_key']").Attributes["value"].Value;
            if (postKey.Length < 9) App.Log(ShortName, "自动登录失败(获取post_key失败）");

            // 请求2
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"pixiv_id", _user[index]},
                {"captcha", ""},
                {"password", _pass[index]},
                {"post_key", postKey},
                {"source", "pc"},
                {"return_to", "https%3A%2F%2Fwww.pixiv.net%2F"}
            });

            var respose = await Net.Client.PostAsync(loginpost, content);
            if (respose.IsSuccessStatusCode) IsLogin = true;
            else
            {
                App.Log(ShortName, "自动登录失败 ");
            }
        }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para)
        {
            if(Net == null) Net = new NetSwap(Settings, HomeUrl);

            if (!IsLogin) await LoginAsync();

            // --------------------------page-------------------
            //if (page > 1000) throw new Exception("页码过大，若需浏览更多图片请使用关键词限定范围");
            string query;
            var keyWord = para.Keyword;
            var page = para.PageIndex;
            if (SubListIndex == 6)
            {
                if (para.Keyword.Length > 0 && Regex.Match(para.Keyword, @"^[0-9]+$").Success)
                {
                    query = $"{HomeUrl}/member_illust.php?mode=medium&illust_id={keyWord}";
                }
                else throw new Exception("请输入图片id");
            }
            else
            {
                //http://www.pixiv.net/new_illust.php?p=2
                query = $"{HomeUrl}/new_illust.php?type=all&p={page}";

                if (keyWord.Length > 0)
                {
                    //http://www.pixiv.net/search.php?s_mode=s_tag&word=hatsune&order=date_d&p=2
                    query = $"{HomeUrl}/search.php?s_mode=s_tag" + (SubListIndex == 5 ? "_full" : "")
                    + "&word=" + keyWord + "&order=date_d&p=" + page;
                }
                if (SubListIndex == 1)
                {
                    var memberId = 0;
                    if (keyWord.Trim().Length == 0 || !int.TryParse(keyWord.Trim(), out memberId))
                    {
                        throw new Exception("必须在关键词中指定画师 id；若需要使用标签进行搜索请使用 www.pixiv.net [TAG]");
                    }
                    //member id
                    query = HomeUrl + "/member_illust.php?id=" + memberId + "&p=" + page;
                }
                else if (SubListIndex == 2)
                {
                    query = HomeUrl + "/ranking.php?mode=daily&p=" + page;
                }
                else if (SubListIndex == 3)
                {
                    query = HomeUrl + "/ranking.php?mode=weekly&p=" + page;
                }
                else if (SubListIndex == 4)
                {
                    query = HomeUrl + "/ranking.php?mode=monthly&p=" + page;
                }
            }
            var pageString = await Net.Client.GetStringAsync(query);


            // ----------------images---------------------------
            var imgs = new ImageItems();

            var doc = new HtmlDocument();
            doc.LoadHtml(pageString);

            //retrieve all elements via xpath
            HtmlNodeCollection nodes = null;
            HtmlNode tagNode = null;

            try
            {
                if (SubListIndex == 0 || SubListIndex == 5)
                {
                    tagNode = doc.DocumentNode.SelectSingleNode("//input[@id='js-mount-point-search-result-list']");
                    //nodes = doc.DocumentNode.SelectSingleNode("//div[@id='wrapper']/div[2]/div[1]/section[1]/ul").SelectNodes("li");
                }
                else if (SubListIndex == 1)
                {
                    nodes = doc.DocumentNode.SelectSingleNode("//ul[@class='_image-items']").SelectNodes("li");
                }
                //else if (srcType == PixivSrcType.Day || srcType == PixivSrcType.Month || srcType == PixivSrcType.Week) //ranking
                //nodes = doc.DocumentNode.SelectSingleNode("//section[@class='ranking-items autopagerize_page_element']").SelectNodes("div");
                else if (SubListIndex == 6)
                {
                    if (!(Regex.Match(pageString, @"<h2.*?/h2>").Value.Contains("错误")))
                    {
                        _tempPage = pageString;
                        var mangaCount = 1;
                        string sampleUrl;
                        string id;

                        if (!pageString.Contains("globalInitData"))
                        {
                            //----- 旧版 -----
                            sampleUrl = doc.DocumentNode.SelectSingleNode("/html/head/meta[@property='og:image']").Attributes["content"].Value;
                            id = sampleUrl.Substring(sampleUrl.LastIndexOf("/") + 1, sampleUrl.IndexOf("_") - sampleUrl.LastIndexOf("/") - 1);

                            var dimension = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[2]").InnerText;
                            if (dimension.EndsWith("P"))
                                mangaCount = int.Parse(Regex.Match(dimension, @"\d+").Value);
                        }
                        else
                        {
                            //----- 新版 -----
                            var strRex = Regex.Match(pageString, @"(?<=(?:,illust\:.{.))\d+(?=(?:\:.))");
                            id = strRex.Value;

                            strRex = Regex.Match(pageString, @"(?<=(?:" + id + ":.)).*?(?=(?:.},user))");

                            var jobj = JObject.Parse(strRex.Value);
                            try { mangaCount = int.Parse(jobj["pageCount"].ToString()); } catch { }

                            jobj = JObject.Parse(jobj["urls"].ToString());
                            sampleUrl = jobj["thumb"].ToString();
                        }

                        var detailUrl = HomeUrl + "/member_illust.php?mode=medium&illust_id=" + id;
                        for (var i = 0; i < mangaCount; i++)
                        {
                            var img = GenerateImg(detailUrl, sampleUrl.Replace("_p0_", "_p" + i.ToString() + "_"), id);
                            var sb = new StringBuilder();
                            sb.Append("P");
                            sb.Append(i.ToString());
                            if (img != null) imgs.Add(img);
                        }
                        return imgs;
                    }
                    else throw new Exception("该作品已被删除，或作品ID不存在");
                }
                else
                {
                    //ranking
                    nodes = doc.DocumentNode.SelectNodes("//section[@class='ranking-item']");
                }
            }
            catch
            {
                throw new Exception("没有找到图片哦～ .=ω=");
            }

            if (SubListIndex == 0 || SubListIndex == 5)
            {
                if (tagNode == null)
                {
                    return imgs;
                }
            }
            else if (nodes == null)
            {
                return imgs;
            }

            //Tag search js-mount-point-search-related-tags Json
            if (SubListIndex == 0 || SubListIndex == 5)
            {
                var jsonData = tagNode.Attributes["data-items"].Value.Replace("&quot;", "\"");
                var array = (new JavaScriptSerializer()).DeserializeObject(jsonData) as object[];
                foreach (var o in array)
                {
                    var obj = o as Dictionary<string, object>;
                    string
                        detailUrl = "",
                        sampleUrl = "",
                        id = "";
                    if (obj["illustId"] != null)
                    {
                        id = obj["illustId"].ToString();
                        detailUrl = HomeUrl + "/member_illust.php?mode=medium&illust_id=" + id;
                    }
                    if (obj["url"] != null)
                    {
                        sampleUrl = obj["url"].ToString();
                    }
                    var img = GenerateImg(detailUrl, sampleUrl, id);
                    if (img != null) imgs.Add(img);
                }
            }
            else
            {
                foreach (var imgNode in nodes)
                {
                    try
                    {
                        var anode = imgNode.SelectSingleNode("a");
                        if (SubListIndex == 2 || SubListIndex == 3 || SubListIndex == 4)
                        {
                            anode = imgNode.SelectSingleNode(".//div[@class='ranking-image-item']").SelectSingleNode("a");
                        }
                        //details will be extracted from here
                        //eg. member_illust.php?mode=medium&illust_id=29561307&ref=rn-b-5-thumbnail
                        //sampleUrl 正则 @"https://i\.pximg\..+?(?=")"
                        var detailUrl = anode.Attributes["href"].Value.Replace("amp;", "");
                        var sampleUrl = "";
                        sampleUrl = anode.SelectSingleNode(".//img").Attributes["src"].Value;

                        if (sampleUrl.ToLower().Contains("images/common"))
                            sampleUrl = anode.SelectSingleNode(".//img").Attributes["data-src"].Value;

                        if (sampleUrl.Contains('?'))
                            sampleUrl = sampleUrl.Substring(0, sampleUrl.IndexOf('?'));

                        //extract id from detail url
                        //string id = detailUrl.Substring(detailUrl.LastIndexOf('=') + 1);
                        var id = Regex.Match(detailUrl, @"illust_id=\d+").Value;
                        id = id.Substring(id.IndexOf('=') + 1);

                        var img = GenerateImg(detailUrl, sampleUrl, id);
                        if (img != null)
                        {
                            imgs.Add(img);
                        }
                    }
                    catch
                    {
                        //int i = 0;
                    }
                }
            }

            return imgs;
        }
       
        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            if (Net == null) Net = new NetSwap(Settings, HomeUrl);
            var re = new AutoHintItems();

            if (SubListIndex == 0 || SubListIndex == 5)
            {
                var tags = new Dictionary<string, object>();
                var tag = new Dictionary<string, object>();

                var url = string.Format(HomeUrl + "/rpc/cps.php?keyword={0}", para.Keyword);

                Net.Client.DefaultRequestHeaders.Referrer = new Uri(_referer);
                var json = await Net.Client.GetStringAsync(url);

                tags = (new JavaScriptSerializer()).DeserializeObject(json) as Dictionary<string, object>;
                if (tags.ContainsKey("candidates"))
                {
                    foreach (var obj in tags["candidates"] as object[])
                    {
                        tag = obj as Dictionary<string, object>;
                        re.Add(new AutoHintItem() { Word = tag["tag_name"].ToString() });
                    }
                }
            }
            return re;
        }


        private ImageItem GenerateImg(string detailUrl, string sampleUrl, string id)
        {
            
            var intId = int.Parse(id);

            if (!detailUrl.StartsWith("http") && !detailUrl.StartsWith("/"))
                detailUrl = "/" + detailUrl;

            //convert relative url to absolute
            if (detailUrl.StartsWith("/"))
                detailUrl = HomeUrl + detailUrl;
            if (sampleUrl.StartsWith("/"))
                sampleUrl = HomeUrl + sampleUrl;

            _referer = detailUrl;
            //string fileUrl = preview_url.Replace("_s.", ".");
            //string sampleUrl = preview_url.Replace("_s.", "_m.");

            //http://i1.pixiv.net/img-inf/img/2013/04/10/00/11/37/34912478_s.png
            //http://i1.pixiv.net/img03/img/tukumo/34912478_m.png
            //http://i1.pixiv.net/img03/img/tukumo/34912478.png

            var img = new ImageItem()
            {
                //Date = "N/A",
                //FileSize = file_size.ToUpper(),
                //Desc = intId + " ",
                Id = intId,
                //JpegUrl = fileUrl,
                //OriginalUrl = fileUrl,
                //PreviewUrl = preview_url,
                ThumbnailUrl = sampleUrl,
                //Score = 0,
                //Width = width,
                //Height = height,
                //Tags = tags,
                DetailUrl = detailUrl
            };

            img.ThumbnailReferer = Referer;
            img.Net = Net;

            img.GetDetailAction = async () =>
            {
                var i = img;
                var p = Settings.Proxy;
                var pageCount = 1;
                string page, dimension, pcount;
                page = dimension = string.Empty;
                if (SubListIndex == 6)
                    page = _tempPage;
                //retrieve details
                else
                    page = await Net.Client.GetStringAsync(i.DetailUrl);

                var reg = new Regex(@"^「(?<Desc>.*?)」/「(?<Author>.*?)」");
                var doc = new HtmlDocument();
                var ds = new HtmlDocument();
                doc.LoadHtml(page);
                pcount = Regex.Match(i.ThumbnailUrl, @"(?<=_p)\d+(?=_)").Value;

                //=================================================
                //「カルタ＆わたぬき」/「えれっと」のイラスト [pixiv]
                //标题中取名字和作者
                try
                {
                    var mc = reg.Matches(doc.DocumentNode.SelectSingleNode("//title").InnerText);
                    if (SubListIndex == 6)
                        i.Description = mc[0].Groups["Desc"].Value + "P" + pcount;
                    else
                        i.Description = mc[0].Groups["Desc"].Value;
                    i.Author = mc[0].Groups["Author"].Value;
                }
                catch { }
                //------------------------
                if (!page.Contains("globalInitData"))
                {
                    //++++旧版详情页+++++
                    //04/16/2012 17:44｜600×800｜SAI  or 04/16/2012 17:44｜600×800 or 04/19/2012 22:57｜漫画 6P｜SAI
                    i.Date = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[1]")?.InnerText;
                    //总点数
                     int.TryParse(doc.DocumentNode.SelectSingleNode("//dd[@class='rated-count']")?.InnerText,out var score);
                    i.Score = score;

                    //URLS
                    //http://i2.pixiv.net/c/600x600/img-master/img/2014/10/08/06/13/30/46422743_p0_master1200.jpg
                    //http://i2.pixiv.net/img-original/img/2014/10/08/06/13/30/46422743_p0.png
                    var rx = new Regex(@"/\d+x\d+/");
                    i.PreviewUrl = rx.Replace(i.ThumbnailUrl, "/1200x1200/");
                    i.JpegUrl = i.PreviewUrl;
                    try
                    {
                        i.FileUrl = doc.DocumentNode.SelectSingleNode("//*[@id='wrapper']/div[2]/div")?.SelectSingleNode(".//img").Attributes["data-src"].Value;
                    }
                    catch { }
                    i.FileUrl = string.IsNullOrWhiteSpace(i.FileUrl) ? i.JpegUrl : i.FileUrl;

                    //600×800 or 漫画 6P
                    dimension = doc.DocumentNode.SelectSingleNode("//ul[@class='meta']/li[2]")?.InnerText;
                    try
                    {
                        //706×1000
                        i.Width = int.Parse(dimension.Substring(0, dimension.IndexOf('×')));
                        i.Height = int.Parse(Regex.Match(dimension.Substring(dimension.IndexOf('×') + 1), @"\d+").Value);
                    }
                    catch { }
                }
                else
                {
                    //+++++新版详情页+++++
                    var strRex = Regex.Match(page, @"(?<=(?:" + i.Id + ":.)).*?(?=(?:.},user))");

                    var jobj = JObject.Parse(strRex.Value);

                    i.Date = jobj["uploadDate"].ToString();

                    try
                    {
                        i.Score = int.Parse(jobj["likeCount"].ToString());
                        i.Width = int.Parse(jobj["width"].ToString());
                        i.Height = int.Parse(jobj["height"].ToString());
                        pageCount = int.Parse(jobj["pageCount"].ToString());
                    }
                    catch { }

                    jobj = JObject.Parse(jobj["urls"].ToString());
                    var rex = new Regex(@"(?<=.*)p\d+(?=[^/]*[^\._]*$)");
                    i.PreviewUrl = rex.Replace(jobj["regular"].ToString(), "p" + pcount);
                    i.JpegUrl = rex.Replace(jobj["small"].ToString(), "p" + pcount);
                    i.FileUrl = rex.Replace(jobj["original"].ToString(), "p" + pcount);
                }

                try
                {
                    if (pageCount > 1 || i.Width == 0 && i.Height == 0)
                    {
                        //i.OriginalUrl = i.SampleUrl.Replace("600x600", "1200x1200");
                        //i.JpegUrl = i.OriginalUrl;
                        //manga list
                        //漫画 6P
                        var oriul = "";
                        var mangaCount = pageCount;
                        if (pageCount > 1)
                        {
                            mangaCount = pageCount;
                        }
                        else
                        {
                            var index = dimension.IndexOf(' ') + 1;
                            var mangaPart = dimension.Substring(index, dimension.IndexOf('P') - index);
                            mangaCount = int.Parse(mangaPart);
                        }
                        if (SubListIndex == 6)
                        {
                            try
                            {
                                page = await Net.Client.GetStringAsync(i.DetailUrl.Replace("medium", "manga_big") + "&page=" + pcount);
                                ds.LoadHtml(page);
                                i.FileUrl = ds.DocumentNode.SelectSingleNode("/html/body/img").Attributes["src"].Value;
                            }
                            catch { }
                        }
                        else
                        {
                            //i.DimensionString = "Manga " + mangaCount + "P";
                            for (var j = 0; j < mangaCount; j++)
                            {
                                //保存漫画时优先下载原图 找不到原图则下jpg
                                try
                                {
                                    page = await Net.Client.GetStringAsync(i.DetailUrl.Replace("medium", "manga_big") + "&page=" + j);
                                    ds.LoadHtml(page);
                                    oriul = ds.DocumentNode.SelectSingleNode("/html/body/img").Attributes["src"].Value;
                                    img.ChilldrenItems.Add(new ImageItem { FileUrl = oriul });
                                    if (j == 0)
                                        img.FileUrl = oriul;
                                }
                                catch
                                {
                                    //oriUrl = "http://img" + imgsvr + ".pixiv.net/img/" + items[6].Split('/')[4] + "/" + id + "_p0." + ext;
                                    img.ChilldrenItems.Add(new ImageItem { FileUrl = i.FileUrl.Replace("_p0", "_p" + j) });
                                }
                            }
                        }
                    }
                }
                catch { }
            };
            img.Site = this;
            return img;
        }


    }
}
