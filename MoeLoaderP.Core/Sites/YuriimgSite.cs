using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// yuriimg.com Last change 20200311
    /// </summary>
    public class YuriimgSite : MoeSite
    {
        public override string HomeUrl => "http://yuriimg.com";
        public override string ShortName => "yuriimg";
        public override string DisplayName => "Yuriimg（百合居）";

        public YuriimgSite()
        {
            SupportState.IsSupportAutoHint = false;
            SupportState.IsSupportRating = false;

            DownloadTypes.Add("原图", 4);
            DownloadTypes.Add("预览图", 2);
        }

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            Net = new NetDocker(Settings, HomeUrl);

            var url = para.HasKeyword ? $"{HomeUrl}/search/index/tags/{para.Keyword}/p/{para.PageIndex}.html"
                : $"{HomeUrl}/post/{para.PageIndex}.html";
            var list = new MoeItems();
            var document = await Net.GetHtmlAsync(url, token);
            var imageItemsNodes = document.DocumentNode.SelectNodes("//*[@class='image-list cl']");
            if (imageItemsNodes == null) return list;
            foreach (var imageItem in imageItemsNodes)
            {
                var imgNode = imageItem.SelectSingleNode("./div[1]/img");
                var tags = imgNode.Attributes["alt"]?.Value;
                var img = new MoeItem(this, para)
                {
                    Height = imageItem.SelectSingleNode(".//div[@class='image']")?.Attributes["data-height"]?.Value.ToInt() ?? 0,
                    Width = imageItem.SelectSingleNode(".//div[@class='image']")?.Attributes["data-width"]?.Value.ToInt() ?? 0,
                    Uploader = imageItem.SelectSingleNode(".//a[@class='avatar_name']")?.InnerText,
                    Description = tags,
                    Id = StringToInt(imgNode.Attributes["id"]?.Value),
                    DetailUrl = HomeUrl + imgNode.Attributes["data-href"]?.Value,
                    Score = imageItem.SelectSingleNode(".//span[@class='num']")?.InnerText.ToInt() ?? 0,
                    Site = this,
                    Net = null
                };
                var imgprurl = $"{imgNode.Attributes["data-original"]?.Value}".Replace("!single", "!320px.jpg");
                img.Urls.Add(new UrlInfo("缩略图", 1, imgprurl, HomeUrl));

                if (!string.IsNullOrWhiteSpace(tags)) foreach (var tag in tags.Split(' '))
                {
                    //if (tag.Contains("画师：")) continue;
                    if (!string.IsNullOrWhiteSpace(tag)) img.Tags.Add(tag.Trim());
                }

                img.GetDetailTaskFunc = async () =>
                {
                    try
                    {
                        var doc = await Net.GetHtmlAsync(img.DetailUrl, token);
                        var showIndex = doc.DocumentNode.SelectSingleNode("//div[@class='logo']");
                        var imgDownNode = showIndex.SelectSingleNode("//div[@class='img-control']");
                        var nodeHtml = showIndex.OuterHtml;
                        img.DateString = TimeConvert(nodeHtml);

                        img.Source = nodeHtml.Contains("pixiv page") ?
                            showIndex.SelectSingleNode(".//a[@target='_blank']").Attributes["href"].Value :
                            Regex.Match(nodeHtml, @"(?<=源地址).*?(?=</p>)").Value.Trim();
                        img.Urls.Add(new UrlInfo("缩略图", 1, doc.DocumentNode.SelectSingleNode("//figure[@class=\'show-image\']/img").Attributes["src"].Value, HomeUrl));
                        var preview = doc.DocumentNode.SelectSingleNode("//figure[@class=\'show-image\']/img").Attributes["src"].Value;
                        string file;
                        if (Regex.Matches(imgDownNode.OuterHtml, "href").Count > 1)
                        {
                            file = HomeUrl + imgDownNode.SelectSingleNode("./a[1]").Attributes["href"].Value;
                        }
                        else
                        {
                            file = HomeUrl + imgDownNode.SelectSingleNode("./a").Attributes["href"].Value;
                        }
                        img.Urls.Add(new UrlInfo("原图", 4, file, HomeUrl));
                        img.Urls.Add(new UrlInfo("预览图", 2, preview.Length > 0 ? preview : file, HomeUrl));
                    }
                    catch (Exception ex)
                    {
                        Extend.Log(ex);
                    }

                };
                img.OriginString = $"{imageItem.OuterHtml}";
                list.Add(img);

            }
            token.ThrowIfCancellationRequested();
            return list;

        }

        private static int StringToInt(string id)
        {
            var str = id.Trim();   // 去掉字符串首尾处的空格
            var charBuf = str.ToArray();   // 将字符串转换为字符数组
            var charToAscii = new ASCIIEncoding();
            var txdBuf = charToAscii.GetBytes(charBuf);
            var idOut = BitConverter.ToInt32(txdBuf, 0);
            return idOut;
        }

        private static string TimeConvert(string html)
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
