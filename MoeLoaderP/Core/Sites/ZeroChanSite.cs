using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// zerochan.net site todo 需修复下载问题 清理中
    /// </summary>
    public class ZeroChanSite : MoeSite
    {
        public override string HomeUrl => "http://www.zerochan.net";

        public override string DisplayName => "Zerochan";

        public override string ShortName => "zerochan";
        
        private readonly string[] _user = { "zerouser1" };
        private readonly string[] _pass = { "zeropass" };
        private string _beforeWord = "", _beforeUrl = "";

        public ZeroChanSite()
        {
            SurpportState.IsSupportScore = false;
        }

        private bool IsLogon { get; set; }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            // logon
            if (!IsLogon)
            {
                Net = new NetSwap(Settings,HomeUrl);
                var index = new Random().Next(0, _user.Length);
                var loginurl = "https://www.zerochan.net/login";

                var response = await Net.Client.PostAsync(loginurl, 
                    new StringContent($"ref=%2F&login=Login&name={_user[index]}&password={_pass[index]}"), token);

                if (response.IsSuccessStatusCode) IsLogon = true;
            }
            if(!IsLogon) return new ImageItems();

            // get page source
            var pageString = "";
            var url = HomeUrl + (para.Keyword.Length > 0 ? $"/search?q={para.Keyword}&" : "/?") + $"p={para.PageIndex}";

            if (!_beforeWord.Equals(para.Keyword, StringComparison.CurrentCultureIgnoreCase))
            {
                // 301
                var respose = await Net.Client.GetAsync(url);
                if (respose.IsSuccessStatusCode)
                    _beforeUrl = respose.Headers.Location.AbsoluteUri;
                else
                {
                    throw new Exception("搜索失败，请检查您输入的关键词");
                }

                pageString = await respose.Content.ReadAsStringAsync();

                _beforeWord = para.Keyword;
            }
            else
            {
                //Net.Client.DefaultRequestHeaders.Referrer = new Uri(beforeUrl);
                url = string.IsNullOrWhiteSpace(para.Keyword) ? url : _beforeUrl + "?p=" + para.PageIndex;
                pageString = await Net.Client.GetStringAsync(url);
            }

            // images 
            var imgs = new ImageItems();

            var doc = new HtmlDocument();
            doc.LoadHtml(pageString);
            //retrieve all elements via xpath

            HtmlNodeCollection nodes;
            try
            {
                nodes = doc.DocumentNode.SelectSingleNode("//ul[@id='thumbs2']").SelectNodes(".//li");
            }
            catch
            {
                throw new Exception("没有搜索到图片");
            }

            foreach (var imgNode in nodes)
            {
                var strId = imgNode.SelectSingleNode("a").Attributes["href"].Value;
                int.TryParse(strId.Substring(1),out var id);
                var imgHref = imgNode.SelectSingleNode(".//img");
                var previewUrl = imgHref?.Attributes["src"]?.Value;
                //http://s3.zerochan.net/Morgiana.240.1355397.jpg   preview
                //http://s3.zerochan.net/Morgiana.600.1355397.jpg    sample
                //http://static.zerochan.net/Morgiana.full.1355397.jpg   full
                //先加前一个，再加后一个  范围都是00-49
                //string folder = (id % 2500 % 50).ToString("00") + "/" + (id % 2500 / 50).ToString("00");
                var sample_url = previewUrl?.Replace("240", "600");
                var fileUrl = imgNode.SelectSingleNode("p//img")?.ParentNode?.Attributes["href"]?.Value;
                var title = imgHref?.Attributes["title"].Value;
                var dimension = title?.Substring(0, title.IndexOf(' '));
                var fileSize = title?.Substring(title.IndexOf(' ')).Trim();
                var tags = imgHref?.Attributes["alt"].Value;

                var img = GenerateImg(fileUrl, sample_url, previewUrl, dimension, tags?.Trim(), fileSize, id);

                if (img != null)
                {
                    img.Site = this;
                    imgs.Add(img);
                }
            }

            return imgs;
        }

        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            //http://www.zerochan.net/suggest?q=tony&limit=8
            var re = new AutoHintItems();

            var url = HomeUrl + "/suggest?limit=8&q=" + para.Keyword;

            Net.Client.DefaultRequestHeaders.Referrer =  new Uri(HomeUrl);

            var txt = await Net.Client.GetStringAsync(url);

            var lines = txt.Split('\n');
            for (var i = 0; i < lines.Length && i < 8; i++)
            {
                //Tony Taka|Mangaka|
                if (lines[i].Trim().Length > 0)
                    re.Add(new AutoHintItem { Word = lines[i].Substring(0, lines[i].IndexOf('|')).Trim() });
            }

            return re;
        }


        private ImageItem GenerateImg(string file_url, string sample_url, string preview_url, string dimension, string tags, string file_size, int id)
        {
            //int intId = int.Parse(id.Substring(1));

            int width = 0, height = 0;
            try
            {
                //706x1000
                width = int.Parse(dimension.Substring(0, dimension.IndexOf('x')));
                height = int.Parse(dimension.Substring(dimension.IndexOf('x') + 1));
            }
            catch { }

            //convert relative url to absolute
            if (!string.IsNullOrWhiteSpace(file_url) && file_url.StartsWith("/"))
                file_url = HomeUrl + file_url;
            if (sample_url!=null && sample_url.StartsWith("/"))
                sample_url = HomeUrl + sample_url;

            var img = new ImageItem()
            {
                //Date = "N/A",
                FileSize = file_size?.ToUpper(),
                Description = tags,
                Id = id,
                //IsViewed = isViewed,
                JpegUrl = file_url,
                FileUrl = file_url,
                PreviewUrl = sample_url,
                ThumbnailUrl = preview_url,
                //Score = 0,
                //Size = width + " x " + height,
                Width = width,
                Height = height,
                //Source = "",
                // todo TagsText = tags,
                DetailUrl = HomeUrl + "/" + id,
            };
            if (file_size != null)
            {
                img.FileSize = new Regex(@"\d+").Match(img.FileSize).Value;
                var fs = Convert.ToInt32(img.FileSize);
                img.FileSize = (fs > 1024 ? (fs / 1024.0).ToString("0.00MB") : fs.ToString("0KB"));
                
            }
            img.FileReferer = HomeUrl;

            return img;
        }
    }
}
