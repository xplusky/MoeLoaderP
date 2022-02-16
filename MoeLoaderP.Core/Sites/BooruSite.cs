using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     Booru 引擎图片站点基类
/// </summary>
public abstract class BooruSite : MoeSite
{
    public enum SiteTypeEnum
    {
        Xml,
        Xml2,
        Json
    }

    protected BooruSite()
    {
        DownloadTypes.Add("原图", DownloadTypeEnum.Origin);
        if (SiteType == SiteTypeEnum.Xml) DownloadTypes.Add("Jpeg图", DownloadTypeEnum.Large);
        DownloadTypes.Add("预览图", DownloadTypeEnum.Medium);

        Config.IsSupportKeyword = true;
        Config.IsSupportRating = true;
        Config.IsSupportResolution = true;
        Config.IsSupportScore = true;
    }

    public virtual SiteTypeEnum SiteType => SiteTypeEnum.Xml;

    public virtual Func<MoeItem, SearchPara, CancellationToken, Task> GetDetailTaskFunc { get; set; }

    public virtual string UrlPre => null;
    public abstract string GetHintQuery(SearchPara para);

    public virtual string GetThumbnailReferer(MoeItem item)
    {
        return HomeUrl;
    }

    public virtual string GetFileReferer(MoeItem item)
    {
        return item.DetailUrl;
    }

    public void Login()
    {
        Net = new NetOperator(Settings, this);
        var cc = SiteSettings.GetCookieContainer();
        if (cc != null) Net.SetCookie(cc);
    }

    public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
    {
        if(Net == null) Login();
        var net = Net.CloneWithCookie();
        var list = new AutoHintItems();
        
        switch (SiteType)
        {
            case SiteTypeEnum.Xml:
                var xml = await net.GetXmlAsync(GetHintQuery(para), token: token);
                if (xml == null) return list;
                var root = xml.SelectSingleNode("tags");
                if (root?.ChildNodes == null) return list;
                foreach (XmlElement child in root.ChildNodes)
                    list.Add(new AutoHintItem
                    {
                        Word = child.GetAttribute("name"),
                        Count = child.GetAttribute("count")
                    });
                return list;
            case SiteTypeEnum.Json:
                var json = await net.GetJsonAsync(GetHintQuery(para), token: token);
                foreach (var item in Ex.GetList(json))
                    list.Add(new AutoHintItem
                    {
                        Word = $"{item.name}",
                        Count = $"{item.post_count}"
                    });
                return list;
        }

        return list;
    }

    public abstract string GetPageQuery(SearchPara para);

    public override async Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
    {
        if (Net == null) Login();
        return SiteType switch
        {
            SiteTypeEnum.Xml => await GetRealPageImagesAsyncFromXml(para, token),
            SiteTypeEnum.Xml2 => await GetRealPageImagesAsyncFromXml2(para, token),
            SiteTypeEnum.Json => await GetRealPageImagesAsyncFromJson(para, token),
            _ => null
        };
    }

