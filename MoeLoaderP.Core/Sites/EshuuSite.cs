using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// e-shuushuu.net Fixed 20180928
    /// </summary>
    public class EshuuSite : MoeSite
    {
        public override string HomeUrl => "https://e-shuushuu.net";

        public override string DisplayName => "E-shuushuu";

        public override string ShortName => "e-shu";

        public EshuuSite()
        {
            SupportState.IsSupportScore = false;
            SubMenu.Add("标签");
            SubMenu.Add("来源");
            SubMenu.Add("画师");
            SubMenu.Add("角色");
            DownloadTypes.Add("原图", 4);
        }

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            var imgs = new MoeItems();
            var url = $"{HomeUrl}/?page={para.PageIndex}";
            if (Net == null) Net = new NetDocker(Settings);
            if (!string.IsNullOrWhiteSpace(para.Keyword))
            {
                url = $"{HomeUrl}/search/process/";
                var i = para.SubMenuIndex;
                var kw = $"{$"\"{para.Keyword.Replace("\"", "")}\"".ToEncodedUrl()}+";
                //e-shuushuu需要将关键词转换为tag id，然后进行搜索
                var mc = new FormUrlEncodedContent(new Pairs
                {
                    {"tags",i == 0 ? kw : ""},
                    {"source", i == 1 ? kw : ""},
                    {"char", i == 3 ? kw : ""},
                    {"artist", i == 2 ? kw : ""},
                    {"postcontent","" },
                    {"txtposter","" }
                });
                var net = Net.CloneWithOldCookie();
                var r = await net.Client.GetAsync(HomeUrl, token);
                if (r.IsSuccessStatusCode == false) return imgs;

                net = net.CloneWithOldCookie();
                net.SetReferer($"{HomeUrl}/search");
                net.HttpClientHandler.AllowAutoRedirect = false; //prevent 303
                var res = await net.Client.PostAsync(url, mc, token);
                var loc303 = res.Headers.Location?.OriginalString;     //todo 无法实现，需要大神

                //http://e-shuushuu.net/search/results/?tags=2
                if (!string.IsNullOrEmpty(loc303)) url = $"{loc303}&page={para.PageIndex}";
                else
                {
                    Extend.ShowMessage("没有搜索到关键词相关的图片", null, Extend.MessagePos.Window);
                    return new MoeItems();
                }
            }

            // images
            var doc = await Net.GetHtmlAsync(url, token);
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='image_thread display']");
            if (nodes == null) return imgs;

            foreach (var imgNode in nodes)
            {
                var img = new MoeItem(this, para);
                var id = imgNode.Attributes["id"]?.Value.Replace("i","");
                img.Id = $"{id}".ToInt();
                var imgHref = imgNode.SelectSingleNode(".//a[@class='thumb_image']");
                var fileUrl = imgHref.Attributes["href"].Value;
                if (fileUrl.StartsWith("/")) fileUrl = $"{HomeUrl}{fileUrl}";
                var previewUrl = imgHref.SelectSingleNode("img").Attributes["src"].Value;
                if (previewUrl.StartsWith("/")) previewUrl = HomeUrl + previewUrl;
                img.Urls.Add(new UrlInfo("缩略图", 1, previewUrl, HomeUrl));
                var meta = imgNode.SelectSingleNode(".//div[@class='meta']");
                img.Date = meta.SelectSingleNode(".//dd[2]").InnerText.ToDateTime();
                var dimension = meta.SelectSingleNode(".//dd[4]").InnerText;
                try
                {
                    var dms = dimension.Substring(0, dimension.IndexOf('(')).Trim();
                    img.Width = dms.Substring(0, dms.IndexOf('x')).ToInt();
                    img.Height = dms.Substring(dms.IndexOf('x') + 1).ToInt();
                }
                catch
                {
                    // ignored
                }

                try
                {
                    var tags = meta.SelectNodes(".//span[@class='tag']/a");
                    foreach (var tag in tags)
                    {
                        if (string.IsNullOrWhiteSpace(tag.InnerText)) continue;
                        img.Tags.Add(tag.InnerText);
                    }

                    img.Uploader = tags.LastOrDefault()?.InnerText;
                }
                catch
                {
                    /*..*/
                }

                var detail = $"{HomeUrl}/image/{id}";
                img.DetailUrl = detail;
                img.Urls.Add(new UrlInfo("原图", 4, fileUrl, detail));
                img.OriginString = imgNode.OuterHtml;
                imgs.Add(img);
            }

            return imgs;
        }

        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            if (Net == null) Net = new NetDocker(para.Site.Settings);
            //type 1 tag 2 source 3 artist | chara no type
            var items = new AutoHintItems();

            //chara without hint
            if (para.SubMenuIndex == 3) return items;
            var url = $"{HomeUrl}/httpreq.php?mode=tag_search&tags={para.Keyword}&type={para.SubMenuIndex + 1}";
            var res = await Net.Client.GetAsync(url, token);
            var txt = await res.Content.ReadAsStringAsync();
            var lines = txt.Split('\n');
            for (var i = 0; i < lines.Length && i < 8; i++)
            {
                if (lines[i].Trim().Length > 0)
                {
                    items.Add(new AutoHintItem
                    {
                        Word = lines[i].Trim().Replace("\"", "")
                    });
                }
            }
            return items;
        }
    }
}
