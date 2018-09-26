using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// Booru 引擎图片站点基类 Fixed 20180922
    /// </summary>
    public abstract class BooruSite : MoeSite
    {
        public enum SiteTypeEnum { Xml,Json}

        public virtual SiteTypeEnum SiteType => SiteTypeEnum.Xml;

        public abstract string GetHintQuery(SearchPara para);

        public virtual string GetThumbnailReferer(ImageItem item) => HomeUrl;
        public virtual string GetFileReferer(ImageItem item) => item.DetailUrl;

        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            var list = new AutoHintItems();
            var client = new NetSwap(Settings).Client;
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

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            switch (SiteType)
            {
                case SiteTypeEnum.Xml: return await GetRealPageImagesAsyncFromXml(para);
                case SiteTypeEnum.Json: return await GetRealPageImagesAsyncFromJson(para);
                default: return null;
            }
        }

        public async Task<ImageItems> GetRealPageImagesAsyncFromXml(SearchPara para)
        {
            
            var client = new NetSwap(Settings).Client;
            var query = GetPageQuery(para);
            var xmlstr = await client.GetStreamAsync(query);

            return await Task.Run(() =>
            {
                var xml = XDocument.Load(xmlstr);
                var imageitems = new ImageItems();
                if (xml.Root == null) return imageitems;
                foreach (var post in xml.Root.Elements())
                {
                    var img = new ImageItem();
                    ulong.TryParse(post.Attribute("file_size")?.Value, out var size);
                    img.FileBiteSize = size;
                    img.PreviewUrl = UrlPre + post.Attribute("sample_url")?.Value;
                    img.ThumbnailUrl = UrlPre + post.Attribute("preview_url")?.Value;
                    int.TryParse(post.Attribute("id")?.Value, out var id);
                    img.Id = id;
                    var tags = post.Attribute("tags")?.Value ?? "";
                    foreach (var tag in tags.Split(' '))
                    {
                        if (!string.IsNullOrWhiteSpace(tag)) img.Tags.Add(tag.Trim());
                    }
                    int.TryParse(post.Attribute("width")?.Value, out var width);
                    img.Width = width;
                    int.TryParse(post.Attribute("height")?.Value, out var height);
                    img.Height = height;
                    img.Author = post.Attribute("author")?.Value;
                    img.Source = post.Attribute("source")?.Value;
                    img.IsExplicit = post.Attribute("rating")?.Value.ToLower() != "s";
                    img.FileUrl = UrlPre + post.Attribute("file_url")?.Value;
                    img.DetailUrl = GetDetailPageUrl(img);
                    img.Site = this;
                    double.TryParse(post.Attribute("created_at")?.Value, out var creatat);
                    if (creatat > 0) img.CreatTime = new DateTime(1970, 1, 1, 0, 0, 0, 0) + TimeSpan.FromSeconds(creatat);
                    int.TryParse(post.Attribute("score")?.Value, out var score);
                    img.Score = score;
                    img.FileReferer = img.DetailUrl;
                    img.ThumbnailReferer = GetThumbnailReferer(img);
                    // img.Net = Net;

                    imageitems.Add(img);
                }
                return imageitems;
            });
        }

        public async Task<ImageItems> GetRealPageImagesAsyncFromJson(SearchPara para)
        {
            var client = new NetSwap(Settings).Client;
            var query = GetPageQuery(para);
            var jsonStr = await client.GetStringAsync(query);
            dynamic list = JsonConvert.DeserializeObject(jsonStr);
            var imageitems = new ImageItems();
            if (list == null) return imageitems;
            foreach (var item in list)
            {
                var img = new ImageItem();
                img.ThumbnailUrl = $"{item.preview_file_url}";
                img.FileUrl = $"{item.large_file_url}";
                img.Width = (int) item.image_width;
                img.Height = (int) item.image_height;
                img.Id = (int) item.id;
                img.Score = (int) item.score;
                img.Author = $"{item.uploader_name}";
                var tagsstr = $"{item.tag_string}";
                foreach (var tag in tagsstr.Split(' '))
                {
                    if (!string.IsNullOrWhiteSpace(tag)) img.Tags.Add(tag.Trim());
                }
                img.IsExplicit = $"{item.rating}" == "e";
                img.Site = this;
                img.DetailUrl = GetDetailPageUrl(img);
                img.FileReferer = img.DetailUrl;
                img.ThumbnailReferer = GetThumbnailReferer(img);
                //img.Net = Net;
                imageitems.Add(img);
            }

            return imageitems;
        }

        public virtual string UrlPre => null;

        public virtual string GetDetailPageUrl(ImageItem item) => $"{HomeUrl}/post/show/{item.Id}";

    }
}
