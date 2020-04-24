using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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
            var net = new NetDocker(Settings);
            switch (SiteType)
            {
                case SiteTypeEnum.Xml:
                    var xml =await  net.GetXmlAsync(GetHintQuery(para),token);
                    if (xml == null) return list;
                    var root = xml.SelectSingleNode("tags");
                    if (root?.ChildNodes == null) return list;
                    foreach (XmlElement child in root.ChildNodes)
                    {
                        list.Add(new AutoHintItem
                        {
                            Word = child.GetAttribute("name"),
                            Count = child.GetAttribute("count")
                        });
                    }

                    return list;
                case SiteTypeEnum.Json:
                    var json = await net.GetJsonAsync(GetHintQuery(para), token);
                    foreach (var item in Extend.CheckListNull(json))
                    {
                        list.Add(new AutoHintItem
                        {
                            Word = $"{item.name}",
                            Count = $"{item.post_count}"
                        });
                    }
                    return list;
            }

            return list;
        }

        public abstract string GetPageQuery(SearchPara para);

        public virtual Func<MoeItem,SearchPara, CancellationToken, Task> GetDetailTaskFunc { get; set; } 

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
            var xml = XDocument.Load(xmlStr);
            var imageItems = new MoeItems();
            if (xml.Root == null) return imageItems;
            foreach (var post in xml.Root.Elements())
            {
                token.ThrowIfCancellationRequested();
                var img = new MoeItem(this, para);
                var tags = post.Attribute("tags")?.Value ?? "";
                foreach (var tag in tags.Split(' '))
                {
                    if (!tag.IsNaN()) img.Tags.Add(tag.Trim());
                }

                img.Id = post.Attribute("id")?.Value.ToInt() ?? 0;
                img.Width = post.Attribute("width")?.Value.ToInt() ?? 0;
                img.Height = post.Attribute("height")?.Value.ToInt() ?? 0;
                img.Uploader = post.Attribute("author")?.Value;
                img.Source = post.Attribute("source")?.Value;
                img.IsExplicit = post.Attribute("rating")?.Value.ToLower() != "s";
                img.DetailUrl = GetDetailPageUrl(img);
                img.Date = post.Attribute("created_at")?.Value.ToDateTime();
                if (img.Date == null) img.DateString = post.Attribute("created_at")?.Value;
                img.Score = post.Attribute("score")?.Value.ToInt() ?? 0;
                img.Urls.Add(1, $"{UrlPre}{post.Attribute("preview_url")?.Value}", GetThumbnailReferer(img));
                img.Urls.Add(2, $"{UrlPre}{post.Attribute("sample_url")?.Value}", GetThumbnailReferer(img));
                img.Urls.Add(3, $"{UrlPre}{post.Attribute("jpeg_url")?.Value}", GetThumbnailReferer(img));
                img.Urls.Add(4, $"{UrlPre}{post.Attribute("file_url")?.Value}", img.DetailUrl);
                img.OriginString = $"{post}";
                if (GetDetailTaskFunc != null)
                {
                    img.GetDetailTaskFunc = async () => await GetDetailTaskFunc(img, para,token);
                }
                imageItems.Add(img);
            }

            var count = xml.Root.Attribute("count")?.Value.ToInt();
            var offset = xml.Root.Attribute("offset")?.Value.ToInt();
            Extend.ShowMessage($"共搜索到{count}张图片，当前第{offset+1}张，第{para.PageIndex}页，共{count / para.Count}页", null, Extend.MessagePos.InfoBar);
            return imageItems;
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
                    foreach (var tag in $"{item.tag_string}".Split(' ').SkipWhile(string.IsNullOrWhiteSpace))
                    {
                        img.Tags.Add(tag.Trim());
                    }

                    img.IsExplicit = $"{item.rating}" == "e";
                    img.DetailUrl = GetDetailPageUrl(img);
                    img.Date = $"{item.created_at}".ToDateTime();
                    if (img.Date == null) img.DateString = $"{item.created_at}";
                    img.Urls.Add(1, $"{item.preview_file_url}", GetThumbnailReferer(img));
                    img.Urls.Add(2, $"{item.large_file_url}", GetThumbnailReferer(img));
                    img.Urls.Add(4, $"{item.file_url}", img.DetailUrl);
                    img.Copyright = $"{item.tag_string_copyright}";
                    img.Character = $"{item.tag_string_character}";
                    img.Artist = $"{item.tag_string_artist}";
                    img.OriginString = $"{item}"; 
                    if (GetDetailTaskFunc != null)
                    {
                        img.GetDetailTaskFunc = async () => await GetDetailTaskFunc(img, para,token);
                    }
                    imageItems.Add(img);
                }

                return imageItems;
            }, token);
        }

        public virtual string UrlPre => null;

        public virtual string GetDetailPageUrl(MoeItem item) => $"{HomeUrl}/post/show/{item.Id}";

    }
}
