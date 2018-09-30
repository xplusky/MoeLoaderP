using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoeLoader.Core.Sites
{
    /// <summary>
    /// B站画友、摄影 Fixed 20180922
    /// </summary>
    public class BilibiliSite : MoeSite
    {
        public override string HomeUrl => $"https://h.bilibili.com/{Cat}";

        public override string DisplayName => "哔哩哔哩";

        public override string ShortName => "bilibili";

        public string Cat
        {
            get
            {
                switch (SubListIndex)
                {
                    default: return "d";
                    case 1:
                    case 2: return "p";
                }
            }
        }

        public BilibiliSite()
        {
            SubMenu.Add("画友",new MoeSiteSubMenu(
                new MoeSiteSubMenuItem("最新"), 
                new MoeSiteSubMenuItem("最热")));
            SubMenu.Add("摄影(COS)", new MoeSiteSubMenu(
                new MoeSiteSubMenuItem("最新"), 
                new MoeSiteSubMenuItem("最热")));
            SubMenu.Add("摄影(私服)", new MoeSiteSubMenu(
                new MoeSiteSubMenuItem("最新"), 
                new MoeSiteSubMenuItem("最热")));

            SurpportState.IsSupportKeyword = false;
            SurpportState.IsSupportScore = false;
            SurpportState.IsSupportRating = false;
        }

        public override Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            return null;
        }

        public override async Task<ImageItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            string query;
            const string api = "https://api.vc.bilibili.com/link_draw/v2";
            var type = Lv3ListIndex == 0 ? "new" : "hot";
            var count = para.Count > 20 ? 20 : para.Count;
            switch (SubListIndex)
            {
                default:
                    query = $"{api}/Doc/list?category=all&type={type}&page_num={para.PageIndex - 1}&page_size={count}";
                break;
                case 1:
                    query = $"{api}/Photo/list?category=cos&type={type}&page_num={para.PageIndex - 1}&page_size={count}";
                    break;
                case 2:
                    query = $"{api}/Photo/list?category=sifu&type={type}&page_num={para.PageIndex - 1}&page_size={count}";
                    break;
            }
            var net = new NetSwap(Settings);
            var jsonstr = await net.Client.GetStringAsync(query);

            return await Task.Run(() =>
            {
                dynamic listobj = JsonConvert.DeserializeObject(jsonstr);
                var items = new ImageItems();
                if (listobj?.data?.items == null) return items;
                foreach (var item in listobj.data.items)
                {
                    var img = new ImageItem();
                    img.Author = $"{item.user?.name}";
                    img.Id = (int)item.item.doc_id;
                    var i0 = item.item?.pictures[0];
                    if (i0?.img_width != null) img.Width = (int) i0.img_width;
                    if (i0?.img_height != null) img.Height = (int) i0.img_height;
                    if (img.Width > 0 && img.Height > 0)
                    {
                        img.ThumbnailUrl = img.Width > img.Height ? 
                            $"{i0?.img_src}@512w_{(int) (512d * img.Height / img.Width)}h_1e" : 
                            $"{i0?.img_src}@{(int) (512d * img.Width / img.Height)}w_512h_1e";
                    }
                    else
                    {
                        img.ThumbnailUrl = $"{i0?.img_src}@512w_512h_1e";
                    }
                    img.FileUrl = $"{i0?.img_src}";
                    
                    img.DetailUrl = $"https://h.bilibili.com/{img.Id}";
                    img.Title = $"{item.item?.title}";

                    var list =(JArray) item.item.pictures;

                    if (list.Count > 1)
                    {
                        foreach (var pic in item.item.pictures)
                        {
                            var child = new ImageItem
                            {
                                ThumbnailUrl = $"{pic.img_src}@512w_512h_1e",
                                FileUrl = $"{pic.img_src}",
                                Site = this,
                            };
                            if (pic.img_width != null) child.Width = (int)pic.img_width;
                            if (pic.img_height != null) child.Height = (int)pic.img_height;

                            img.ChilldrenItems.Add(child);
                        }
                    }

                    img.Site = this;
                    img.Net = null;
                    img.ThumbnailReferer = HomeUrl;
                    items.Add(img);
                }
                return items;
            }, token);
        }
    }
}
