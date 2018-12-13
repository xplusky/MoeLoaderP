using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// pixiv.net fixed 20181001
    /// </summary>
    public class PixivSite : MoeSite
    {
        public override string HomeUrl => "https://www.pixiv.net";

        public override string DisplayName => "Pixiv";

        public override string ShortName => "pixiv";
        
        private readonly string[] _user = { "moe1user", "moe3user", "a-rin-a" };
        private readonly string[] _pass = { "630489372", "1515817701", "2422093014" };

        private bool IsLogin { get; set; }
        public SearchTypeEnum SearchType => (SearchTypeEnum)SubListIndex;

        public enum SearchTypeEnum
        {
            TagOrNew = 0,
            Author = 1,
            Day = 2,
            Week = 3,
            Month = 4,
        }
        /// <summary>
        /// pixiv.net site
        /// </summary>
        public PixivSite(bool xmode)
        {
            SubMenu.Add("最新/标签"); // 0
            SubMenu.Add("作者ID"); // 1
            SubMenu.Add("本日排行"); // 2
            SubMenu.Add("本周排行"); // 3
            SubMenu.Add("本月排行"); // 4

            if (xmode)
            {
                for (var i = 0; i < SubMenu.Count; i++)
                {
                    var item = SubMenu[i];
                    if (i == 2 || i == 3 || i == 4) item.NoNeedKeyword = true;
                    if(i == 1 || i == 4) continue;
                    item.SubMenu.Add(new MoeSiteSubMenuItem { Name = "普通" });
                    item.SubMenu.Add(new MoeSiteSubMenuItem { Name = "R18" });
                }
            }

            DownloadTypes.Add("原图", 4);
        }

        private async Task LoginAsync(CancellationToken token)
        {
            // 步骤0 GET 测试网络可用性
            Net.SetReferer(HomeUrl);
            try
            {
                var homerespose = await Net.Client.GetAsync("https://www.pixiv.net/", token);
                if (!homerespose.IsSuccessStatusCode) throw new Exception("!homerespose.IsSuccessStatusCode");
            }
            catch (Exception e)
            {
                App.Log(e);
                App.ShowMessage("无法连接至https://www.pixiv.net，请检查代理设置");
                return;
            }

            // 步骤1 GET 获取 post_key 参数
            App.ShowMessage("pixiv.net自动登录中",1);
            const string loginurl = "https://accounts.pixiv.net/login?lang=zh&source=pc&view_type=page&ref=";
            var data = await Net.Client.GetStringAsync(loginurl);
            var hdoc = new HtmlDocument();
            hdoc.LoadHtml(data);
            var postKey = hdoc.DocumentNode.SelectSingleNode("//input[@name='post_key']").Attributes["value"].Value;
            if (string.IsNullOrWhiteSpace(postKey) || postKey.Length < 9)
            {
                App.Log(ShortName, "自动登录失败(获取post_key失败）");
                App.ShowMessage("pixiv.net自动登录失败");
                return;
            }

            // 步骤2 POST 登录获取Cookie
            var uindex = new Random().Next(0, _user.Length);
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"pixiv_id", _user[uindex]},
                {"captcha", ""},
                {"password", _pass[uindex]},
                {"post_key", postKey},
                {"source", "pc"},
                {"ref", "wwwtop_accounts_index"},
                {"return_to", HomeUrl.ToEncodedUrl()}
            });
            try
            {
                
                Net.SetReferer("https://accounts.pixiv.net/login?lang=zh&source=pc&view_type=page&ref=wwwtop_accounts_index");
                const string loginpost = "https://accounts.pixiv.net/api/login?lang=zh";
                var response = await Net.Client.PostAsync(loginpost, content, token);
                if (response.IsSuccessStatusCode)
                {
                    IsLogin = true;
                    App.ShowMessage("pixiv.net登录成功，获取页面中", 1);
                }
                else throw new Exception("步骤2 POST 登录获取Cookie response.IsSuccessStatusCode == false");
            }
            catch (Exception e)
            {
                App.Log(e);
                App.ShowMessage("pixiv.net自动登录失败");
                return;
            }
        }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            if (Net == null)
            {
                Net = new NetSwap(Settings, HomeUrl);
                Net.HttpClientHandler.AllowAutoRedirect = true;
                Net.SetTimeOut(40);
            }
            
            if (!IsLogin) await LoginAsync(token);
            if (!IsLogin) return new ImageItems();

            // page
            var word = para.Keyword;
            var page = para.PageIndex;
            var query = "";
            var xmodestr = para.IsShowExplicit ? "" : "&mode=safe";
            if (Lv3ListIndex == 1) xmodestr = "&mode=r18";
            var r18 = Lv3ListIndex == 1 ? "_r18" : "";
            switch (SearchType)
            {
                case SearchTypeEnum.TagOrNew:
                    if (string.IsNullOrWhiteSpace(word)) // empty
                    {
                        // https://www.pixiv.net/new_illust.php?type=all&p=2
                        
                        query = $"{HomeUrl}/new_illust{r18}.php?type=all&p={page}";
                    }
                    else
                    {
                        query = $"{HomeUrl}/search.php?s_mode=s_tag&word={word.ToEncodedUrl()}&order=date_d&p={page}{xmodestr}";
                    }
                    break;
                case SearchTypeEnum.Author:// 作者 member id
                    //word = "4338012"; // test
                    if (!int.TryParse(word.Trim(), out var memberId))
                    {
                        App.ShowMessage("参数错误，必须在关键词中指定画师 id（纯数字）");
                        return new ImageItems();
                    }
                    // https://www.pixiv.net/member_illust.php?id=4338012&p=1
                    query = $"{HomeUrl}/member_illust.php?id={word.Trim()}&p={page}";
                    query = word;
                    break;
                case SearchTypeEnum.Day:
                    query = $"{HomeUrl}/ranking.php?mode=daily{r18}&p={page}&format=json";
                    break;
                case SearchTypeEnum.Week:
                    // https://www.pixiv.net/ranking.php?mode=weekly&p=1&format=json
                    query = $"{HomeUrl}/ranking.php?mode=weekly{r18}&p={page}&format=json";
                    break;
                case SearchTypeEnum.Month:
                    query = $"{HomeUrl}/ranking.php?mode=monthly&p={page}&format=json";
                    break;
            }

            // ----------------images---------------------------
            var imgs = new ImageItems();

            switch (SearchType)
            {
                case SearchTypeEnum.TagOrNew:
                    if (string.IsNullOrWhiteSpace(word)) await SearchByNew(imgs, query,para, token);
                    else await SearchByTag(imgs, query, para, token);

                    break;
                case SearchTypeEnum.Day:
                case SearchTypeEnum.Week:
                case SearchTypeEnum.Month:
                    await SearchByRank(imgs, query,para, token);
                    break;
                case SearchTypeEnum.Author:
                    await SearchByAuthor(imgs, query,para, token);
                    break;
            }
            token.ThrowIfCancellationRequested();
            return imgs;
        }

        public async Task SearchByAuthor(ImageItems imgs, string uid,SearchPara para, CancellationToken token)
        {
            var net = Net.CreatNewWithRelatedCookie();
            net.SetReferer($"{HomeUrl}/member_illust.php?id={uid}&p=1");
            var jsonres = await net.Client.GetAsync($"{HomeUrl}/ajax/user/{uid}/profile/all", token);
            var jsonstr = await jsonres.Content.ReadAsStringAsync();
            dynamic json = JsonConvert.DeserializeObject(jsonstr);
            var picids = new List<string>();
            var illusts = json?.body?.illusts;
            if (illusts != null)
            {
                foreach (var ill in illusts)
                {
                    var property = (JProperty) ill;
                    picids.Add(property.Name);
                }
            }
            var manga = json?.body?.manga;
            if (manga != null)
            {
                foreach (var m in manga)
                {
                    var property = (JProperty)m;
                    picids.Add(property.Name);
                }
            }
            var picCurrentPage = picids.OrderByDescending(i => i).Skip((para.PageIndex-1)*para.Count).Take(para.Count).ToList();
            if(!picCurrentPage.Any()) return;
            var q = $"{HomeUrl}/ajax/user/{uid}/profile/illusts?";
            foreach (var pic in picCurrentPage)
            {
                q += $"ids%5B%5D={pic}&";
            }
            q += "is_manga_top=0";
            var net2 = Net.CreatNewWithRelatedCookie();
            net2.SetReferer($"{HomeUrl}/member_illust.php?id={uid}");
            var picrespose = await net2.Client.GetStringAsync(q);
            dynamic picsjson = JsonConvert.DeserializeObject(picrespose);
            var works = picsjson?.body?.works;
            if (works != null)
            {
                foreach (var item in works)
                {
                    var jp = (JProperty)item;
                    dynamic jitm = jp.Value;
                    var img = new ImageItem(this,para);
                    img.Net = Net.CreatNewWithRelatedCookie();
                    img.Urls.Add(new UrlInfo("缩略图", 1, $"{jitm.url}", $"{HomeUrl}/member_illust.php?id={uid}"));
                    int.TryParse($"{jitm.id}", out var id);
                    img.Id = id;
                    int.TryParse($"{jitm.rating_count}", out var score);
                    img.Score = score;
                    if (jitm.tags != null)
                    {
                        foreach (var tag in jitm.tags)
                        {
                            img.Tags.Add($"{tag}");
                        }
                    }
                    img.DetailUrl = $"{HomeUrl}/member_illust.php?mode=medium&illust_id={id}";
                    img.Author = $"{jitm.user_name}";
                    img.Title = $"{jitm.title}";
                    img.GetDetailAction = async () => await GetDetailAction(img.DetailUrl, img, Net.CreatNewWithRelatedCookie(),para);

                    imgs.Add(img);
                }
            }
        }

        public async Task SearchByRank(ImageItems imgs, string query, SearchPara para, CancellationToken token)
        {
            var net = Net.CreatNewWithRelatedCookie();
            var pageres = await net.Client.GetAsync(query,token);
            var pageString = await pageres.Content.ReadAsStringAsync();
            dynamic jsonlist = JsonConvert.DeserializeObject(pageString);
            if(jsonlist?.contents == null) return;
            foreach (var jitm in jsonlist.contents)
            {
                var img = new ImageItem(this, para);
                img.Site = this;
                img.Net = Net.CreatNewWithRelatedCookie();
                img.Urls.Add(new UrlInfo("缩略图", 1, $"{jitm.url}", query));
                img.Title = $"{jitm.title}";
                if (jitm.tags != null)
                {
                    foreach (var tag in jitm.tags)
                    {
                        img.Tags.Add($"{tag}");
                    }
                }
                int.TryParse($"{jitm.rating_count}", out var score);
                img.Score = score;
                int.TryParse($"{jitm.illust_id}", out var id);
                img.Id = id;
                img.Author = $"{jitm.user_name}";
                int.TryParse($"{jitm.illust_page_count}", out var pcount);
                img.DetailUrl = $"{HomeUrl}/member_illust.php?mode=medium&illust_id={id}";
                img.GetDetailAction = async () => await GetDetailAction(img.DetailUrl, img, Net.CreatNewWithRelatedCookie(),para);

                imgs.Add(img);
            }
        }

        public async Task SearchByNew(ImageItems imgs,string query,SearchPara para, CancellationToken token)
        {
            var net = Net.CreatNewWithRelatedCookie();
            var pageres = await net.Client.GetAsync(query, token);
            var pageString = await pageres.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(pageString);

            var imgnodes = doc.DocumentNode.SelectNodes("//li[@class='image-item']");
            if (imgnodes == null) return;

            foreach (var imglinode in imgnodes)
            {
                var img = new ImageItem(this, para);
                img.Site = this;
                img.Net = Net.CreatNewWithRelatedCookie();
                

                var imgel = imglinode.SelectSingleNode("a/div/img").OuterHtml;
                var imgdoc = new HtmlDocument();
                imgdoc.LoadHtml(imgel);
                var i2 = imgdoc.DocumentNode.SelectSingleNode("img");
                img.Urls.Add(new UrlInfo("缩略图", 1, i2?.GetAttributeValue("data-src", ""), query));
                int.TryParse(i2?.GetAttributeValue("data-id", "0"), out var id);
                img.Id = id;
                var tags = i2?.GetAttributeValue("data-tags", "");
                if (!string.IsNullOrWhiteSpace(tags))
                {
                    foreach (var tag in tags.Split(' '))
                    {
                        img.Tags.Add(tag);
                    }
                }

                var title = imglinode.SelectNodes("a")?[1]?.InnerText?.ToDecodedUrl();
                img.Title = title;
                var usernode = imglinode.SelectNodes("a")?[2];
                var user = usernode?.GetAttributeValue("data-user_name", "");
                img.Author = user;

                var link = imglinode.SelectSingleNode("a")?.GetAttributeValue("href", "");
                //var countdiv = imglinode.SelectSingleNode("./div[@class='page-count']");

                var fulllink = $"{HomeUrl}{link?.Replace("&amp;", "&")}";
                //if (countb) fulllink = fulllink.Replace("mode=medium", "mode=manga");
                img.DetailUrl = fulllink;

                var subnet = Net.CreatNewWithRelatedCookie();
                img.GetDetailAction = async () => await GetDetailAction(fulllink,img,subnet,para);

                imgs.Add(img);
            }
        }

        public async Task SearchByTag(ImageItems imgs, string query,SearchPara para, CancellationToken token)
        {
            var net = Net.CreatNewWithRelatedCookie();
            var pageres = await net.Client.GetAsync(query, token);
            var pageString = await pageres.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(pageString);

            var itemsjsonstr = doc.DocumentNode.SelectSingleNode("//*[@id='js-mount-point-search-result-list']").GetAttributeValue("data-items","");
            if (string.IsNullOrWhiteSpace(itemsjsonstr))return;
            itemsjsonstr = itemsjsonstr.Replace("&quot;", "\"");
            dynamic jlist = JsonConvert.DeserializeObject(itemsjsonstr);
            if(jlist == null)return;
            foreach (var jitem in jlist)
            {
                var img = new ImageItem(this,para);
                img.Net = Net.CreatNewWithRelatedCookie();
                img.Urls.Add(new UrlInfo("缩略图", 1, $"{jitem.url}", query));
                int.TryParse($"{jitem.illustId}", out var id);
                img.Id = id;
                int.TryParse($"{jitem.bookmarkCount}", out var score);
                img.Score = score;
                img.Width = (int) jitem.width;
                img.Height = (int)jitem.height;
                if (jitem.tags != null)
                {
                    foreach (var tag in jitem.tags)
                    {
                        img.Tags.Add($"{tag}");
                    }
                }
                img.DetailUrl = $"{HomeUrl}/member_illust.php?mode=medium&illust_id={id}";
                img.Author = $"{jitem.userName}";
                img.Title = $"{jitem.illustTitle}";
                img.GetDetailAction = async () => await GetDetailAction(img.DetailUrl, img, Net.CreatNewWithRelatedCookie(),para);

                imgs.Add(img);
            }
        }

        public async Task GetDetailAction(string pageUrl,ImageItem img,NetSwap net,SearchPara para)
        {
            try
            {
                var subpage = await net.Client.GetStringAsync(pageUrl);
                var subdoc = new HtmlDocument();
                subdoc.LoadHtml(subpage);

                img.Net = Net.CreatNewWithRelatedCookie();
                

                var strRex = Regex.Match(subpage, $@"(?<=(?:{img.Id}:.)).*?(?=(?:.}},user))");
                dynamic jobj = JsonConvert.DeserializeObject(strRex.Value);
                if (jobj == null) return;
                int.TryParse($"{jobj.likeCount}", out var score);
                img.Score = score;
                int.TryParse($"{jobj.width}", out var width);
                img.Width = width;
                int.TryParse($"{jobj.height}", out var height);
                img.Height = height;
                int.TryParse($"{jobj.pageCount}", out var pageCount);

                img.Urls.Add(new UrlInfo("原图", 4, $"{jobj.urls?.original}", pageUrl));
                img.Author = $"{jobj.userName}";
                int.TryParse($"{jobj.likeCount}", out var like);
                if (like > 0) img.Score = like;
                var tags = jobj.tags;
                try
                {
                    if (img.Tags.Count == 0)
                    {
                        foreach (var tag in tags)
                        {
                            dynamic v = ((JProperty)tag).Value;
                            img.Tags.Add($"{v.tag}");
                        }
                    }
                }
                catch (Exception e)
                {
                    App.Log(e);
                }
                var title = $"{jobj.illustTitle}";
                if (!string.IsNullOrWhiteSpace(title)) img.Title = title;
                img.Description = $"{jobj.illustComment}";

                if (pageCount > 1)
                {
                    for (var i = 0; i < pageCount; i++)
                    {
                        var subimg = new ImageItem(this,para);
                        var rex = new Regex(@"(?<=.*)p\d+(?=[^/]*[^\._]*$)");
                        subimg.Net = Net.CreatNewWithRelatedCookie();
                        subimg.Urls.Add(new UrlInfo("原图", 4, rex.Replace(img.DownloadUrlInfo.Url, $"p{i}"), pageUrl.Replace("mode=medium", "mode=manga")));

                        img.ChilldrenItems.Add(subimg);
                    }
                }
            }
            catch (Exception e)
            {
                App.Log(e);
            }
        }
        
        private NetSwap AutoHintNet { get; set; }
        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            if (AutoHintNet == null)
            {
                AutoHintNet = new NetSwap(Settings, HomeUrl);
                AutoHintNet.SetReferer(HomeUrl);
            }
            var re = new AutoHintItems();

            if (SubListIndex == 0 || SubListIndex == 5)
            {
                var url = $"{HomeUrl}/rpc/cps.php?keyword={ para.Keyword}";
                var json = await AutoHintNet.Client.GetStringAsync(url);
                dynamic jlist = JsonConvert.DeserializeObject(json);
                if (jlist?.candidates != null)
                {
                    foreach (var obj in jlist.candidates)
                    {
                        var item =new AutoHintItem();
                        item.Word = $"{obj.tag_name}";
                        re.Add(item);
                    }
                }
            }
            return re;
        }
    }
}
