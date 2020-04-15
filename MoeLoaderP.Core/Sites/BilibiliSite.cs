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
        
        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            const string api = "https://api.vc.bilibili.com/link_draw/v2";
            var type = para.Lv3MenuIndex == 0 ? "new" : "hot";
            var count = para.Count > 20 ? 20 : para.Count;
            var api2 = "";
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

            var imgs = new MoeItems();
            foreach (var item in Extend.CheckListNull(json?.data?.items))
            {
                var cat = para.SubMenuIndex == 0 ? "/d" : "/p";
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
                img.Urls.Add(1, $"{i0?.img_src}@320w_320h.jpg", HomeUrl + cat);
                img.Urls.Add(4, $"{i0?.img_src}");
                img.Title = $"{item.item?.title}";
                var list = item.item?.pictures as JArray;
                if (list?.Count > 1)
                {
                    foreach (var pic in item.item.pictures)
                    {
                        var child = new MoeItem(this, para);
                        child.Urls.Add(1, $"{pic.img_src}@512w_512h_1e", HomeUrl + cat);
                        child.Urls.Add(4, $"{pic.img_src}");
                        child.Width = $"{pic.img_width}".ToInt();
                        child.Height = $"{pic.img_height}".ToInt();
                        img.ChildrenItems.Add(child);
                    }
                }
                img.OriginString = $"{item}";
                imgs.Add(img);
            }

            var c = $"{json?.data.total_count}".ToInt();
            Extend.ShowMessage($"共搜索到{c}张，已加载至{para.PageIndex}页，共{c/para.Count}页", null, Extend.MessagePos.InfoBar);
            return imgs;
        }
    }
}
