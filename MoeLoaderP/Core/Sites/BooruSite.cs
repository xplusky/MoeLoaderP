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

        protected BooruSite()
        {
            DownloadTypes.Add("原图", 4);
            DownloadTypes.Add("预览图",2);
            if(SiteType == SiteTypeEnum.Xml) DownloadTypes.Add("Jpeg图",3);
        }

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
                case SiteTypeEnum.Xml: return await GetRealPageImagesAsyncFromXml(para,  token);
                case SiteTypeEnum.Json: return await GetRealPageImagesAsyncFromJson(para,  token);
                default: return null;
            }
        }

        public async Task<ImageItems> GetRealPageImagesAsyncFromXml(SearchPara para, CancellationToken token)
        {
            
            var client = new NetSwap(Settings).Client;
            var query = GetPageQuery(para);
            var xmlres = await client.GetAsync(query, token);
            var xmlstr = await xmlres.Content.ReadAsStreamAsync();
            return await Task.Run(() =>
            {
                var xml = XDocument.Load(xmlstr);
                var imageitems = new ImageItems();
                if (xml.Root == null) return imageitems;
                foreach (var post in xml.Root.Elements())
                {
                    token.ThrowIfCancellationRequested();
                    var img = new ImageItem(this,para);

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
                    img.DetailUrl = GetDetailPageUrl(img);
                    img.Site = this;
                    double.TryParse(post.Attribute("created_at")?.Value, out var creatat);
                    if (creatat > 0) img.CreatTime = new DateTime(1970, 1, 1, 0, 0, 0, 0) + TimeSpan.FromSeconds(creatat);
                    int.TryParse(post.Attribute("score")?.Value, out var score);
                    img.Score = score;
                    ulong.TryParse(post.Attribute("file_size")?.Value,out var filesize);
                        
                    img.Urls.Add(new UrlInfo("缩略图", 1, UrlPre + post.Attribute("preview_url")?.Value, GetThumbnailReferer(img)));
                    img.Urls.Add(new UrlInfo("预览图", 2, UrlPre + post.Attribute("sample_url")?.Value, GetThumbnailReferer(img)));
                    img.Urls.Add(new UrlInfo("Jpeg图", 3, UrlPre + post.Attribute("jpeg_url")?.Value, GetThumbnailReferer(img)));
                    img.Urls.Add(new UrlInfo("原图", 4, UrlPre + post.Attribute("file_url")?.Value, img.DetailUrl)
                    {
                        Md5 =  post.Attribute("md5")?.Value,
                        BiteSize = filesize,
                    });

                    imageitems.Add(img);
                }
                return imageitems;
            }, token);
        }

        public async Task<ImageItems> GetRealPageImagesAsyncFromJson(SearchPara para, CancellationToken token)
        {
            var client = new NetSwap(Settings).Client;
            var query = GetPageQuery(para);
            var jsonRes = await client.GetAsync(query, token);
            var jsonStr = await jsonRes.Content.ReadAsStringAsync();
            
            return await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var imageitems = new ImageItems();
                dynamic list = JsonConvert.DeserializeObject(jsonStr);
                if (list == null) return imageitems;
                foreach (var item in list)
                {
                    token.ThrowIfCancellationRequested();
                    var img = new ImageItem(this, para);

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
                    img.DetailUrl = GetDetailPageUrl(img);
                    img.Urls.Add(new UrlInfo("缩略图", 1, $"{item.preview_file_url}", GetThumbnailReferer(img)));
                    img.Urls.Add(new UrlInfo("预览图", 2, $"{item.large_file_url}", GetThumbnailReferer(img)));
                    img.Urls.Add(new UrlInfo("原图", 4, $"{item.file_url}", img.DetailUrl));
                    //img.Net = Net;
                    imageitems.Add(img);
                }

                return imageitems;
            }, token);
        }

        public virtual string UrlPre => null;

        public virtual string GetDetailPageUrl(ImageItem item) => $"{HomeUrl}/post/show/{item.Id}";

    }
}
