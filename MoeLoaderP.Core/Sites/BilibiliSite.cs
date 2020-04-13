using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
            var sub = new MoeMenuItems(new MoeMenuItem("最新"), new MoeMenuItem("最热"));
            SubMenu.Add("画友", sub);
            SubMenu.Add("摄影(COS)", sub);
            SubMenu.Add("摄影(私服)", sub);

            SupportState.IsSupportKeyword = false;
            SupportState.IsSupportScore = false;
            SupportState.IsSupportRating = false;
            DownloadTypes.Add("原图", 4);

            MenuFunc.ShowKeyword = false;
        }

        public override Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token) => null;

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            const string api = "https://api.vc.bilibili.com/link_draw/v2";
            var type = para.Lv3MenuIndex == 0 ? "new" : "hot";
            var count = para.Count > 20 ? 20 : para.Count;
            var api2 ="";
            switch (para.SubMenuIndex)
            {
                case 0:
                    api2 = $"{api}/Doc/list";
                    break;
                case 1:
                case 2:
                    api2 = $"{api}/Photo/list";
                    break;
            }
            var net = new NetDocker(Settings);
            var json = await net.GetJsonAsync(api2, token, new Pairs
            {
                {"category", para.SubMenuIndex == 0 ? "all" : (para.SubMenuIndex == 1 ? "cos" : "sifu")},
                {"type", type},
                {"page_num", $"{para.PageIndex - 1}"},
                {"page_size", $"{count}"}
            });

            var items = new MoeItems();
            if (json?.data?.items == null) return items;
            foreach (var jitem in json.data.items)
            {
                var cat = para.SubMenuIndex == 0 ? "/d" : "/p";
                var img = new MoeItem(this, para)
                {
                    Uploader = $"{jitem.user?.name}",
                    Id = $"{jitem.item?.doc_id}".ToInt(),
                };
                img.DetailUrl = $"https://h.bilibili.com/{img.Id}";
                var i0 = jitem.item?.pictures[0];
                img.Width = $"{i0?.img_width}".ToInt();
                img.Height = $"{i0?.img_height}".ToInt();
                img.Date = $"{jitem.item?.upload_time}".ToDateTime();
                img.Urls.Add(new UrlInfo("缩略图", 1, $"{i0?.img_src}@320w_320h.jpg", HomeUrl + cat));
                img.Urls.Add(new UrlInfo("原图", 4, $"{i0?.img_src}"));


                img.Title = $"{jitem.item?.title}";

                var list = (JArray)jitem.item.pictures;

                if (list.Count > 1)
                {
                    foreach (var pic in jitem.item.pictures)
                    {
                        var child = new MoeItem(this, para);
                        child.Urls.Add(new UrlInfo("缩略图", 1, $"{pic.img_src}@512w_512h_1e", HomeUrl + cat));
                        child.Urls.Add(new UrlInfo("原图", 4, $"{pic.img_src}"));
                        child.Width = $"{pic.img_width}".ToInt();
                        child.Height = $"{pic.img_height}".ToInt();
                        img.ChildrenItems.Add(child);
                    }
                }

                img.OriginString = $"{jitem}";
                items.Add(img);
            }
            return items;
        }
    }
}
