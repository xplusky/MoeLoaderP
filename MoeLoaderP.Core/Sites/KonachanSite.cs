using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    public class KonachanSite : MoeSite
    {
        public override string HomeUrl => "https://konachan.com";
        public override string DisplayName => "Konachan";
        public override string ShortName => "konachan";

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            var pairs = new Pairs
                {
                    {"page",$"{para.PageIndex}" },
                    {"limit",$"{para.Count}" },
                    {"tags",para.Keyword.ToEncodedUrl() }
                };
            var query= $"{HomeUrl}/post.json{pairs.ToPairsString()}";
            var net = new NetOperator(Settings);
            var json = await net.GetJsonAsync(query, token);
            var imageItems = new MoeItems();
            foreach (var item in Extend.GetList(json))
            {
                
                var img = new MoeItem(this, para);
                img.Width = $"{item.width}".ToInt();
                img.Height = $"{item.height}".ToInt();
                img.Id = $"{item.id}".ToInt();
                img.Score = $"{item.score}".ToInt();
                img.Uploader = $"{item.author}";
                img.UploaderId = $"{item.creator_id}";
                foreach (var tag in $"{item.tags}".Split(' ').SkipWhile(string.IsNullOrWhiteSpace))
                {
                    img.Tags.Add(tag.Trim());
                }

                img.IsExplicit = $"{item.rating}" == "e";
                img.DetailUrl = $"{HomeUrl}/post/show/{img.Id}";
                img.Date = $"{item.created_at}".ToDateTime();
                if (img.Date == null) img.DateString = $"{item.created_at}";
                img.Urls.Add(1, $"{item.preview_url}");
                img.Urls.Add(2, $"{item.sample_url}");
                img.Urls.Add(4, $"{item.file_url}", img.DetailUrl);
                img.Source = $"{item.source}";
                img.OriginString = $"{item}";
                imageItems.Add(img);
            }

            return imageItems;
        }

        public KonachanSite()
        {
            DownloadTypes.Add("原图", 4);
            DownloadTypes.Add("预览图", 1);

        }

        public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
        {
            var net = new NetOperator(para.Site.Settings);
            var pairs = new Pairs
                {
                    {"limit", "15"},
                    {"order", "count"},
                    {"name", para.Keyword}
                };
            var query =  $"{HomeUrl}/tag.json{pairs.ToPairsString()}";
            var json =await net.GetJsonAsync(query, token);

            var items = new AutoHintItems();

            foreach (var item in Extend.GetList(json))
            {
                var hintItem = new AutoHintItem();
                hintItem.Count = $"{item.count}";
                hintItem.Word = $"{item.name}";
                items.Add(hintItem);
            }

            return items;
        }
        //public override string GetHintQuery(SearchPara para)
        //{
        //    var pairs = new Pairs
        //    {
        //        {"limit", "15"},
        //        {"order", "count"},
        //        {"name", para.Keyword}
        //    };
        //    return $"{HomeUrl}/tag.json{pairs.ToPairsString()}";
        //}

        //public override string GetPageQuery(SearchPara para)
        //{
        //    var pairs = new Pairs
        //    {
        //        {"page",$"{para.PageIndex}" },
        //        {"limit",$"{para.Count}" },
        //        {"tags",para.Keyword.ToEncodedUrl() }
        //    };
        //    return $"{HomeUrl}/post.json{pairs.ToPairsString()}";
        //}
    }
}
