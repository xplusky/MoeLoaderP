using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    public class DeviantartSite : MoeSite
    {
        public override string HomeUrl => "https://www.deviantart.com";
        public override string DisplayName => "Deviantart";
        public override string ShortName => "deviantart";

        public DeviantartSite()
        {

            LoginPageUrl = "https://www.deviantart.com/users/login";
            Config.IsSupportAccount = true;
            DownloadTypes.Add("原图", DownloadTypeEnum.Origin);
            DownloadTypes.Add("大图", DownloadTypeEnum.Large);
            
        }

        public override bool VerifyCookieAndSave(CookieCollection ccol)
        {
            var b = false;
            foreach (Cookie cookie in ccol)
            {
                if (cookie.Name.Equals("auth_secure", StringComparison.OrdinalIgnoreCase))
                {
                    SiteSettings.LoginExpiresTime = cookie.Expires;
                    b = true;
                    continue;
                }
                if (cookie.Name.Equals("auth", StringComparison.OrdinalIgnoreCase))
                {
                    //SiteSettings.SetSetting("auth",cookie.Value);
                    b = true;
                }
            }
            return b;
        }

        public override async Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
        {

            if (!IsUserLogin) Login();
            if (!IsUserLogin) throw new Exception("必须登录才能搜索");
            var page = new SearchedPage();
            var net = GetCloneNet();
            
            var pairs = new Pairs();
            if (!para.PageIndexCursor.IsEmpty())
            {
                pairs.Add("cursor", para.PageIndexCursor);

            }
            dynamic json = await net.GetJsonAsync($"{HomeUrl}/_napi/da-browse/api/networkbar/rfy/deviations", pairs, true, token);
            foreach(var devi in Ex.GetList(json.deviations))
            {
                if (!$"{devi.type}".Equals("image", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var item = new MoeItem(this, para);
                var orgfile = $"{devi.media.baseUri}";
                var thumbToken = "";
                foreach(var t in Ex.GetList(devi.media.token))
                {
                    thumbToken = $"{t}";
                    if (thumbToken.IsEmpty()) continue;
                    break;
                }
                var types = devi.media.types;
                item.DetailUrl = $"{devi.url}";
                var dlbool = false;
                var dl = $"{devi.isDownloadable}";
                if (dl.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    var org = $"{orgfile}?token={thumbToken}";
                    item.Urls.Add(DownloadTypeEnum.Origin, org, HomeUrl);
                    dlbool = true;
                }
                foreach ( var type in Ex.GetList(types))
                {
                    var t = $"{type.t}";
                    if (t.Contains("350"))
                    {
                        //thumbContnent = $"/v1/fit/w_{type.w},h_{type.h},q_70,strp/{type.c}";
                        var thumbContnent = $"/{type.c}".Replace("<prettyName>",$"{devi.media.prettyName}");
                        var url = $"{orgfile}{thumbContnent}?token={thumbToken}";
                        item.Urls.Add(DownloadTypeEnum.Thumbnail, url, HomeUrl);
                        continue;
                    }
                    if (t.Equals("preview", StringComparison.OrdinalIgnoreCase))
                    {
                        var thumbContnent = $"/{type.c}".Replace("<prettyName>", $"{devi.media.prettyName}");
                        var url = $"{orgfile}{thumbContnent}?token={thumbToken}";
                        item.Urls.Add(DownloadTypeEnum.Medium, url, HomeUrl);
                        continue;
                    }
                    if (t.Equals("fullview",StringComparison.OrdinalIgnoreCase))
                    {
                        if ($"{type.c}".IsEmpty())
                        {
                            continue;
                        }
                        var thumbContnent = $"/{type.c}".Replace("<prettyName>", $"{devi.media.prettyName}");
                        var url = $"{orgfile}{thumbContnent}?token={thumbToken}";
                        item.Urls.Add(DownloadTypeEnum.Large, url, HomeUrl);
                        if(!dlbool ) item.Urls.Add(DownloadTypeEnum.Origin, url, HomeUrl);
                        continue;
                    }
                }
                          
                

                item.Id = $"{devi.deviationId}".ToInt();
                
                item.Title = $"{devi.title}";
                item.UploaderId = $"{devi.author.userId}";
                item.Uploader = $"{devi.author.username}";
                item.OriginString = $"{devi}";
                page.Add(item);
            }

            page.NextPageIndexCursor = $"{json.nextCursor}";
            return page;
        }

        public void Login()
        {
            Net = new NetOperator(Settings);
            var cc = SiteSettings.GetCookieContainer();
            if (cc == null) return;
            Net.SetCookie(cc);
            IsUserLogin = true;
        }
    }
}
