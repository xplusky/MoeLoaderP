using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// Booru 引擎图片站点基类 Fixed 20180922
    /// </summary>
    public abstract class BooruSite : MoeSite
    {
        public enum SiteTypeEnum { Xml, Json }
        public virtual SiteTypeEnum SiteType => SiteTypeEnum.Xml;
        public abstract string GetHintQuery(SearchPara para);
        public virtual string GetThumbnailReferer(MoeItem item) => HomeUrl;
        public virtual string GetFileReferer(MoeItem item) => item.DetailUrl;

        protected BooruSite()
        {
            DownloadTypes.Add("原图", 4);
            DownloadTypes.Add("预览图", 2);
            if (SiteType == SiteTypeEnum.Xml) DownloadTypes.Add("Jpeg图", 3);
        }

        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            var list = new AutoHintItems();
            var client = new NetDocker(Settings).Client;
            switch (SiteType)
            {
                case SiteTypeEnum.Xml:
                    {
                        var xmlstr = await client.GetStringAsync(GetHintQuery(para));
                        var xml = new XmlDocument();
                        xml.LoadXml(xmlstr);
                        var root = xml.SelectSingleNode("tags");
                        if (root == null) return list;
                        foreach (XmlElement child in root.ChildNodes)
                        {
                            list.Add(new AutoHintItem
                            {
                                Word = child.GetAttribute("name"),
                                Count = child.GetAttribute("count")
                            });
                        }
                        return list;
                    }
                case SiteTypeEnum.Json:
                    {
                        var jsonstr = await client.GetStringAsync(GetHintQuery(para));
                        dynamic jsonlist = JsonConvert.DeserializeObject(jsonstr);
                        foreach (var item in jsonlist)
                        {
                            list.Add(new AutoHintItem
                            {
                                Word = $"{item.name}",
                                Count = $"{item.post_count}"
                            });
                        }
                        return list;
                    }
                default: return null;
            }
        }

        public abstract string GetPageQuery(SearchPara para);

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            switch (SiteType)
            {
                case SiteTypeEnum.Xml: return await GetRealPageImagesAsyncFromXml(para, token);
                case SiteTypeEnum.Json: return await GetRealPageImagesAsyncFromJson(para, token);
                default: return null;
            }
        }

        public async Task<MoeItems> GetRealPageImagesAsyncFromXml(SearchPara para, CancellationToken token)
        {

            var client = new NetDocker(Settings).Client;
            var query = GetPageQuery(para);
            var xmlRes = await client.GetAsync(query, token);
            var xmlStr = await xmlRes.Content.ReadAsStreamAsync();
            return await Task.Run(() =>
            {
                var xml = XDocument.Load(xmlStr);
                var imageItems = new MoeItems();
                if (xml.Root == null) return imageItems;
                foreach (var post in xml.Root.Elements())
                {
                    token.ThrowIfCancellationRequested();
                    var img = new MoeItem(this, para)
                    {
                        Id = post.Attribute("id")?.Value.ToInt() ?? 0
                    };

                    var tags = post.Attribute("tags")?.Value ?? "";
                    foreach (var tag in tags.Split(' '))
                    {
                        if (!string.IsNullOrWhiteSpace(tag)) img.Tags.Add(tag.Trim());
                    }

                    img.Width = post.Attribute("width")?.Value.ToInt() ?? 0;
                    img.Height = post.Attribute("height")?.Value.ToInt() ?? 0;
                    img.Uploader = post.Attribute("author")?.Value;
                    img.Source = post.Attribute("source")?.Value;
                    img.IsExplicit = post.Attribute("rating")?.Value.ToLower() != "s";
                    img.DetailUrl = GetDetailPageUrl(img);
                    img.Date = post.Attribute("created_at")?.Value.ToDateTime();
                    img.Site = this;
                    double.TryParse(post.Attribute("created_at")?.Value, out var createAt);
                    if (createAt > 0) img.Date = new DateTime(1970, 1, 1, 0, 0, 0, 0) + TimeSpan.FromSeconds(createAt);
                    int.TryParse(post.Attribute("score")?.Value, out var score);
                    img.Score = score;
                    ulong.TryParse(post.Attribute("file_size")?.Value, out var fileSize);

                    img.Urls.Add(new UrlInfo("缩略图", 1, UrlPre + post.Attribute("preview_url")?.Value, GetThumbnailReferer(img)));
                    img.Urls.Add(new UrlInfo("预览图", 2, UrlPre + post.Attribute("sample_url")?.Value, GetThumbnailReferer(img)));
                    img.Urls.Add(new UrlInfo("Jpeg图", 3, UrlPre + post.Attribute("jpeg_url")?.Value, GetThumbnailReferer(img)));
                    img.Urls.Add(new UrlInfo("原图", 4, UrlPre + post.Attribute("file_url")?.Value, img.DetailUrl)
                    {
                        Md5 = post.Attribute("md5")?.Value,
                        BiteSize = fileSize,
                    });
                    img.OriginString = $"{post}";
                    imageItems.Add(img);
                }

                return imageItems;
            }, token);
        }

        public async Task<MoeItems> GetRealPageImagesAsyncFromJson(SearchPara para, CancellationToken token)
        {
            var list = await new NetDocker(Settings).GetJsonAsync(GetPageQuery(para), token);

            return await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var imageItems = new MoeItems();
                if (list == null) return imageItems;
                foreach (var item in list)
                {
                    token.ThrowIfCancellationRequested();
                    var img = new MoeItem(this, para);

                    img.Width = $"{item.image_width}".ToInt();
                    img.Height = $"{item.image_height}".ToInt();
                    img.Id = $"{item.id}".ToInt();
                    img.Score = $"{item.score}".ToInt();
                    img.Uploader = $"{item.uploader_name}";
                    foreach (var tag in $"{item.tag_string}".Split(' '))
                    {
                        if (!string.IsNullOrWhiteSpace(tag)) img.Tags.Add(tag.Trim());
                    }

                    img.IsExplicit = $"{item.rating}" == "e";
                    img.DetailUrl = GetDetailPageUrl(img);
                    img.Date = $"{item.created_at}".ToDateTime();
                    img.Urls.Add(new UrlInfo("缩略图", 1, $"{item.preview_file_url}", GetThumbnailReferer(img)));
                    img.Urls.Add(new UrlInfo("预览图", 2, $"{item.large_file_url}", GetThumbnailReferer(img)));
                    img.Urls.Add(new UrlInfo("原图", 4, $"{item.file_url}", img.DetailUrl));

                    img.Copyright = $"{item.tag_string_copyright}";
                    img.Character = $"{item.tag_string_character}";
                    img.Artist = $"{item.tag_string_artist}";
                    img.OriginString = $"{item}";
                    imageItems.Add(img);
                }

                return imageItems;
            }, token);
        }

        public virtual string UrlPre => null;

        public virtual string GetDetailPageUrl(MoeItem item) => $"{HomeUrl}/post/show/{item.Id}";

    }
}
