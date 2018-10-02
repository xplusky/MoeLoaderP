using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

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
            Tag = 0,
            Author = 1,
            Pid = 2,
            Day = 3,
            Week = 4,
            Month = 5,

        }
        /// <summary>
        /// pixiv.net site
        /// </summary>
        public PixivSite()
        {
            SurpportState.IsSupportResolution = false;
            SurpportState.IsSupportScore = false;

            SubMenu.Add("最新/标签"); // 0
            SubMenu.Add("作者"); // 1
            SubMenu.Add("作品ID"); // 2
            SubMenu.Add("本日排行"); // 3
            SubMenu.Add("本周排行"); // 4
            SubMenu.Add("本月排行"); // 5
        }

        private async Task LoginAsync()
        {
            // 步骤0 GET 测试网络可用性
            Net.SetReferer(HomeUrl);
            try
            {
                var homerespose = await Net.Client.GetAsync("https://www.pixiv.net/");
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
                var response = await Net.Client.PostAsync(loginpost, content);
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
            
            if (!IsLogin) await LoginAsync();
            if (!IsLogin) return new ImageItems();

            // --------------------------page-------------------
            // https://www.pixiv.net/search.php?word=GUMI&order=date_d&p=3
            string query;
            var keyWord = para.Keyword;
            var page = para.PageIndex;
            if (SearchType == SearchTypeEnum.Pid)
            {
                if (para.Keyword.Length > 0 && Regex.Match(para.Keyword, @"^[0-9]+$").Success)
                {
                    query = $"{HomeUrl}/member_illust.php?mode=medium&illust_id={keyWord}";
                }
                else throw new Exception("请输入图片id");
            }
            else
            {
                // https://www.pixiv.net/new_illust.php?type=all&p=2
                // keyword is empty
                query = $"{HomeUrl}/new_illust.php?type=all&p={page}";

                if (keyWord.Length > 0)
                {
                    // http://www.pixiv.net/search.php?s_mode=s_tag&word=hatsune&order=date_d&p=2
                    query = $"{HomeUrl}/search.php?s_mode=s_tag{(SubListIndex == 5 ? "_full" : "")}&word={keyWord}&order=date_d&p={page}";
                }
                switch (SearchType)
                {
                    case SearchTypeEnum.Author:// 作者
                        {
                        if (keyWord.Trim().Length == 0 || !int.TryParse(keyWord.Trim(), out var memberId))
                        {
                            App.ShowMessage("参数错误，必须在关键词中指定画师 id（纯数字）");
                            return new ImageItems();
                        }
                        //member id
                        query = $"{HomeUrl}/member_illust.php?id={memberId}&p={page}";
                        break;
                    }
                    case SearchTypeEnum.Day:
                        query = $"{HomeUrl}/ranking.php?mode=daily&p={page}";
                        break;
                    case SearchTypeEnum.Week:
                        query = $"{HomeUrl}/ranking.php?mode=weekly&p={page}";
                        break;
                    case SearchTypeEnum.Month:
                        query = $"{HomeUrl}/ranking.php?mode=monthly&p={page}";
                        break;
                }
            }
            var net = Net.CreatNewWithRelatedCookie();
            var pageString = await net.Client.GetStringAsync(query);


            // ----------------images---------------------------
            var imgs = new ImageItems();

            var doc = new HtmlDocument();
            doc.LoadHtml(pageString);

            // new 
            var imgnodes = doc.DocumentNode.SelectNodes("//li[@class='image-item']");
            if (imgnodes == null) return null;

            foreach (var imglinode in imgnodes)
            {
                var img = new ImageItem();
                img.Site = this;
                img.Net = Net.CreatNewWithRelatedCookie();
                img.ThumbnailReferer = query;
                var imgel = imglinode.SelectSingleNode("a/div/img").OuterHtml;
                var imgdoc = new HtmlDocument();
                imgdoc.LoadHtml(imgel);
                var i2 = imgdoc.DocumentNode.SelectSingleNode("img");
                img.ThumbnailUrl = i2?.GetAttributeValue("data-src", "");
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
                var user = usernode?.GetAttributeValue("data-user_name","");
                img.Author = user;

                var link = imglinode.SelectSingleNode("a")?.GetAttributeValue("href", "");
                //var countdiv = imglinode.SelectSingleNode("./div[@class='page-count']");

                var fulllink = $"{HomeUrl}{link?.Replace("&amp;", "&")}";
                //if (countb) fulllink = fulllink.Replace("mode=medium", "mode=manga");
                img.DetailUrl = fulllink;

                var subnet = Net.CreatNewWithRelatedCookie();
                img.GetDetailAction = async () =>
                {
                    try
                    {
                        var subpage = await subnet.Client.GetStringAsync(fulllink);
                        var subdoc = new HtmlDocument();
                        subdoc.LoadHtml(subpage);

                        img.Net = Net.CreatNewWithRelatedCookie();
                        img.FileReferer = fulllink;

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
                        img.FileUrl = $"{jobj.urls?.original}";

                        if (pageCount > 1)
                        {
                            for (var i = 0; i < pageCount; i++)
                            {
                                var subimg = new ImageItem();
                                var rex = new Regex(@"(?<=.*)p\d+(?=[^/]*[^\._]*$)");
                                subimg.FileUrl = rex.Replace(img.FileUrl, $"p{i}");
                                subimg.Site = this;
                                subimg.FileReferer = fulllink.Replace("mode=medium", "mode=manga");
                                subimg.Net = Net.CreatNewWithRelatedCookie();

                                img.ChilldrenItems.Add(subimg);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        App.Log(e);
                    }
                };

                imgs.Add(img);
            }
            return imgs;
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
                var tags = new Dictionary<string, object>();
                var tag = new Dictionary<string, object>();

                var url = string.Format(HomeUrl + "/rpc/cps.php?keyword={0}", para.Keyword);

                
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
