using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// www.minitokyo.net fixed 20200317
    /// </summary>
    public class MiniTokyoSite : MoeSite
    {
        public override string HomeUrl => "http://www.minitokyo.net";

        public string HomeGalleryUrl => "http://gallery.minitokyo.net";
        public string HomeBrowseUrl => "http://browse.minitokyo.net";
        public override string DisplayName => "MiniTokyo";

        public override string ShortName => "minitokyo";

        private readonly string[] _user = { "miniuser2", "miniuser3" };
        private readonly string[] _pass = { "minipass", "minipass3" };

        public string GetSort(SearchPara para)
        {
            var sorts = new[] {"wallpapers", "scans", "mobile", "indy-art"};
            return sorts[para.SubMenuIndex];
        }

        public string GetOrder(SearchPara para)
        {
            var orders = new [] {"id", "favorites"};
            return orders[para.Lv3MenuIndex];
        }

        public MiniTokyoSite()
        {
            var lv3 = new MoeMenuItems(null, "最新", "最热");
            SubMenu.Add("壁纸", lv3);
            SubMenu.Add("扫描图", lv3);
            SubMenu.Add("手机壁纸", lv3);
            SubMenu.Add("Indy Art", lv3);

            DownloadTypes.Add("原图", 4);
        }

        public bool IsLogin { get; set; }

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            Net = Net == null ? new NetDocker(Settings, HomeUrl) : Net.CloneWithOldCookie();

            if (!IsLogin)
            {
                Extend.ShowMessage("MiniTokyo 正在自动登录中……", null, Extend.MessagePos.Searching);
                var accIndex = new Random().Next(0, _user.Length);
                var content = new FormUrlEncodedContent(new Pairs
                {
                    {"username", _user[accIndex]},
                    {"password", _pass[accIndex]}
                });
                var p = await Net.Client.PostAsync("http://my.minitokyo.net/login", content, token);
                if (p.IsSuccessStatusCode) IsLogin = true;
            }

            var imgs = new MoeItems();
            string query;
            if (para.Keyword.IsNaN()) // by new
            {
                // recent:
                // wall http://gallery.minitokyo.net/wallpapers?display=thumbnails&order=id&page=2
                // mobile http://gallery.minitokyo.net/mobile?order=id&display=thumbnails&page=2
                // indy http://gallery.minitokyo.net/indy-art?order=id&display=thumbnails&page=2
                // scan  http://gallery.minitokyo.net/scans?order=id&display=thumbnails&page=2

                // popular
                // wall http://gallery.minitokyo.net/wallpapers?order=favorites&display=thumbnails&page=2
                // scan http://gallery.minitokyo.net/scans?display=thumbnails&order=favorites&page=2
                query = $"{HomeGalleryUrl}/{GetSort(para)}?order={GetOrder(para)}&display=thumbnails&page={para.PageIndex}";

            }
            else
            {
                var q = $"{HomeUrl}/search?q={para.Keyword}";
                var net = Net.CloneWithOldCookie();
                net.SetReferer(HomeUrl);
                net.HttpClientHandler.AllowAutoRedirect = false;
                var res = await net.Client.GetAsync(q, token);
                var loc303 = res.Headers.Location.OriginalString;
                var net2 = Net.CloneWithOldCookie();
                var doc1 = await net2.GetHtmlAsync($"{HomeUrl}{loc303}", token);
                var tabnodes = doc1.DocumentNode.SelectNodes("*//ul[@id='tabs']//a");
                var url = tabnodes[1].Attributes["href"]?.Value;
                var reg = new Regex(@"(?:^|\?|&)tid=(\d*)(?:&|$)");
                var tid = reg.Match(url ?? "").Groups[0].Value;
                var indexs = new [] {1, 3, 1, 2};
                query = $"{HomeBrowseUrl}/gallery{tid}index={indexs[para.SubMenuIndex]}&order={GetOrder(para)}&display=thumbnails";

            }

            var doc = await Net.GetHtmlAsync(query, token);
            var docnode = doc.DocumentNode;
            var empty = docnode.SelectSingleNode("*//p[@class='empty']")?.InnerText.ToLower().Trim();
            if (empty == "no items to display") return imgs;
            var wallNode = docnode.SelectSingleNode("*//ul[@class='scans']");
            var imgNodes = wallNode.SelectNodes(".//li");
            if (imgNodes == null) return imgs;

            foreach (var node in imgNodes)
            {
                var img = new MoeItem(this, para);
                var detailUrl = node.SelectSingleNode("a").Attributes["href"].Value;
                img.DetailUrl = detailUrl;
                img.Id = detailUrl.Substring(detailUrl.LastIndexOf('/') + 1).ToInt();
                var imgHref = node.SelectSingleNode(".//img");
                var sampleUrl = imgHref.Attributes["src"].Value;
                img.Urls.Add(1, sampleUrl, HomeUrl);
                //http://static2.minitokyo.net/thumbs/24/25/583774.jpg preview
                //http://static2.minitokyo.net/view/24/25/583774.jpg   sample
                //http://static.minitokyo.net/downloads/24/25/583774.jpg   full
                const string api2 = "http://static2.minitokyo.net";
                const string api = "http://static.minitokyo.net";
                var previewUrl = $"{api2}/view{sampleUrl.Substring(sampleUrl.IndexOf('/', sampleUrl.IndexOf(".net/", StringComparison.Ordinal) + 5))}";
                var fileUrl = $"{api}t/downloads{previewUrl.Substring(previewUrl.IndexOf('/', previewUrl.IndexOf(".net/", StringComparison.Ordinal) + 5))}";
                img.Urls.Add(4, fileUrl, HomeUrl);
                img.Title = node.SelectSingleNode("./p/a").InnerText.Trim();
                img.Uploader = node.SelectSingleNode("./p").InnerText.Delete("by ").Trim();
                var res = node.SelectSingleNode("./a/img").Attributes["title"].Value;
                var resi = res?.Split('x');
                if (resi?.Length == 2)
                {
                    img.Width = resi[0].ToInt();
                    img.Height = resi[1].ToInt();
                }
                img.OriginString = node.OuterHtml;
                imgs.Add(img);
            }
            token.ThrowIfCancellationRequested();
            return imgs;
        }

        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            var items = new AutoHintItems();
            var url = $"{HomeUrl}/suggest?limit=8&q={para.Keyword}";
            var txtres = await Net.Client.GetAsync(url, token);
            var txt = await txtres.Content.ReadAsStringAsync();
            var lines = txt.Split('\n');
            for (var i = 0; i < lines.Length && i < 8; i++)
            {
                if (lines[i].IsNaN()) continue;
                items.Add(lines[i].Substring(0, lines[i].IndexOf('|')).Trim());
            }
            return items;
        }

    }
}
