using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// www.minitokyo.net fixed 20210311
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
            return sorts[para.Lv2MenuIndex];
        }

        public string GetOrder(SearchPara para)
        {
            var orders = new[] { "id", "favorites" };
            return orders[para.Lv3MenuIndex];
        }

        public MiniTokyoSite()
        {
            var lv3 = new Categories("最新", "最热");
            Lv2Cat = new Categories()
            {
                new Category("壁纸", lv3),
                new Category("扫描图", lv3),
                new Category("手机壁纸", lv3),
                new Category("Indy Art", lv3)
            };
            DownloadTypes.Add("原图", DownloadTypeEnum.Origin);
        }

        public async Task<bool> IsLogin(CancellationToken token)
        {
            if (Net == null)
            {
                Net = new NetOperator(Settings, HomeUrl);
                Ex.ShowMessage("MiniTokyo 正在自动登录中……", null, Ex.MessagePos.Searching);
                var accIndex = new Random().Next(0, _user.Length);
                var content = new FormUrlEncodedContent(new Pairs
                {
                    {"username", _user[accIndex]},
                    {"password", _pass[accIndex]}
                });
                var p = await Net.Client.PostAsync("http://my.minitokyo.net/login", content, token);
                if (p.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    Net = null;
                    return false;
                }
            }

            return true;
        }

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            if (await IsLogin(token) == false) return null;

            var imgs = new MoeItems();
            string query;
            if (para.Keyword.IsEmpty()) // by new
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
                var net = Net.CreateNewWithOldCookie();
                net.SetReferer(HomeUrl);
                net.HttpClientHandler.AllowAutoRedirect = false;
                var res = await net.Client.GetAsync(q, token);
                var loc303 = res.Headers.Location.OriginalString;
                var net2 = Net.CreateNewWithOldCookie();
                var doc1 = await net2.GetHtmlAsync($"{HomeUrl}{loc303}", token);
                var tabnodes = doc1.DocumentNode.SelectNodes("*//ul[@id='tabs']//a");
                var url = tabnodes[1].Attributes["href"]?.Value;
                var reg = new Regex(@"(?:^|\?|&)tid=(\d*)(?:&|$)");
                var tid = reg.Match(url ?? "").Groups[0].Value;
                var indexs = new [] {1, 3, 1, 2};
                query = $"{HomeBrowseUrl}/gallery{tid}index={indexs[para.Lv2MenuIndex]}&order={GetOrder(para)}&display=thumbnails";

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
                img.Id = detailUrl[(detailUrl.LastIndexOf('/') + 1)..].ToInt();
                var imgHref = node.SelectSingleNode(".//img");
                var sampleUrl = imgHref.Attributes["src"].Value;
                img.Urls.Add(DownloadTypeEnum.Thumbnail, sampleUrl, HomeUrl);
                const string api2 = "http://static2.minitokyo.net";
                const string api = "http://static.minitokyo.net";
                var previewUrl = $"{api2}/view{sampleUrl[sampleUrl.IndexOf('/', sampleUrl.IndexOf(".net/", StringComparison.Ordinal) + 5)..]}";
                var fileUrl = $"{api}/downloads{previewUrl[previewUrl.IndexOf('/', previewUrl.IndexOf(".net/", StringComparison.Ordinal) + 5)..]}";
                img.Urls.Add(DownloadTypeEnum.Origin, fileUrl, HomeUrl);
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
            if (await IsLogin(token) == false) return null;

            var items = new AutoHintItems();
            var url = $"{HomeUrl}/suggest?limit=8&q={para.Keyword}";
            var net = Net.CreateNewWithOldCookie();
            net.SetTimeOut(15);
            var txtres = await net.Client.GetAsync(url, token);
            var txt = await txtres.Content.ReadAsStringAsync();
            var lines = txt.Split('\n');
            for (var i = 0; i < lines.Length && i < 8; i++)
            {
                if (lines[i].IsEmpty()) continue;
                items.Add(lines[i].Substring(0, lines[i].IndexOf('|')).Trim());
            }
            return items;
        }

    }
}
