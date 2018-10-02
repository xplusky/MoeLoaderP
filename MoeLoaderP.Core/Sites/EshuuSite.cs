using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// e-shuushuu.net Fixed 20180928
    /// </summary>
    public class EshuuSite : MoeSite
    {
        public override string HomeUrl => "http://e-shuushuu.net";

        public override string DisplayName => "E-shuushuu";

        public override string ShortName => "e-shu";

        public EshuuSite()
        {
            SurpportState.IsSupportScore = false;
            SurpportState.IsSupportKeyword = false;
            SubMenu.Add("标签");
            SubMenu.Add("来源");
            SubMenu.Add("画师");
            SubMenu.Add("角色");
        }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            // page source
            var url =  $"{HomeUrl}/?page={para.PageIndex}";
            if (Net == null)
            {
                Net = new NetSwap(Settings);
            }

            if (!string.IsNullOrWhiteSpace(para.Keyword))
            {
                url = $"{HomeUrl}/search/process/";
                //multi search
                string data;
                switch (SubListIndex)
                {
                    default:
                        data = $"tags={para.Keyword}&source=&char=&artist=&postcontent=&txtposter=";
                        break;
                    case 1:
                        data = $"tags=&source={para.Keyword}&char=&artist=&postcontent=&txtposter=";
                        break;
                    case 2:
                        data = $"tags=&source=&char=&artist={para.Keyword}&postcontent=&txtposter=";
                        break;
                    case 3:
                        data = $"tags=&source=&char={para.Keyword}&artist=&postcontent=&txtposter=";
                        break;
                }

                //e-shuushuu需要将关键词转换为tag id，然后进行搜索 todo 这里要测试
                var net = new NetSwap(Settings);
                net.HttpClientHandler.AllowAutoRedirect = false; //prevent 303
                var res = await net.Client.GetAsync(url, token);
                var loc = res.Headers.Location;

                //http://e-shuushuu.net/search/results/?tags=2
                if (!string.IsNullOrEmpty(loc.OriginalString))
                {
                    //非完整地址，需要前缀
                    url = $"{loc}&page={para.PageIndex}";
                }
                else
                {
                    Extend.ShowMessage("没有搜索到关键词相关的图片（每个关键词前后需要加双引号如 \"sakura\"）");
                    return new ImageItems();
                }
            }
            
            var pageString = await Net.Client.GetStringAsync(url);

            // images
            var images = new ImageItems();
            var doc = new HtmlDocument();
            doc.LoadHtml(pageString);
            //retrieve all elements via xpath
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='image_thread display']");
            if (nodes == null)
            {
                return images;
            }
            foreach (var imgNode in nodes)
            {
                var item = new ImageItem();
                var id = imgNode.Attributes["id"].Value;
                int.TryParse(id.Substring(1), out var ido);
                item.Id = ido;
                var imgHref = imgNode.SelectSingleNode(".//a[@class='thumb_image']");
                var fileUrl = imgHref.Attributes["href"].Value;
                if (fileUrl.StartsWith("/")) fileUrl = HomeUrl + fileUrl;
                item.FileUrl = fileUrl;
                var previewUrl = imgHref.SelectSingleNode("img").Attributes["src"].Value;
                if (previewUrl.StartsWith("/")) previewUrl = HomeUrl + previewUrl;
                item.ThumbnailUrl = previewUrl;
                item.ThumbnailReferer = HomeUrl;
                var meta = imgNode.SelectSingleNode(".//div[@class='meta']");
                var date = meta.SelectSingleNode(".//dd[2]").InnerText;
                var fileSize = meta.SelectSingleNode(".//dd[3]").InnerText;
                var dimension = meta.SelectSingleNode(".//dd[4]").InnerText;
                try
                {
                    //706x1000 (0.706 MPixel)
                    var dms = dimension.Substring(0, dimension.IndexOf('(')).Trim();
                    item.Width = int.Parse(dms.Substring(0, dms.IndexOf('x')));
                    item.Height = int.Parse(dms.Substring(dms.IndexOf('x') + 1));
                }
                catch {/*..*/ }
                try
                {
                    var tags = meta.SelectSingleNode(".//dd[5]").InnerText.Replace("\t","").Replace("\n","");
                    var regex = new Regex("\"([^\"]*)\"");
                    foreach (Match match in regex.Matches(tags))
                    {
                        if (string.IsNullOrWhiteSpace(match.Value)) continue;
                        item.Tags.Add(match.Value);
                    }
                }
                catch { /*..*/ }
                item.DetailUrl = $"{HomeUrl}/image/{ido}";
                item.Site = this;

                images.Add(item);
            }

            return images;
        }

        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            //type 1 tag 2 source 3 artist | chara no type
            var items = new AutoHintItems();

            //chara without hint
            if (SubListIndex == 3) return items;
            var url = $"{HomeUrl}/httpreq.php?mode=tag_search&tags={para.Keyword}&type={SubListIndex + 1}";
            var txt = await Net.Client.GetStringAsync(url);
            var lines = txt.Split('\n');
            for (var i = 0; i < lines.Length && i < 8; i++)
            {
                if (lines[i].Trim().Length > 0)
                {
                    items.Add(new AutoHintItem
                    {
                        Word = lines[i].Trim()
                    });
                }
            }
            return items;
        }


    }
}
