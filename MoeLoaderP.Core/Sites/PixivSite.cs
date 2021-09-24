using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// pixiv.net
    /// </summary>
    public class PixivSite : MoeSite
    {
        public override string HomeUrl => "https://www.pixiv.net";//"https://img.cheerfun.dev:233";

        public override string DisplayName => "Pixiv";

        public override string ShortName => "pixiv";

        public bool IsR18 { get; set; }

        public string R18Query => IsR18 ? "true" : "false";
        public string R18ModeQuery => IsR18 ? "r18" : "safe";

        public enum SearchTypeEnum
        {
            TagOrNew = 0,
            Author = 1,
            Rank = 2,
        }

        public PixivSite()
        {
            var mangaIlluLv3Cat = new Categories("插画+动图", "漫画");
            var rankLv4Cat = new Categories( "综合", "插画", "漫画", "动图");
            var rankLv3Cat = IsR18 ? new Categories(rankLv4Cat, "今日", "本周", "最受男性欢迎", "最受女性欢迎")
                : new Categories(rankLv4Cat, "今日", "本周", "本月", "新人", "原创", "最受男性欢迎", "最受女性欢迎");
            Config = new MoeSiteConfig
            {
                IsSupportKeyword = true,
                IsSupportRating = true,
                IsSupportResolution = true,
                IsSupportScore = true,
                IsSupportAccount = true
            };
            Lv2Cat = new Categories
            {
                new Category("最新/搜索", mangaIlluLv3Cat), //0
                new Category("作者ID搜索", mangaIlluLv3Cat), //1
                new Category("排行", rankLv3Cat) //2
                {
                    OverrideConfig = new MoeSiteConfig
                    {
                        IsSupportDatePicker = true,
                        IsSupportResolution = true,
                        IsSupportAccount = true,
                        IsSupportScore = true,
                        IsSupportSearchByImageLastId = true
                    }
                }
            };
            
            DownloadTypes.Add("原图", DownloadTypeEnum.Origin);
            DownloadTypes.Add("大图", DownloadTypeEnum.Large);
            
            LoginPageUrl = "https://accounts.pixiv.net/login";
            Mirrors = new MirrorSiteConfigs()
            {
                new MirrorSiteConfig()
                {
                    Name = "pixiviz",
                    HomeUrl = "https://pixiviz.pwp.app/"
                },
            };
        }

        public override bool VerifyCookieAndSave(CookieCollection ccol)
        {
            var b = false;
            foreach (Cookie cookie in ccol)
            {
                if (!cookie.Name.Equals("device_token", StringComparison.OrdinalIgnoreCase)) continue;
                SiteSettings.LoginExpiresTime = cookie.Expires;
                b = true;
                break;
            }
            return b;
        }

        public bool CheckIsLogin()
        {
            var et = SiteSettings.LoginExpiresTime;
            if (et != null && et < DateTime.Now)
            {
                Ex.ShowMessage("登录密钥过期，需要重新登录Pixiv站点", null, Ex.MessagePos.Window);
                return false;
            }

            if (SiteSettings.GetCookieContainer() != null)
            {
                return true;
            }

            Ex.ShowMessage("需要重新登录Pixiv站点才能开始搜索", null, Ex.MessagePos.Window);
            return false;
        }



        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            var islogin = CheckIsLogin();
            if (!islogin) return null;
            if (Net == null)
            {
                Net = new NetOperator(Settings, HomeUrl);
                Net.SetCookie(SiteSettings.GetCookieContainer());
            }

            var net = Net.CreateNewWithOldCookie();
            net.SetTimeOut(40);

            var imgs = new MoeItems();
            
            if (para.MirrorSite.IsDefault)
            {
                switch ((SearchTypeEnum)para.Lv2MenuIndex)
                {
                    case SearchTypeEnum.TagOrNew:
                        await SearchByNewOrTag(net, imgs, para, token);
                        break;
                    case SearchTypeEnum.Author: // 作者 member id  word = "4338012"; // test
                        if (para.Keyword.ToInt() == 0) Ex.ShowMessage("参数错误，必须在关键词中指定画师 id（纯数字）", null, Ex.MessagePos.Window);
                        else await SearchByAuthor(net, imgs, para.Keyword.Trim(), para, token);
                        break;
                    case SearchTypeEnum.Rank:
                        await SearchByRank(net, imgs, para, token);
                        break;
                }
            }
            else
            {
                await SearchViaMirrorSite(net, imgs, para, token);
            }

            token.ThrowIfCancellationRequested();
            return imgs;
        }

        public async Task SearchViaMirrorSite(NetOperator net, MoeItems imgs, SearchPara para, CancellationToken token)
        {
            var referer = "https://pixiviz.pwp.app";
            var homeurl = referer;
            var replaceTo = "";
            net.SetReferer(referer);
            if (para.Keyword.IsEmpty())
            {
                throw new Exception("镜像站点必须填入关键字");
            }
            var api = $"https://pixiviz-api-us.pwp.link/v1/illust/search?word={para.Keyword.ToEncodedUrl()}&page={para.StartPageIndex}";
            if (para.MirrorSite.Name == "pixiviz")
            {
                replaceTo = "https://pixiv-image-lv.pwp.link";
            }
            var json = await net.GetJsonAsync(api, token);
            dynamic list = null;
            if (para.MirrorSite.Name == "pixiviz")
            {
                list = json?.illusts;
            }

            foreach (var illus in Ex.GetList(list))
            {
                var img = new MoeItem(this, para);
                img.Net = Net.CreateNewWithOldCookie();
                img.Id = $"{illus.id}".ToInt();
                var imgurl = $"{illus.image_urls.medium}".Replace("https://i.pximg.net", replaceTo);
                var imgurlL = $"{illus.image_urls.large}".Replace("https://i.pximg.net", replaceTo);
                img.Urls.Add(new UrlInfo(DownloadTypeEnum.Large, imgurl, referer));
                img.Urls.Add(new UrlInfo(DownloadTypeEnum.Thumbnail, imgurlL, referer));
                img.Title = $"{illus.title}";
                img.Uploader = $"{illus.user.account}";
                img.UploaderId = $"{illus.user.id}";
                img.Width = $"{illus.width}".ToInt();
                img.Height = $"{illus.height}".ToInt();
                img.ChildrenItemsCount = $"{illus.page_count}".ToInt();
                foreach (var tag in Ex.GetList(illus.tags)) img.Tags.Add($"{tag.name}");
                img.Date = $"{illus.create_date}".ToDateTime();
                img.OriginString = $"{illus}";
                img.Score = $"{illus.total_bookmarks}".ToInt();
                if (illus.meta_pages != null)
                {
                    foreach (var item in Ex.GetList(illus.meta_pages))
                    {
                        if (item.image_urls?.original != null)
                        {
                            var child = new MoeItem(this, para);
                            var imgUrlO = $"{item.image_urls?.original}".Replace("https://i.pximg.net", replaceTo);
                            child.Urls.Add(new UrlInfo(DownloadTypeEnum.Origin, imgUrlO, homeurl));
                            img.ChildrenItems.Add(child);
                        }

                    }

                }

                if (illus.meta_single_page?.original_image_url != null)
                {
                    var imgurlO = $"{illus.meta_single_page?.original_image_url}".Replace("https://i.pximg.net", replaceTo);
                    img.Urls.Add(new UrlInfo(DownloadTypeEnum.Origin, imgurlO, homeurl));
                }

                imgs.Add(img);
            }
        }

        public async Task SearchByNewOrTag(NetOperator net,MoeItems imgs, SearchPara para, CancellationToken token)
        {
            string referer, api; Pairs pairs;
            var isIllust = para.Lv3MenuIndex == 0;
            var homeurl = HomeUrl;
            //if (para.MirrorSite.Name == "pixiviz")
            //{
            //    homeurl = "https://pixiviz-api-us.pwp.link/v1";
            //}
            if (para.Keyword.IsEmpty()) // new
            {
                api = $"{homeurl}/ajax/illust/new";
                referer = isIllust ? $"{homeurl}/new_illust.php" : $"{homeurl}/new_illust.php?type=manga";
                pairs = new Pairs
                {
                    {"lastId", para.NextPageMark.IsEmpty() ? "" : $"{para.NextPageMark}"},
                    {"limit", $"{para.Count}"},
                    {"type", isIllust ? "illust" : "manga"},
                    {"r18", R18Query}
                };
            }
            else // tag
            {
                api = $"{homeurl}/ajax/search/{(isIllust ? "illustrations" : "manga")}/{para.Keyword.ToEncodedUrl()}";
                referer = $"{homeurl}tags/{para.Keyword.ToEncodedUrl()}/{(isIllust ? "illustrations" : "manga")}?mode={R18ModeQuery}&s_mode=s_tag";
                pairs = new Pairs
                {
                    {"word", para.Keyword.ToEncodedUrl()},
                    {"order", "date"},
                    {"mode", R18ModeQuery},
                    {"p", $"{para.StartPageIndex}"},
                    {"s_mode", "s_tag"},
                    {"type", isIllust ? "illust_and_ugoira" : "manga"}
                };
            }

            net.SetReferer(referer);
            var json = await net.GetJsonAsync(api, token, pairs);
            var list = para.Keyword.IsEmpty()
                ? (json?.body?.illusts)
                : (isIllust ? (json?.body?.illust?.data) : (json?.body?.manga?.data));

            foreach (var illus in Ex.GetList(list))
            {
                var img = new MoeItem(this, para);
                img.Net = Net.CreateNewWithOldCookie();
                img.Id = $"{illus.id}".ToInt();
                img.Urls.Add(new UrlInfo(DownloadTypeEnum.Thumbnail, $"{illus.url}", $"{homeurl}/new_illust.php"));
                img.Title = $"{illus.title}";
                img.Uploader = $"{illus.userName}";
                img.UploaderId = $"{illus.userId}";
                img.Width = $"{illus.width}".ToInt();
                img.Height = $"{illus.height}".ToInt();
                img.DetailUrl = $"{homeurl}/artworks/{img.Id}";
                img.ChildrenItemsCount = $"{illus.pageCount}".ToInt();
                foreach (var tag in Ex.GetList(illus.tags)) img.Tags.Add($"{tag}");
                img.Date = GetDateFromUrl($"{illus.url}");
                if ($"{illus.illustType}" == "2") img.GetDetailTaskFunc = async () => await GetUgoiraDetailPageTask(img);
                else img.GetDetailTaskFunc = async () => await GetDetailPageTask(img, para);
                img.OriginString = $"{illus}";
                imgs.Add(img);
            }

            var lastid = $"{json?.body?.lastId}";

            if (!lastid.IsEmpty())
            {
                para.NextPageMark = lastid;
            }
            if (!para.Keyword.IsEmpty() && json != null)
            {
                var count = $"{json?.body?.illust?.total}".ToInt();
                Ex.ShowMessage($"共搜索到{count}张图片，当前已加载至第{para.StartPageIndex}页，共{count / 60}页", null, Ex.MessagePos.InfoBar);
            }
        }

        public DateTime GetDateFromUrl(string url)
        {
            try
            {
                var str = url.Remove(0, url.IndexOf("/img/", StringComparison.Ordinal) + 5);
                str = str.Remove(19);
                var ifp = new CultureInfo("zh-CN", true);
                DateTime.TryParseExact(str, "yyyy/MM/dd/HH/mm/ss", ifp, DateTimeStyles.None, out var time);
                return time;
            }
            catch (Exception e)
            {
                Ex.Log(e.Message, e.StackTrace);
                return DateTime.MinValue;
            }
        }

        public async Task SearchByAuthor(NetOperator net, MoeItems imgs, string uid, SearchPara para, CancellationToken token)
        {
            var isIllust = para.Lv3MenuIndex == 0;
            var mi = isIllust ? "illustrations" : "manga";
            var mi2 = isIllust ? "illusts" : "manga";
            var mi3 = isIllust ? "插画" : "漫画";
            net.SetReferer($"{HomeUrl}/users/{uid}/{mi}");
            var allJson = await net.GetJsonAsync($"{HomeUrl}/ajax/user/{uid}/profile/all", token);
            if ($"{allJson?.error}".ToLower() == "true")
            {
                Ex.ShowMessage($"搜索失败，网站信息：“{$"{allJson?.message}".ToDecodedUrl()}”", null, Ex.MessagePos.Window);
                return;
            }
            var picIds = new List<string>();
            var arts = isIllust ? allJson?.body?.illusts : allJson?.body?.manga;
            foreach (var ill in Ex.GetList(arts)) picIds.Add((ill as JProperty)?.Name);
            var picCurrentPage = picIds.OrderByDescending(i => i.ToInt()).Skip((para.StartPageIndex - 1) * para.Count).Take(para.Count).ToList();
            if (!picCurrentPage.Any()) return;
            var pairs = new Pairs();
            foreach (var pic in picCurrentPage) pairs.Add("ids[]".ToEncodedUrl(), pic);
            pairs.Add("work_category", mi2);
            pairs.Add("is_first_page", "1");
            var picsJson = await net.GetJsonAsync($"{HomeUrl}/ajax/user/{uid}/profile/illusts", token, pairs);
            var works = picsJson?.body?.works;
            foreach (var item in Ex.GetList(works))
            {
                dynamic illus = (item as JProperty)?.Value;
                if (illus == null) continue;
                var img = new MoeItem(this, para);
                img.Urls.Add(DownloadTypeEnum.Thumbnail, $"{illus.url}", $"{HomeUrl}/users/{uid}/{mi}");
                img.Id = $"{illus.id}".ToInt();
                img.Net = Net.CreateNewWithOldCookie();
                img.Title = $"{illus.title}";
                img.Uploader = $"{illus.userName}";
                img.UploaderId = $"{illus.userId}";
                img.UploaderHeadUrl = $"{illus.profileImageUrl}";
                img.Width = $"{illus.width}".ToInt();
                img.Height = $"{illus.height}".ToInt();
                img.DetailUrl = $"{HomeUrl}/artworks/{img.Id}";
                img.ChildrenItemsCount = $"{illus.pageCount}".ToInt();
                foreach (var tag in Ex.GetList(illus.tags)) img.Tags.Add($"{tag}");
                img.Date = GetDateFromUrl($"{illus.url}");
                if ($"{illus.illustType}" == "2") img.GetDetailTaskFunc = async () => await GetUgoiraDetailPageTask(img);
                else img.GetDetailTaskFunc = async () => await GetDetailPageTask(img, para);
                img.OriginString = $"{item}";

                imgs.Add(img);
            }
            Ex.ShowMessage($"该作者共有{mi3}{picIds.Count}张,当前第{para.Count * (para.StartPageIndex - 1) + 1}张", null, Ex.MessagePos.InfoBar);
        }

        public async Task SearchByRank(NetOperator net, MoeItems imgs, SearchPara para, CancellationToken token)
        {
            var modesR18 = new[] { "daily", "weekly", "male", "female" };
            var modes = new[] { "daily", "weekly", "monthly", "rookie,", "original", "male", "female" };
            var mode = IsR18 ? modesR18[para.Lv3MenuIndex] : modes[para.Lv3MenuIndex];
            if (IsR18) mode += "_r18";
            var contents = new[] { "all", "illust", "manga", "ugoira" };
            var content = contents[para.Lv4MenuIndex];
            var referer = $"{HomeUrl}/ranking.php?mode={mode}&content={content}";
            net.SetReferer(referer);
            var q = $"{HomeUrl}/ranking.php";
            var pair = new Pairs
            {
                {"mode", mode},
                {"content", content},
                {"date", para.Date == null ? "" : $"{para.Date:yyyyMMdd}"},
                {"p", $"{para.StartPageIndex}"},
                {"format", "json"}
            };
            var json = await net.GetJsonAsync(q, token, pair);
            foreach (var illus in Ex.GetList(json?.contents))
            {
                var img = new MoeItem(this, para)
                {
                    Net = Net.CreateNewWithOldCookie(),
                    Id = $"{illus.illust_id}".ToInt()
                };
                img.Urls.Add(DownloadTypeEnum.Thumbnail, $"{illus.url}", referer);
                img.Title = $"{illus.title}";
                img.Uploader = $"{illus.user_name}";
                img.UploaderId = $"{illus.user_id}";
                img.Width = $"{illus.width}".ToInt();
                img.Height = $"{illus.height}".ToInt();
                img.DetailUrl = $"{HomeUrl}/artworks/{img.Id}";
                img.ChildrenItemsCount = $"{illus.illust_page_count}".ToInt();
                img.Score = $"{illus.rating_count}".ToInt();
                img.Rank = $"{illus.rank}".ToInt();
                if (img.Rank > 0)
                {
                    var yes = $"{illus.yes_rank}".ToInt();
                    img.Tip = yes == 0 ? "首次登场" : $"之前#{yes}";
                    if (yes == 0) img.TipHighLight = true;
                }
                foreach (var tag in Ex.GetList(illus.tags)) img.Tags.Add($"{tag}");

                img.Date = GetDateFromUrl($"{illus.url}");
                if ($"{illus.illust_type}" == "2") img.GetDetailTaskFunc = async () => await GetUgoiraDetailPageTask(img);
                else img.GetDetailTaskFunc = async () => await GetDetailPageTask(img, para);

                img.OriginString = $"{illus}";
                imgs.Add(img);
            }

            var count = $"{json?.rank_total}".ToInt();
            Ex.ShowMessage($"共{count}张，当前日期：{json?.date}", null, Ex.MessagePos.InfoBar);
        }

        public async Task GetDetailPageTask( MoeItem img, SearchPara para)
        {
            var net = Net.CreateNewWithOldCookie();
            var json = await net.GetJsonAsync($"{HomeUrl}/ajax/illust/{img.Id}/pages");
            var img1 = json?.body?[0];
            var refer = $"{HomeUrl}/artworks/{img.Id}";
            if (img1 != null)
            {
                //img.Urls.Add(2, $"{img1.urls.small}", refer);
                img.Urls.Add(DownloadTypeEnum.Large, $"{img1.urls.regular}", refer);
                img.Urls.Add(DownloadTypeEnum.Origin, $"{img1.urls.original}", refer);
            }
            var list = (JArray)json?.body;
            if (list?.Count > 1)
            {
                foreach (var item in json.body)
                {
                    var imgItem = new MoeItem(this, para);
                    //imgItem.Urls.Add(2, $"{img1?.urls.small}", refer);
                    imgItem.Urls.Add(DownloadTypeEnum.Large, $"{img1?.urls.regular}", refer);
                    imgItem.Urls.Add(DownloadTypeEnum.Origin, $"{item?.urls?.original}", refer);
                    img.ChildrenItems.Add(imgItem);
                }
            }
        }

        public async Task GetUgoiraDetailPageTask(MoeItem img)
        {
            if (img.Tip.IsEmpty()) img.Tip = "动图";
            var net = Net.CreateNewWithOldCookie();
            var api = $"{HomeUrl}/ajax/illust/{img.Id}/ugoira_meta";
            var jsonRes = await net.Client.GetAsync(api);
            var jsonStr = await jsonRes.Content.ReadAsStringAsync();
            dynamic json = JsonConvert.DeserializeObject(jsonStr);
            var img1 = json?.body;
            var refer = $"{HomeUrl}/artworks/{img.Id}";
            if (img1 != null)
            {
                //img.Urls.Add(2, $"{img1.src}", refer, UgoiraAfterEffects);
                img.Urls.Add(3, $"{img1.src}", refer, UgoiraAfterEffects);
                img.Urls.Add(4, $"{img1.originalSrc}", refer, UgoiraAfterEffects);
                img.ExtraFile = new TextFileInfo { FileExt = "json", Content = jsonStr };
            }
        }

        private async Task UgoiraAfterEffects(MoeItem item,  CancellationToken token)
        {
            // save json
            var path = Path.ChangeExtension(item.LocalFileFullPath, item.ExtraFile.FileExt);
            if (path != null)
            {
                try
                {
                    await File.WriteAllTextAsync(path, item.ExtraFile.Content, token);
                }
                catch (Exception e)
                {
                    Ex.Log(e);
                }
            }

            // save gif
            dynamic json = JsonConvert.DeserializeObject(item.ExtraFile.Content);
            var list = json?.body.frames;
            var gifPath = Path.ChangeExtension(item.LocalFileFullPath, "gif");
            if (gifPath == null) return;
            var fi = new FileInfo(gifPath);
            await using var stream = new FileStream(item.LocalFileFullPath, FileMode.Open);
            item.StatusText = "正在转换为GIF..";
            await Task.Run(() =>
            {
                // ConvertPixivZipToGif
                var delayList = new List<int>();

                using var images = new MagickImageCollection();
                foreach (var frame in Ex.GetList(list))
                {
                    delayList.Add($"{frame.delay}".ToInt());
                }
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    for (var i = 0; i < zip.Entries.Count; i++)
                    {
                        var ms = new MemoryStream();
                        using (var aStream = zip.Entries[i].Open())
                        {
                            aStream.CopyTo(ms);
                            ms.Position = 0L;
                        }
                        var img = new MagickImage(ms)
                        {
                            AnimationDelay = delayList[i] / 10
                        };
                        images.Add(img);
                        ms.Dispose();
                    }
                }
                var set = new QuantizeSettings
                {
                    Colors = 256
                };
                images.Quantize(set);
                images.Optimize();
                images.Write(fi, MagickFormat.Gif);
            }, token);
        }

        private NetOperator AutoHintNet { get; set; }
        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            if (para.MirrorSite != null)
            {
                return null;
            }
            var islogin = CheckIsLogin();
            if (!islogin) return null;
            if (AutoHintNet == null)
            {
                AutoHintNet = new NetOperator(Settings, HomeUrl);
                AutoHintNet.SetCookie(SiteSettings.GetCookieContainer());
            }

            var net = AutoHintNet.CreateNewWithOldCookie();
            net.SetTimeOut(15);
            net.SetReferer(HomeUrl);

            var items = new AutoHintItems();
            if (para.Lv2MenuIndex != 0 && para.Lv2MenuIndex != 5) return items;
            var url = $"{HomeUrl}/rpc/cps.php?keyword={para.Keyword}";
            var jList = await net.GetJsonAsync(url, token);
            foreach (var obj in Ex.GetList(jList?.candidates)) items.Add($"{obj.tag_name}");
            return items;
        }
    }

    public class PixivR18Site : PixivSite
    {
        public override string DisplayName => "Pixiv[R18]";

        public PixivR18Site()
        {
            IsR18 = true;
        }
    }
}