    public async Task<SearchedPage> GetRealPageImagesAsyncFromXml(SearchPara para, CancellationToken token)
    {
        var net = Net.CloneWithCookie();
        var query = GetPageQuery(para);
        var xml = await net.GetXDocAsync(query, token: token);
        if (xml?.Root == null) return null;

        var imageItems = new SearchedPage();
        foreach (var post in xml.Root.Elements())
        {
            token.ThrowIfCancellationRequested();
            var img = new MoeItem(this, para);
            var tags = post.Attribute("tags")?.Value ?? "";
            foreach (var tag in tags.Split(' '))
            {
                if (!tag.IsEmpty()) img.Tags.Add(tag.Trim());
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
            img.Urls.Add(DownloadTypeEnum.Thumbnail, $"{UrlPre}{post.Attribute("preview_url")?.Value}",
                GetThumbnailReferer(img));
            img.Urls.Add(DownloadTypeEnum.Medium, $"{UrlPre}{post.Attribute("sample_url")?.Value}",
                GetThumbnailReferer(img));
            img.Urls.Add(DownloadTypeEnum.Large, $"{UrlPre}{post.Attribute("jpeg_url")?.Value}",
                GetThumbnailReferer(img));
            img.Urls.Add(DownloadTypeEnum.Origin, $"{UrlPre}{post.Attribute("file_url")?.Value}", img.DetailUrl,
                filesize: post.Attribute("file_size")?.Value.ToUlong() ?? 0);
            img.OriginString = $"{post}";
            if (GetDetailTaskFunc != null) img.GetDetailTaskFunc = async t => await GetDetailTaskFunc(img, para, t);
            imageItems.Add(img);
        }

        var count = xml.Root.Attribute("count")?.Value.ToInt();
        var offset = xml.Root.Attribute("offset")?.Value.ToInt();
        if (offset != null) imageItems.CurrentPageItemsStartNum = offset + 1;
        imageItems.TotalItemCount = count;
        imageItems.TotalPageCount = count / para.CountLimit;
        Ex.ShowMessage($"共搜索到{count}张图片，当前第{offset + 1}张，第{para.PageIndex}页，共{count / para.CountLimit}页", null,
            Ex.MessagePos.InfoBar);
        return imageItems;
    }

    
    /// <summary>
    /// danbooru
    /// </summary>
    /// <param name="para"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<SearchedPage> GetRealPageImagesAsyncFromXml2(SearchPara para, CancellationToken token)
    {
        var net = Net.CloneWithCookie();
        var query = GetPageQuery(para);
        var xml = await net.GetXDocAsync(query, token: token);
        if (xml?.Root == null) return null;

        var rpage = new SearchedPage();
        foreach (var post in xml.Root.Elements())
        {
            token.ThrowIfCancellationRequested();
            var img = new MoeItem(this, para);
            var tags = post.Attribute("tags")?.Value ?? "";
            foreach (var tag in tags.Split(' '))
                if (!tag.IsEmpty())
                    img.Tags.Add(tag.Trim());

            img.Id = post.GetXIntValue("id");
            img.Width = post.GetXIntValue("width");
            img.Height = post.GetXIntValue("height");
            img.Uploader = post.GetXStringValue("author");
            img.Source = post.GetXStringValue("source");
            img.IsExplicit = post.GetXStringValue("rating") != "safe";
            img.DetailUrl = GetDetailPageUrl(img);
            var dateStr = post.GetXStringValue("created_at");
            img.Date= dateStr.ToDateTime();
            if (img.Date == null) img.DateString = dateStr;
            img.Score = post.GetXIntValue("score");
            img.Urls.Add(DownloadTypeEnum.Thumbnail, $"{UrlPre}{post.GetXStringValue("preview_url")}", GetThumbnailReferer(img));
            img.Urls.Add(DownloadTypeEnum.Medium, $"{UrlPre}{post.GetXStringValue("sample_url")}", GetThumbnailReferer(img));
            img.Urls.Add(DownloadTypeEnum.Large, $"{UrlPre}{post.GetXStringValue("jpeg_url")}", GetThumbnailReferer(img));
            img.Urls.Add(DownloadTypeEnum.Origin, $"{UrlPre}{post.GetXStringValue("file_url")}", img.DetailUrl);
            img.OriginString = $"{post}";
            var tagsStr = post.GetXStringValue("tags");
            foreach (var tagStr in tagsStr.Split(' '))
            {
                img.Tags.Add(tagStr.Trim());
            }

            //img.Width = post.Attribute("width")?.Value.ToInt() ?? 0;
            //img.Height = post.Attribute("height")?.Value.ToInt() ?? 0;
            //img.Uploader = post.Attribute("author")?.Value;
            //img.Source = post.Attribute("source")?.Value;
            //img.IsExplicit = post.Attribute("rating")?.Value.ToLower() != "s";
            //img.DetailUrl = GetDetailPageUrl(img);
            //img.Date = post.Attribute("created_at")?.Value.ToDateTime();
            //if (img.Date == null) img.DateString = post.Attribute("created_at")?.Value;
            //img.Score = post.Attribute("score")?.Value.ToInt() ?? 0;
            //img.Urls.Add(DownloadTypeEnum.Thumbnail, $"{UrlPre}{post.Attribute("preview_url")?.Value}",
            //    GetThumbnailReferer(img));
            //img.Urls.Add(DownloadTypeEnum.Medium, $"{UrlPre}{post.Attribute("sample_url")?.Value}",
            //    GetThumbnailReferer(img));
            //img.Urls.Add(DownloadTypeEnum.Large, $"{UrlPre}{post.Attribute("jpeg_url")?.Value}",
            //    GetThumbnailReferer(img));
            //img.Urls.Add(DownloadTypeEnum.Origin, $"{UrlPre}{post.Attribute("file_url")?.Value}", img.DetailUrl,
            //    filesize: post.Attribute("file_size")?.Value.ToUlong() ?? 0);
            //img.OriginString = $"{post}";

            if (GetDetailTaskFunc != null) img.GetDetailTaskFunc = async t => await GetDetailTaskFunc(img, para, t);
            rpage.Add(img);
        }

        var count = xml.Root.Attribute("count")?.Value.ToInt();
        var offset = xml.Root.Attribute("offset")?.Value.ToInt();
        rpage.TotalItemCount = count;
        rpage.TotalPageCount = count / para.CountLimit;
        rpage.CurrentPageItemsStartNum = offset + 1;
        Ex.ShowMessage($"共搜索到{count}张图片，当前第{offset + 1}张，第{para.PageIndex}页，共{count / para.CountLimit}页", null,
            Ex.MessagePos.InfoBar);
        return rpage;
    }

    public async Task<SearchedPage> GetRealPageImagesAsyncFromJson(SearchPara para, CancellationToken token)
    {
        
        var net = Net.CloneWithCookie();
        var list = await net.GetJsonAsync(GetPageQuery(para), token: token);

        SearchedPage GetImgsFromJson()
        {
            token.ThrowIfCancellationRequested();
            var imageItems = new SearchedPage();
            if (list == null) return imageItems;
            foreach (var item in list)
            {
                token.ThrowIfCancellationRequested();
                var img = new MoeItem(this, para);
                img.Width = $"{item.image_width}".ToInt();
                img.Height = $"{item.image_height}".ToInt();
                img.Id = $"{item.id}".ToInt();
                if (img.Id == 0) continue;
                img.Score = $"{item.score}".ToInt();
                img.Uploader = $"{item.uploader_name}";
                foreach (var tag in $"{item.tag_string}".Split(' ').SkipWhile(string.IsNullOrWhiteSpace))
                    img.Tags.Add(tag.Trim());

                img.IsExplicit = $"{item.rating}" == "e";
                img.DetailUrl = GetDetailPageUrl(img);
                img.Date = $"{item.created_at}".ToDateTime();
                if (img.Date == null) img.DateString = $"{item.created_at}";
                img.Urls.Add(DownloadTypeEnum.Thumbnail, $"{item.preview_file_url}", GetThumbnailReferer(img));
                img.Urls.Add(DownloadTypeEnum.Large, $"{item.large_file_url}", GetThumbnailReferer(img));
                img.Urls.Add(DownloadTypeEnum.Origin, $"{item.file_url}", img.DetailUrl,
                    filesize: $"{item.file_size}".ToUlong());
                img.Copyright = $"{item.tag_string_copyright}";
                img.Character = $"{item.tag_string_character}";
                img.Artist = $"{item.tag_string_artist}";
                img.OriginString = $"{item}";
                if (GetDetailTaskFunc != null) img.GetDetailTaskFunc = async t => await GetDetailTaskFunc(img, para, t);
                imageItems.Add(img);
            }

            return imageItems;
        }

        return await Task.Run(GetImgsFromJson, token);
    }

    public virtual string GetDetailPageUrl(MoeItem item)
    {
        return $"{HomeUrl}/post/show/{item.Id}";
    }
}