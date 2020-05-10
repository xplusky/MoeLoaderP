using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// pixiv.net fixed 20200424
    /// </summary>
    public class PixivSite : MoeSite
    {
        public override string HomeUrl =>  "https://www.pixiv.net";//"https://img.cheerfun.dev:233";

        public override string DisplayName => "Pixiv";

        public override string ShortName => "pixiv";

        public virtual bool IsR18 => false;

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
            var mangaIlluMenu = new MoeMenuItems(new MoeMenuItem("插画+动图"), new MoeMenuItem("漫画"));
            SubMenu.Add("最新/搜索", mangaIlluMenu); // 0
            SubMenu.Add("作者ID", mangaIlluMenu); // 1

            var rankLv4Menu = new MoeMenuItems(null, "综合", "插画", "漫画", "动图");

            var ranSubMenu = IsR18 ? new MoeMenuItems(rankLv4Menu, "今日", "本周", "最受男性欢迎", "最受女性欢迎")
                : new MoeMenuItems(rankLv4Menu, "今日", "本周", "本月", "新人", "原创", "最受男性欢迎", "最受女性欢迎");

            var rankingMiten = new MoeMenuItem("排行", ranSubMenu)
            {
                Func = { ShowDatePicker = true, ShowKeyword = false }
            };
            SubMenu.Add(rankingMiten); // 2

            SupportState.IsSupportAccount = true;

            DownloadTypes.Add("原图", 4);
            DownloadTypes.Add("中图", 3);
            //DownloadTypes.Add("小图", 2);

            SupportState.IsSupportRating = false;
            SupportState.IsSupportSearchByImageLastId = true;
            FuncSupportState.IsSupportSelectPixivRankNew = true;
            FuncSupportState.IsSupportSearchByAuthorId = true;
            LoginPageUrl = "https://accounts.pixiv.net/login";
        }

        public override CookieContainer GetCookies()
        {
            var cookieStr = CurrentSiteSetting.LoginCookie;
            if (cookieStr.IsEmpty()) return null;
            var cookies = cookieStr.Split(';');
            var cc = new CookieContainer();
            foreach (var cookie in cookies)
            {
                var values = cookie.Trim().Split('^');
                if (values.Length != 3) continue;
                if (values[0].Trim().ToLower() == "www.pixiv.net" || values[0].ToLower() == "pixiv.net")
                {
                    cc.Add(new Cookie(values[1], values[2], "/", values[0]));
                }
            }

            if (cc.Count == 0)
            {
                CurrentSiteSetting.LoginCookie = null;
                return null;
            }
            return cc;
        }

        public override bool VerifyCookie(string cookieStr) => cookieStr.Contains("device_token");

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            if (Net?.HttpClientHandler?.CookieContainer == null)
            {
                Net = new NetDocker(Settings, HomeUrl);
                Net.HttpClientHandler.AllowAutoRedirect = true;
                Net.HttpClientHandler.UseCookies = true;
                var cc = GetCookies();
                if (cc == null)
                {
                    Extend.ShowMessage("需要重新登录Pixiv站点", null, Extend.MessagePos.Window);
                    Net = null;
                    return null;
                }
                Net.HttpClientHandler.CookieContainer = cc;
                Net.SetTimeOut(40);
            }
            else
            {
                Net = Net.CloneWithOldCookie();
            }

            var imgs = new MoeItems();
            switch ((SearchTypeEnum)para.SubMenuIndex)
            {
                case SearchTypeEnum.TagOrNew:
                    await SearchByNewOrTag(imgs, para, token);
                    break;
                case SearchTypeEnum.Author: // 作者 member id  word = "4338012"; // test
                    if (para.Keyword.ToInt() == 0) Extend.ShowMessage("参数错误，必须在关键词中指定画师 id（纯数字）", null, Extend.MessagePos.Window);
                    else await SearchByAuthor(imgs, para.Keyword.Trim(), para, token);
                    break;
                case SearchTypeEnum.Rank:
                    await SearchByRank(imgs, para, token);
                    break;
            }

            token.ThrowIfCancellationRequested();
            return imgs;
        }


        public async Task SearchByNewOrTag(MoeItems imgs, SearchPara para, CancellationToken token)
        {
            string referer, api; Pairs pairs;
            var isIllust = para.Lv3MenuIndex == 0;
            if (para.Keyword.IsEmpty()) // new
            {
                api = $"{HomeUrl}/ajax/illust/new";
                referer = isIllust ? $"{HomeUrl}/new_illust.php" : $"{HomeUrl}/new_illust.php?type=manga";
                pairs = new Pairs
                {
                    {"lastId", para.LastId == 0 ? "" : $"{para.LastId}"},
                    {"limit", $"{para.Count}"},
                    {"type", isIllust ? "illust" : "manga"},
                    {"r18", R18Query}
                };
            }
            else // tag
            {
                api = $"{HomeUrl}/ajax/search/{(isIllust ? "illustrations" : "manga")}/{para.Keyword.ToEncodedUrl()}";
                referer = $"{HomeUrl}tags/{para.Keyword.ToEncodedUrl()}/{(isIllust ? "illustrations" : "manga")}?mode={R18ModeQuery}&s_mode=s_tag";
                pairs = new Pairs
                {
                    {"word", para.Keyword.ToEncodedUrl()},
                    {"order", "date"},
                    {"mode", R18ModeQuery},
                    {"p", $"{para.PageIndex}"},
                    {"s_mode", "s_tag"},
                    {"type", isIllust ? "illust_and_ugoira" : "manga"}
                };
            }

            Net.SetReferer(referer);
            var json = await Net.GetJsonAsync(api, token, pairs);
            var list = para.Keyword.IsEmpty()
                ? (json?.body?.illusts)
                : (isIllust ? (json?.body?.illust?.data) : (json?.body?.manga?.data));

            foreach (var illus in Extend.CheckListNull(list))
            {
                var img = new MoeItem(this, para) { Site = this, Net = Net.CloneWithOldCookie(), Id = $"{illus.illustId}".ToInt() };
                img.Urls.Add(new UrlInfo(1, $"{illus.url}", $"{HomeUrl}/new_illust.php"));
                img.Title = $"{illus.illustTitle}";
                img.Uploader = $"{illus.userName}";
                img.UploaderId = $"{illus.userId}";
                img.Width = $"{illus.width}".ToInt();
                img.Height = $"{illus.height}".ToInt();
                img.DetailUrl = $"{HomeUrl}/artworks/{img.Id}";
                img.ImagesCount = $"{illus.pageCount}".ToInt();
                foreach (var tag in Extend.CheckListNull(illus.tags)) img.Tags.Add($"{tag}");
                img.Date = GetDateFromUrl($"{illus.url}");
                if ($"{illus.illustType}" == "2") img.GetDetailTaskFunc = async () => await GetUgoiraDetailPageTask(img);
                else img.GetDetailTaskFunc = async () => await GetDetailPageTask(img, para);
                img.OriginString = $"{illus}";
                imgs.Add(img);
            }
            if (!para.Keyword.IsEmpty() && json != null)
            {
                var count = $"{json?.body?.illust?.total}".ToInt();
                Extend.ShowMessage($"共搜索到{count}张图片，当前已加载至第{para.PageIndex}页，共{count / 60}页", null, Extend.MessagePos.InfoBar);
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
                Extend.Log(e.Message, e.StackTrace);
                return DateTime.MinValue;
            }
        }

        public async Task SearchByAuthor(MoeItems imgs, string uid, SearchPara para, CancellationToken token)
        {
            var isIorM = para.Lv3MenuIndex == 0;
            var mi = isIorM ? "illustrations" : "manga";
            var mi2 = isIorM ? "illusts" : "manga";
            var mi3 = isIorM ? "插画" : "漫画";
            Net.SetReferer($"{HomeUrl}/users/{uid}/{mi}");
            var allJson = await Net.GetJsonAsync($"{HomeUrl}/ajax/user/{uid}/profile/all", token);
            if ($"{allJson?.error}".ToLower() == "true")
            {
                Extend.ShowMessage($"搜索失败，网站信息：“{$"{allJson?.message}".ToDecodedUrl()}”", null, Extend.MessagePos.Window);
                return;
            }
            var picIds = new List<string>();
            var arts = isIorM ? allJson?.body?.illusts : allJson?.body?.manga;
            foreach (var ill in Extend.CheckListNull(arts)) picIds.Add((ill as JProperty)?.Name);
            var picCurrentPage = picIds.OrderByDescending(i => i.ToInt()).Skip((para.PageIndex - 1) * para.Count).Take(para.Count).ToList();
            if (!picCurrentPage.Any()) return;
            var pairs = new Pairs();
            foreach (var pic in picCurrentPage) pairs.Add("ids[]".ToEncodedUrl(), pic);
            pairs.Add("work_category", mi2);
            pairs.Add("is_first_page", "1");
            var picsJson = await Net.GetJsonAsync($"{HomeUrl}/ajax/user/{uid}/profile/illusts", token, pairs);
            var works = picsJson?.body?.works;
            foreach (var item in Extend.CheckListNull(works))
            {
                dynamic illus = (item as JProperty)?.Value;
                if (illus == null) continue;
                var img = new MoeItem(this, para);
                img.Urls.Add(1, $"{illus.url}", $"{HomeUrl}/users/{uid}/{mi}");
                img.Id = $"{illus.illustId}".ToInt();
                img.Net = Net.CloneWithOldCookie();
                img.Title = $"{illus.illustTitle}";
                img.Uploader = $"{illus.userName}";
                img.UploaderId = $"{illus.userId}";
                img.Width = $"{illus.width}".ToInt();
                img.Height = $"{illus.height}".ToInt();
                img.DetailUrl = $"{HomeUrl}/artworks/{img.Id}";
                img.ImagesCount = $"{illus.pageCount}".ToInt();
                foreach (var tag in Extend.CheckListNull(illus.tags)) img.Tags.Add($"{tag}");
                img.Date = GetDateFromUrl($"{illus.url}");
                if ($"{illus.illustType}" == "2") img.GetDetailTaskFunc = async () => await GetUgoiraDetailPageTask(img);
                else img.GetDetailTaskFunc = async () => await GetDetailPageTask(img, para);
                img.OriginString = $"{item}";
                imgs.Add(img);
            }
            Extend.ShowMessage($"该作者共有{mi3}{picIds.Count}张,当前第{para.Count * (para.PageIndex - 1) + 1}张", null, Extend.MessagePos.InfoBar);
        }

        public async Task SearchByRank(MoeItems imgs, SearchPara para, CancellationToken token)
        {
            var modesR18 = new[] { "daily", "weekly", "male", "female" };
            var modes = new[] { "daily", "weekly", "monthly", "rookie,", "original", "male", "female" };
            var mode = IsR18 ? modesR18[para.Lv3MenuIndex] : modes[para.Lv3MenuIndex];
            if (IsR18) mode += "_r18";
            var contents = new[] { "all", "illust", "manga", "ugoira" };
            var content = contents[para.Lv4MenuIndex];
            var referer = $"{HomeUrl}/ranking.php?mode={mode}&content={content}";
            Net.SetReferer(referer);
            var q = $"{HomeUrl}/ranking.php";
            var pair = new Pairs
            {
                {"mode", mode},
                {"content", content},
                {"date", para.Date == null ? "" : $"{para.Date:yyyyMMdd}"},
                {"p", $"{para.PageIndex}"},
                {"format", "json"}
            };
            var json = await Net.GetJsonAsync(q, token, pair);
            foreach (var illus in Extend.CheckListNull(json?.contents))
            {
                var img = new MoeItem(this, para)
                {
                    Net = Net.CloneWithOldCookie(),
                    Id = $"{illus.illust_id}".ToInt()
                };
                img.Urls.Add(1, $"{illus.url}", referer);
                img.Title = $"{illus.title}";
                img.Uploader = $"{illus.user_name}";
                img.UploaderId = $"{illus.user_id}";
                img.Width = $"{illus.width}".ToInt();
                img.Height = $"{illus.height}".ToInt();
                img.DetailUrl = $"{HomeUrl}/artworks/{img.Id}";
                img.ImagesCount = $"{illus.illust_page_count}".ToInt();
                img.Score = $"{illus.rating_count}".ToInt();
                img.Rank = $"{illus.rank}".ToInt();
                if (img.Rank > 0)
                {
                    var yes = $"{illus.yes_rank}".ToInt();
                    img.Tip = yes == 0 ? "首次登场" : $"之前#{yes}";
                    if (yes == 0) img.TipHighLight = true;
                }
                foreach (var tag in Extend.CheckListNull(illus.tags)) img.Tags.Add($"{tag}");

                img.Date = GetDateFromUrl($"{illus.url}");
                if ($"{illus.illust_type}" == "2") img.GetDetailTaskFunc = async () => await GetUgoiraDetailPageTask(img);
                else img.GetDetailTaskFunc = async () => await GetDetailPageTask(img, para);

                img.OriginString = $"{illus}";
                imgs.Add(img);
            }

            var count = $"{json?.rank_total}".ToInt();
            Extend.ShowMessage($"共{count}张，当前日期：{json?.date}", null, Extend.MessagePos.InfoBar);
        }

        public async Task GetDetailPageTask(MoeItem img, SearchPara para)
        {
            var net = Net.CloneWithOldCookie();
            var json = await net.GetJsonAsync($"{HomeUrl}/ajax/illust/{img.Id}/pages");
            var img1 = json?.body?[0];
            var refer = $"{HomeUrl}/artworks/{img.Id}";
            if (img1 != null)
            {
                //img.Urls.Add(2, $"{img1.urls.small}", refer);
                img.Urls.Add(3, $"{img1.urls.regular}", refer);
                img.Urls.Add(4, $"{img1.urls.original}", refer);
            }
            var list = (JArray)json?.body;
            if (list?.Count > 1)
            {
                foreach (var item in json.body)
                {
                    var imgItem = new MoeItem(this, para);
                    //imgItem.Urls.Add(2, $"{img1?.urls.small}", refer);
                    imgItem.Urls.Add(3, $"{img1?.urls.regular}", refer);
                    imgItem.Urls.Add(4, $"{item?.urls?.original}", refer);
                    img.ChildrenItems.Add(imgItem);
                }
            }
        }

        public async Task GetUgoiraDetailPageTask(MoeItem img)
        {
            if (img.Tip.IsEmpty()) img.Tip = "动图";
            var net = Net.CloneWithOldCookie();
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

        private async Task UgoiraAfterEffects(DownloadItem item, HttpContent content, CancellationToken token)
        {
            // save json
            var path = Path.ChangeExtension(item.LocalFileFullPath, item.CurrentMoeItem.ExtraFile.FileExt);
            if (path != null)
            {
                try
                {
                    File.WriteAllText(path, item.CurrentMoeItem.ExtraFile.Content);
                }
                catch (Exception e)
                {
                    Extend.Log(e);
                }
            }

            // save gif
            dynamic json = JsonConvert.DeserializeObject(item.CurrentMoeItem.ExtraFile.Content);
            var list = json.body.frames;
            var gifpath = Path.ChangeExtension(item.LocalFileFullPath, "gif");
            if(gifpath == null)return;
            var fi = new FileInfo(gifpath);
            using (var stream = await content.ReadAsStreamAsync())
            {
                item.StatusText = "正在转换为GIF..";
                await Task.Run(() =>
                {
                    // ConvertPixivZipToGif
                    var delayList = new List<int>();
                    using (var images = new MagickImageCollection())
                    {
                        foreach (var frame in list)
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
                                var img = new MagickImage(ms);
                                img.AnimationDelay = delayList[i] / 10;
                                images.Add(img);
                                ms.Dispose();
                            }
                        }
                        var set = new QuantizeSettings();
                        set.Colors = 256;
                        images.Quantize(set);
                        images.Optimize();
                        images.Write(fi, MagickFormat.Gif);
                    }
                }, token);
            }
        }

        private NetDocker AutoHintNet { get; set; }
        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            if (AutoHintNet == null)
            {
                AutoHintNet = new NetDocker(Settings, HomeUrl);
                AutoHintNet.SetReferer(HomeUrl);
            }
            var items = new AutoHintItems();
            if (para.SubMenuIndex != 0 && para.SubMenuIndex != 5) return items;
            var url = $"{HomeUrl}/rpc/cps.php?keyword={para.Keyword}";
            var jList = await AutoHintNet.GetJsonAsync(url, token);
            foreach (var obj in Extend.CheckListNull(jList?.candidates)) items.Add($"{obj.tag_name}");
            return items;
        }
    }

    public class PixivR18Site : PixivSite
    {
        public override string DisplayName => "Pixiv[R18]";
        public override bool IsR18 => true;

    }
}
