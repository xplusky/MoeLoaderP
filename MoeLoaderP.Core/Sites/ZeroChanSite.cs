using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// zerochan.net fixed 20200323
    /// </summary>
    public class ZeroChanSite : MoeSite
    {
        public override string HomeUrl => "http://www.zerochan.net";

        public override string DisplayName => "Zerochan";

        public override string ShortName => "zerochan";
        
        public ZeroChanSite()
        {
            DownloadTypes.Add("原图", DownloadTypeEnum.Origin);
            DownloadTypes.Add("预览图", DownloadTypeEnum.Medium);
            Config = new MoeSiteConfig
            {
                IsSupportKeyword = true,
                IsSupportResolution = true,
                IsSupportScore = true,
                ImageOrders = new ImageOrders()
                {
                    new(){Name = "最新", Order = ImageOrderBy.Date},
                    new(){Name = "最热", Order = ImageOrderBy.Popular}
                }
            };

        }
        
        public override async Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
        {
            var net = GetCloneNet();

            string url = $"{HomeUrl}/";

            if (!para.Keyword.IsEmpty())
            {
                url += para.Keyword.Trim().Replace(" ", "+");
            }

            var pairs = new Pairs();
            if (para.OrderBy?.IsDefault == false)
            {
                switch (para.OrderBy?.Order)
                {
                    case null:
                        break;
                    case ImageOrderBy.Date:
                        pairs.Add("s", "id");
                        break;
                    case ImageOrderBy.Popular:
                        pairs.Add("s", "fav");
                        break;
                }
            }
            
            pairs.Add("p",para.PageIndex.ToString());
            
            var doc = await net.GetHtmlAsync(url, pairs , true,token);

            // images
            var imgs = new SearchedPage();
            var nodes = doc.DocumentNode.SelectSingleNode("//ul[@id='thumbs2']").SelectNodes(".//li");
            if (nodes == null) return null;

            foreach (var imgNode in nodes)
            {
                var img = new MoeItem(this, para);
                var mo = imgNode.SelectSingleNode(".//b")?.InnerText?.Trim();
                if (mo?.ToLower().Trim().Contains("members only") == true) continue;
                var strId = imgNode.SelectSingleNode("a").Attributes["href"].Value;
                var fav = imgNode.SelectSingleNode("a/span")?.InnerText;
                if (!fav.IsEmpty()) img.Score = Regex.Replace(fav, @"[^0-9]+", "")?.ToInt() ?? 0;
                var imgHref = imgNode.SelectSingleNode(".//img");
                var previewUrl = imgHref?.Attributes["src"]?.Value;
                //http://s3.zerochan.net/Morgiana.240.1355397.jpg   preview
                //http://s3.zerochan.net/Morgiana.600.1355397.jpg    sample
                //http://static.zerochan.net/Morgiana.full.1355397.jpg   full
                //先加前一个，再加后一个  范围都是00-49
                //string folder = (id % 2500 % 50).ToString("00") + "/" + (id % 2500 / 50).ToString("00");
                var sampleUrl = "";
                var fileUrl = "";
                if (!previewUrl.IsEmpty())
                {
                    sampleUrl = previewUrl?.Replace("240", "600");
                    fileUrl = Regex.Replace(previewUrl, "^(.+?)zerochan.net/", "https://static.zerochan.net/").Replace("240", "full");
                }

                var resAndFileSize = imgHref?.Attributes["title"]?.Value;
                if (!resAndFileSize.IsEmpty())
                {
                    foreach (var s in resAndFileSize.Split(' '))
                    {
                        if (!s.Contains('x')) continue;
                        var res = s.Split('x');
                        if (res.Length != 2) continue;
                        img.Width = res[0].ToInt();
                        img.Height = res[1].ToInt();
                    }
                }
                var title = imgHref?.Attributes["alt"]?.Value;

                //convert relative url to absolute
                if (!fileUrl.IsEmpty() && fileUrl.StartsWith("/")) fileUrl = $"{HomeUrl}{fileUrl}";
                if (sampleUrl != null && sampleUrl.StartsWith("/")) sampleUrl = HomeUrl + sampleUrl;

                img.Description = title;
                img.Title = title;
                img.Id = strId[1..].ToInt();

                img.Urls.Add( DownloadTypeEnum.Thumbnail, previewUrl, HomeUrl);
                img.Urls.Add(DownloadTypeEnum.Medium, sampleUrl, HomeUrl);
                img.Urls.Add(DownloadTypeEnum.Origin, fileUrl, img.DetailUrl);
                img.DetailUrl = $"{HomeUrl}/{img.Id}";

                img.OriginString = imgNode.OuterHtml;
                imgs.Add(img);
            }
            token.ThrowIfCancellationRequested();
            return imgs;
        }
        
        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            var kw = para.Keyword.Replace(" ", "+");

            var net = GetCloneNet();
            var api = $"{HomeUrl}/suggest?q={kw}&limit=10";
            var str = await net.GetStringAsync(api,token: token);
            if (str == null) return null;
            var aitems = new AutoHintItems();
            foreach (var s in str.Split("\n"))
            {
                if(s.IsEmpty())continue;
                var resultsp = s.Split("|");
                if(resultsp.Length==0)continue;
                
                var aitem = new AutoHintItem()
                {
                    Word = resultsp[0]
                };
                aitems.Add(aitem);
            }

            return aitems;
        }

    }
}
