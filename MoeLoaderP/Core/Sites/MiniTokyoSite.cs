using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// www.minitokyo.net fixed 20180923
    /// </summary>
    public class MiniTokyoSite : MoeSite
    {
        public override string HomeUrl => "http://www.minitokyo.net";

        public override string DisplayName => "MiniYokyo";
            
        public override string ShortName => "minitokyo";
        
        private readonly string[] _user = { "miniuser2", "miniuser3" };
        private readonly string[] _pass = { "minipass", "minipass3" };

        private string Type => SubListIndex == 0 ? "wallpapers" : "scans";// 1 搜索壁纸  2搜索扫描图
        
        public MiniTokyoSite()
        {
            SubMenu.Add("壁纸");
            SubMenu.Add("扫描图");
        }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            if (Net == null)
            {
                // login
                Net = new NetSwap(Settings, HomeUrl);
                var accIndex = new Random().Next(0, _user.Length);
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"username", _user[accIndex]},
                    {"password", _pass[accIndex]}
                });
                // Net.Client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                await Net.Client.PostAsync("http://my.minitokyo.net/login", content, token);

            }

            // page source
            string query;
            if (string.IsNullOrWhiteSpace(para.Keyword))
            {
                query = $"http://gallery.minitokyo.net/{Type}?order=id&display=extensive&page={para.PageIndex}";
            }
            else
            {
                var page = await Net.Client.GetStringAsync($"{HomeUrl}/search?q={para.Keyword}");
                var urlindex = page.IndexOf("http://browse.minitokyo.net/gallery?tid=", StringComparison.Ordinal);
                var url = page.Substring(urlindex, page.IndexOf('"', urlindex) - urlindex - 1) + (Type.Contains("wallpapers") ? "1" : "3");
                url += "&order=id&display=extensive&page=" + page;
                query = url.Replace("&amp;", "&");
            }
            var imgs = new ImageItems(); 
            var doc = new HtmlDocument();
            using (var pagestream = await Net.Client.GetStreamAsync(query))
            {
                doc.Load(pagestream);
            }
            //retrieve all elements via xpath
            var wallNode = doc.DocumentNode.SelectSingleNode("//ul[@class='wallpapers']");
            var imgNodes = wallNode.SelectNodes(".//li");
            if (imgNodes == null)
            {
                return imgs;
            }

            for (var i = 0; i < imgNodes.Count - 1; i++)
            {
                var item = new ImageItem();
                //最后一个是空的，跳过
                var imgNode = imgNodes[i];

                var detailUrl = imgNode.SelectSingleNode("a").Attributes["href"].Value;
                item.DetailUrl = detailUrl;
                var id = detailUrl.Substring(detailUrl.LastIndexOf('/') + 1);
                item.Id = int.Parse(id);
                var imgHref = imgNode.SelectSingleNode(".//img");
                var sampleUrl = imgHref.Attributes["src"].Value;
                item.ThumbnailUrl = sampleUrl;
                //http://static2.minitokyo.net/thumbs/24/25/583774.jpg preview
                //http://static2.minitokyo.net/view/24/25/583774.jpg   sample
                //http://static.minitokyo.net/downloads/24/25/583774.jpg   full
                var previewUrl = "http://static2.minitokyo.net/view" + sampleUrl.Substring(sampleUrl.IndexOf('/', sampleUrl.IndexOf(".net/", StringComparison.Ordinal) + 5));
                var fileUrl = "http://static.minitokyo.net/downloads" + previewUrl.Substring(previewUrl.IndexOf('/', previewUrl.IndexOf(".net/", StringComparison.Ordinal) + 5));
                item.FileUrl = fileUrl;
                // \n\tMasaru -\n\tMasaru \n\tSubmitted by\n\t\tadri24rukiachan\n\t4200x6034, 4 Favorites\n
                var info = imgNode.SelectSingleNode(".//div").InnerText;
                var infomc = Regex.Match(info, @"^\n\t(?<title>.*?)\s-\n.*?\n\t.*?by\n\t\t(?<author>.*?)\n\t(?<size>\d+x\d+),\s(?<score>\d+)\s");
                var title = infomc.Groups["title"].Value;
                item.Title = title;
                item.Author = infomc.Groups["author"].Value;
                var size = infomc.Groups["size"].Value;
                if (!string.IsNullOrWhiteSpace(size))
                {
                    try
                    {
                        item.Width = int.Parse(size.Substring(0, size.IndexOf('x')));
                        item.Height = int.Parse(size.Substring(size.IndexOf('x') + 1));
                    }
                    catch { /*..*/ }
                }
                
                int.TryParse(infomc.Groups["score"].Value,out var score);
                item.Score = score;
                item.ThumbnailReferer = HomeUrl;
                item.Site = this;
                imgs.Add(item);
            }
            return imgs;
        }

        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            var items = new AutoHintItems();
            var url =  $"{HomeUrl}/suggest?limit=8&q={para.Keyword}";
            var txt = await Net.Client.GetStringAsync(url);
            var lines = txt.Split('\n');
            for (var i = 0; i < lines.Length && i < 8; i++)
            {
                //The Melancholy of Suzumiya Haruhi|Series|Noizi Ito
                if (lines[i].Trim().Length > 0)
                {
                    items.Add(new AutoHintItem
                    {
                        Word = lines[i].Substring(0, lines[i].IndexOf('|')).Trim()
                    });
                }
            }
            return items;
        }

    }
}
