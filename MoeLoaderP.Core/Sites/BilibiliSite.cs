using System;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// B站画友、摄影 Fixed 20200315
    /// </summary>
    public class BilibiliSite : MoeSite
    {
        public override string HomeUrl => "https://h.bilibili.com";

        public override string DisplayName => "哔哩哔哩";

        public override string ShortName => "bilibili";

        public BilibiliSite()
        {
            var lv3Cats = new Categories("最新", "最热");
            Lv2Cat = new Categories()
            {
                new Category("画友", lv3Cats),
                new Category("摄影(COS)", lv3Cats),
                new Category("摄影(私服)", lv3Cats)
            };

            DownloadTypes.Add("原图", DownloadTypeEnum.Origin);


            Config.IsSupportRating = false;
            Config.IsSupportKeyword = true;
            Config.IsSupportScore = true;
            LoginPageUrl = "https://passport.bilibili.com/login";

        }
        
        public override bool VerifyCookieAndSave(CookieCollection ccol)
        {
            return ccol.Cast<Cookie>().Any(cookie => cookie.Name.Equals("DedeUserID", StringComparison.OrdinalIgnoreCase));
        }

        //public override async Task<bool> ThumbAsync(MoeItem item, CancellationToken token)
        //{
        //    if (!IsLogin()) return false;
        //    var r = await AccountNet.Client.PostAsync()
        //}
        
        public bool IsLogin()
        {
            return SiteSettings.GetCookieContainer() != null;
        }

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            var imgs = new MoeItems();
            if (para.Keyword.IsEmpty())
            {
                await SearchByNewOrHot(para, token, imgs);
            }
            else
            {
                await SearchByKeyword(para, token, imgs);
            }
            return imgs;
        }

        public async Task SearchByNewOrHot(SearchPara para, CancellationToken token, MoeItems imgs)
        {
            const string api = "https://api.vc.bilibili.com/link_draw/v2";
            var type = para.Lv3MenuIndex == 0 ? "new" : "hot";
            var count = para.Count > 20 ? 20 : para.Count;
            var api2 = "";
            switch (para.Lv2MenuIndex)
            {
                case 0:
                    api2 = $"{api}/Doc/list";
                    break;
                case 1:
                case 2:
                    api2 = $"{api}/Photo/list";
                    break;
            }
            var net = new NetOperator(Settings);
            var json = await net.GetJsonAsync(api2, token, new Pairs
            {
                {"category", para.Lv2MenuIndex == 0 ? "all" : (para.Lv2MenuIndex == 1 ? "cos" : "sifu")},
                {"type", type},
                {"page_num", $"{para.StartPageIndex - 1}"},
                {"page_size", $"{count}"}
            });


            foreach (var item in Ex.GetList(json?.data?.items))
            {
                var cat = para.Lv2MenuIndex == 0 ? "/d" : "/p";
                var img = new MoeItem(this, para)
                {
                    Uploader = $"{item.user?.name}",
                    Id = $"{item.item?.doc_id}".ToInt(),
                };
                img.DetailUrl = $"https://h.bilibili.com/{img.Id}";
                var i0 = item.item?.pictures[0];
                img.Width = $"{i0?.img_width}".ToInt();
                img.Height = $"{i0?.img_height}".ToInt();
                img.Date = $"{item.item?.upload_time}".ToDateTime();
                img.Urls.Add(1, $"{i0?.img_src}@336w_336h_1e_1c.jpg", HomeUrl + cat);
                img.Urls.Add(2, $"{i0?.img_src}@1024w_768h.jpg");
                img.Urls.Add(4, $"{i0?.img_src}");
                img.Title = $"{item.item?.title}";
                var list = item.item?.pictures as JArray;
                if (list?.Count > 1)
                {
                    foreach (var pic in item.item.pictures)
                    {
                        var child = new MoeItem(this, para);
                        child.Urls.Add(1, $"{pic.img_src}@336w_336h_1e_1c.jpg", HomeUrl + cat);
                        child.Urls.Add(2, $"{pic.img_src}@1024w_768h.jpg", HomeUrl + cat);
                        child.Urls.Add(4, $"{pic.img_src}");
                        child.Width = $"{pic.img_width}".ToInt();
                        child.Height = $"{pic.img_height}".ToInt();
                        img.ChildrenItems.Add(child);
                    }
                }
                img.GetDetailTaskFunc = async () => await GetSearchByNewOrHotDetailTask(img, token, para);
                img.OriginString = $"{item}";
                imgs.Add(img);
            }

            var c = $"{json?.data.total_count}".ToInt();
            Ex.ShowMessage($"共搜索到{c}张，已加载至{para.StartPageIndex}页，共{c / para.Count}页", null, Ex.MessagePos.InfoBar);
        }

        public async Task SearchByKeyword(SearchPara para, CancellationToken token, MoeItems imgs)
        {
            const string api = "https://api.bilibili.com/x/web-interface/search/type";
            var newOrHotOrder = para.Lv3MenuIndex == 0? "pubdate" : "stow";
            var drawOrPhotoCatId = para.Lv2MenuIndex == 0 ? "1" : "2";
            var pairs = new Pairs
            {
                {"search_type", "photo"},
                {"page",$"{para.StartPageIndex}" },
                {"order",newOrHotOrder },
                {"keyword",para.Keyword.ToEncodedUrl() },
                {"category_id",drawOrPhotoCatId },
            };
            var net = new NetOperator(Settings);
            var json = await net.GetJsonAsync(api, token, pairs);
            if(json == null) return;
            foreach (var item in Ex.GetList(json.data?.result))
            {
                var img = new MoeItem(this,para);
                img.Urls.Add(1,$"{item.cover}@336w_336h_1e_1c.jpg");
                img.Urls.Add(2, $"{item.cover}@1024w_768h.jpg");
                img.Urls.Add(4, $"{item.cover}");
                img.Id = $"{item.id}".ToInt();
                img.Score = $"{item.like}".ToInt();
                img.Rank = $"{item.rank_offset}".ToInt();
                img.Title = $"{item.title}";
                img.Uploader = $"{item.uname}";
                img.GetDetailTaskFunc = async () => await GetSearchByKeywordDetailTask(img, token, para);
                img.DetailUrl = $"https://h.bilibili.com/{img.Id}";
                img.OriginString = $"{item}";
                imgs.Add(img);
            }

            var c = $"{json.data?.numResults}".ToInt();
            Ex.ShowMessage($"共搜索到{c}张，已加载至{para.StartPageIndex}页，共{c / para.Count}页", null, Ex.MessagePos.InfoBar);
        }

        public async Task GetSearchByKeywordDetailTask(MoeItem img,CancellationToken token,SearchPara para)
        {
            var query = $"https://api.vc.bilibili.com/link_draw/v1/doc/detail?doc_id={img.Id}";
            var json = await new NetOperator(Settings).GetJsonAsync(query,token);
            var item = json.data?.item;
            if (item == null )return;
            if ((item.pictures as JArray)?.Count > 1)
            {
                var i = 0;
                foreach (var pic in Ex.GetList(item.pictures))
                {
                    var child = new MoeItem(this, para);
                    child.Urls.Add(1, $"{pic.img_src}@336w_336h_1e_1c.jpg");
                    child.Urls.Add(2, $"{pic.img_src}@1024w_768h.jpg");
                    child.Urls.Add(4,$"{pic.img_src}");
                    if (i == 0)
                    {
                        img.Width = $"{pic.img_width}".ToInt();
                        img.Height = $"{pic.img_height}".ToInt();
                    }
                    img.ChildrenItems.Add(child);
                    i++;
                }
            }
            else if((item.pictures as JArray)?.Count == 1)
            {
                var pic = json.data?.item?.pictures[0];
                img.Width = $"{pic?.img_width}".ToInt();
                img.Height = $"{pic?.img_height}".ToInt();
                img.Urls.Add(4, $"{pic?.img_src}");
            }

            foreach (var tag in Ex.GetList(item.tags))
            {
                img.Tags.Add($"{tag.name}");
            }

            img.Date = $"{json.data?.item?.upload_time}".ToDateTime();
            if (img.Date == null) img.DateString = $"{item.upload_time}";
        }

        public async Task GetSearchByNewOrHotDetailTask(MoeItem img, CancellationToken token, SearchPara para)
        {
            var query = $"https://api.vc.bilibili.com/link_draw/v1/doc/detail?doc_id={img.Id}";
            var json = await new NetOperator(Settings).GetJsonAsync(query, token);
            var item = json.data?.item;
            if (item == null) return;
            foreach (var tag in Ex.GetList(item.tags))
            {
                img.Tags.Add($"{tag.name}");
            }

            img.Score = $"{item.vote_count}".ToInt();
        }
    }
}
