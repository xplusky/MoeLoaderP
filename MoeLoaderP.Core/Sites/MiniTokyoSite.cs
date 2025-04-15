using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     www.minitokyo.net
/// </summary>
public class MiniTokyoSite : MoeSite
{
    private readonly string[] _pass = {"minipass", "minipass3"};

    private readonly string[] _user = {"miniuser2", "miniuser3"};

    public MiniTokyoSite()
    {
        Config.IsSupportKeyword = true;
        Config.IsSupportResolution = true;
        Config.IsSupportScore = true;

        //var lv3 = new Categories(Config, "最新", "最热");
        //Lv2Cat.Add(new Category(Config, "壁纸", lv3));
        //Lv2Cat.Add(new Category(Config, "扫描图", lv3));
        //Lv2Cat.Add(new Category(Config, "手机壁纸", lv3));
        //Lv2Cat.Add(new Category(Config, "Indy Art", lv3));

        Lv2Cat = new Categories(Config);
        Lv2Cat.Adds("最新", "壁纸", "扫描图", "手机壁纸", "Indy Art");
        Lv2Cat.EachSubAdds("最新", "最热");
        DownloadTypes.Add("原图", DownloadTypeEnum.Origin);
    }

    public override string HomeUrl => "http://www.minitokyo.net";

    public static string HomeGalleryUrl => "http://gallery.minitokyo.net";
    public static string HomeBrowseUrl => "http://browse.minitokyo.net";
    public override string DisplayName => "MiniTokyo";

    public override string ShortName => "minitokyo";

    public static string GetSort(SearchPara para)
    {
        // 修改为与网站tabs顺序一致
        var sorts = new[] {"wallpapers", "mobile", "indy-art", "scans"};
        
        // 如果UI顺序是：最新(0), 壁纸(1), 扫描图(2), 手机壁纸(3), Indy Art(4)
        // 需要重新映射索引
        switch(para.Lv2MenuIndex)
        {
            case 1: return "wallpapers"; // 壁纸
            case 2: return "scans";      // 扫描图
            case 3: return "mobile";     // 手机壁纸
            case 4: return "indy-art";   // Indy Art
            default: return "wallpapers"; // 默认或最新
        }
    }

    public static string GetOrder(SearchPara para)
    {
        var orders = new[] {"id", "favorites"};
        return orders[para.Lv3MenuIndex];
    }

    public async Task Login(CancellationToken token)
    {
        Net = new NetOperator(Settings, this);
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
            IsUserLogin = true;
        }
        else
        {
            Net = null;
            IsUserLogin = false;
            Ex.ShowMessage("MiniTokyo 登录失败");
        }
    }

    public override async Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
    {
        if (!IsUserLogin) await Login(token);
        if (!IsUserLogin) return null;

        var imgs = new SearchedPage();
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
            var net = GetCloneNet();
            net.SetReferer(HomeUrl);
            net.HttpClientHandler.AllowAutoRedirect = false;
            var res = await net.Client.GetAsync(q, token);
            var loc303 = res.Headers.Location?.OriginalString;
            var net2 = GetCloneNet();
            var doc1 = await net2.GetHtmlAsync($"{HomeUrl}{loc303}", token: token);
            var tabnodes = doc1.DocumentNode.SelectNodes("*//ul[@id='tabs']//a");
            var url = tabnodes[1].Attributes["href"]?.Value;
            var reg = new Regex(@"(?:^|\?|&)tid=(\d*)(?:&|$)");
            var tid = reg.Match(url ?? "").Groups[0].Value;
            var indexs = new[] { 1, 1, 3, 2, 4 }; //Index Definition: 1-Wallpapers, 2-IndyArt, 3-Scans, 4-MobileWallpaper
            query =
                $"{HomeBrowseUrl}/gallery{tid}index={indexs[para.Lv2MenuIndex]}&order={GetOrder(para)}&display=thumbnails&page={para.PageIndex}";
        }

        var doc = await Net.GetHtmlAsync(query, token: token);
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
            var previewUrl =
                $"{api2}/view{sampleUrl[sampleUrl.IndexOf('/', sampleUrl.IndexOf(".net/", StringComparison.Ordinal) + 5)..]}";
            var fileUrl =
                $"{api}/downloads{previewUrl[previewUrl.IndexOf('/', previewUrl.IndexOf(".net/", StringComparison.Ordinal) + 5)..]}";
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
        if (!IsUserLogin) await Login(token);
        if (!IsUserLogin) return null;

        var items = new AutoHintItems();
        var url = $"{HomeUrl}/suggest?limit=8&q={para.Keyword}";
        var net = Net.CloneWithCookie();
        net.SetTimeOut(15);
        var txtres = await net.Client.GetAsync(url, token);
        var txt = await txtres.Content.ReadAsStringAsync(token);
        var lines = txt.Split('\n');
        for (var i = 0; i < lines.Length && i < 8; i++)
        {
            if (lines[i].IsEmpty()) continue;
            items.Add(lines[i][..lines[i].IndexOf('|')].Trim());
        }

        return items;
    }
}
