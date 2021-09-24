using System.IO;
using Newtonsoft.Json;

namespace MoeLoaderP.Core.Sites
{
    public class CustomSiteFactory
    {
        public CustomSiteConfigList SiteConfigList = new CustomSiteConfigList();
        public void GenTestSites()
        {
            #region caosi

            var caosi = new CustomSiteConfig
            {
                HomeUrl = "https://www.caosi.com",
                DisplayName = "草丝美女社",
                ShortName = "caosi",
                Categories = new CustomCategories
                {
                    new CustomCategory()
                    {
                        FirstPageApi = "https://www.caosi.com/c/xinggan",
                        FollowUpPageApi = "https://www.caosi.com/c/xinggan/{pagenum}",
                        Name = "性感美女"
                    },
                    new CustomCategory()
                    {
                        FirstPageApi = "https://www.caosi.com/c/qingchun",
                        FollowUpPageApi = "https://www.caosi.com/c/qingchun/{pagenum}",
                        Name = "清纯美女"
                    },
                    new CustomCategory()
                    {
                        FirstPageApi = "https://www.caosi.com/c/changtui",
                        FollowUpPageApi = "https://www.caosi.com/c/changtui/{pagenum}",
                        Name = "长腿美女"
                    },
                    new CustomCategory()
                    {
                        FirstPageApi = "https://www.caosi.com/c/siwa",
                        FollowUpPageApi = "https://www.caosi.com/c/siwa/{pagenum}",
                        Name = "丝袜美女"
                    },
                    new CustomCategory()
                    {
                        FirstPageApi = "https://www.caosi.com/c/mingxing",
                        FollowUpPageApi = "https://www.caosi.com/c/mingxing/{pagenum}",
                        Name = "明星美女"
                    },
                },
                PagePara =
                {
                    ImagesList = new CustomXpath(".//div[@id='imgList']/ul/li", CustomXpathMode.Node, mul:true),
                    ImageItemThumbnailUrl = new CustomXpath(".//img", CustomXpathMode.Attribute, "src","https:"),
                    ImageItemTitle = new CustomXpath(".//a", CustomXpathMode.Attribute, "title"),
                    ImageItemDetailUrl = new CustomXpath(".//a", CustomXpathMode.Attribute,"href","https://www.caosi.com"),
                    DetailImagesList = new CustomXpath(".//div[@id='picBody']/p/a/img", CustomXpathMode.Node),
                    DetailImageItemOriginUrl = new CustomXpath(".", CustomXpathMode.Attribute,"src","https:"),
                    DetailCurrentPageIndex = new CustomXpath(".//div[@class='pages']/ul/li[@class='thisclass']/a", CustomXpathMode.InnerText),
                    DetailNextPageIndex = new CustomXpath(".//div[@class='pages']/ul/li[@class='thisclass']/following-sibling::li[1]/a", CustomXpathMode.InnerText),
                    DetailNextPageUrl = new CustomXpath(".//div[@class='pages']/ul/li[@class='thisclass']/following-sibling::li[1]/a", CustomXpathMode.Attribute, "href","https://www.caosi.com"),
                    DetailMaxPageIndex = new CustomXpath(".//div[@class='pages']/ul/li[last()-1]/a", CustomXpathMode.InnerText),
                    DetailImagesCount = new CustomXpath(".//div[@class='pages']/ul/li[last()-1]/a", CustomXpathMode.InnerText),

                }
            };
            caosi.Config.IsSupportKeyword = false;
            SiteConfigList.Add(caosi);

            #endregion

            #region 546z

            var site546ZPara2 = new CustomPagePara
            {
                ImagesList = new CustomXpath("//*[@id='infinite_scroll']/div", CustomXpathMode.Node, mul:true),
                ImageItemThumbnailUrl = new CustomXpath(".//img", CustomXpathMode.Attribute, "data-original"),
                ImageItemTitle = new CustomXpath(".//img", CustomXpathMode.Attribute, "title"),
                ImageItemDateTime = new CustomXpath(".//div[@class='items_likes']", CustomXpathMode.InnerText,
                    regex: @"(\d{2}|\d{4})(?:\-)?([0]{1}\d{1}|[1]{1}[0-2]{1})(?:\-)?([0-2]{1}\d{1}|[3]{1}[0-1]{1})(?:\s)?([0-1]{1}\d{1}|[2]{1}[0-3]{1})(?::)?([0-5]{1}\d{1})(?::)?([0-5]{1}\d{1})"),
                ImageItemDetailUrl = new CustomXpath(".//a", CustomXpathMode.Attribute, "href", "http://www.546z.com"),
                DetailImagesList = new CustomXpath(".//div[@id='big-pic']/p/a/img", CustomXpathMode.Node),
                DetailImageItemOriginUrl = new CustomXpath(".", CustomXpathMode.Attribute, "src"),
                DetailCurrentPageIndex = new CustomXpath(".//div[@class='pages']/ul/li[@class='thisclass']/a", CustomXpathMode.InnerText),
                DetailNextPageIndex = new CustomXpath(".//div[@class='pages']/ul/li[@class='thisclass']/following-sibling::li[1]/a", CustomXpathMode.InnerText),
                DetailNextPageUrl = new CustomXpath(".//div[@class='pages']/ul/li[@class='thisclass']/following-sibling::li[1]/a", CustomXpathMode.Attribute, "href", "http://www.546z.com"),
                DetailMaxPageIndex = new CustomXpath(".//div[@class='pages']/ul/li[1]/a", CustomXpathMode.InnerText),
                DetailImagesCount = new CustomXpath(".//div[@class='pages']/ul/li[1]/a", CustomXpathMode.InnerText),
            };
            
            var site546Z = new CustomSiteConfig
            {
                HomeUrl = "http://www.546z.com",
                DisplayName = "爱套图546Z",
                ShortName = "546z",
                
                Categories = new CustomCategories
                {
                    new CustomCategory
                    {
                        FirstPageApi = "http://www.546z.com/guonei/",
                        FollowUpPageApi = "http://www.546z.com/guonei/index_{pagenum}.html",
                        Name = "国内套图",
                        OverridePagePara =site546ZPara2,
                    },
                    new CustomCategory
                    {
                        FirstPageApi = "http://www.546z.com/rihan/",
                        FollowUpPageApi = "http://www.546z.com/rihan/index_{pagenum}.html",
                        Name = "日韩套图",
                        OverridePagePara =site546ZPara2,
                    },
                    new CustomCategory
                    {
                        FirstPageApi = "http://www.546z.com/gangtai/",
                        FollowUpPageApi = "http://www.546z.com/gangtai/index_{pagenum}.html",
                        Name = "港台套图",
                        OverridePagePara =site546ZPara2,
                    },
                    new CustomCategory
                    {
                        FirstPageApi = "http://www.546z.com/tag/rentiyishu.html",
                        FollowUpPageApi = "http://www.546z.com/tag/rentiyishu_{pagenum-1}.html",
                        Name = "人体艺术"
                    },
                    new CustomCategory
                    {
                        FirstPageApi = "http://www.546z.com/tag/meiru.html",
                        FollowUpPageApi = "http://www.546z.com/tag/meiru_{pagenum-1}.html",
                        Name = "美乳"
                    },
                    new CustomCategory
                    {
                        FirstPageApi = "http://www.546z.com/tag/tuinvlang.html",
                        FollowUpPageApi = "http://www.546z.com/tag/tuinvlang_{pagenum-1}.html",
                        Name = "推女郎"
                    },
                },
                PagePara =
                {
                    ImagesList = new CustomXpath(".//ul[@id='mainbodypul']/li", CustomXpathMode.Node ,mul:true),
                    ImageItemThumbnailUrl = new CustomXpath(".//img", CustomXpathMode.Attribute, "data-original"),
                    ImageItemTitle = new CustomXpath(".//a", CustomXpathMode.Attribute, "title"),
                    ImageItemDetailUrl = new CustomXpath(".//a", CustomXpathMode.Attribute, "href", "http://www.546z.com"),
                    DetailImagesList = new CustomXpath(".//div[@id='big-pic']/p/a/img", CustomXpathMode.Node),
                    DetailImageItemOriginUrl = new CustomXpath(".", CustomXpathMode.Attribute, "src"),
                    DetailCurrentPageIndex = new CustomXpath(".//div[@class='pages']/ul/li[@class='thisclass']/a", CustomXpathMode.InnerText),
                    DetailNextPageIndex = new CustomXpath(".//div[@class='pages']/ul/li[@class='thisclass']/following-sibling::li[1]/a", CustomXpathMode.InnerText),
                    DetailNextPageUrl = new CustomXpath(".//div[@class='pages']/ul/li[@class='thisclass']/following-sibling::li[1]/a", CustomXpathMode.Attribute, "href", "http://www.546z.com"),
                    DetailMaxPageIndex = new CustomXpath(".//div[@class='pages']/ul/li[1]/a", CustomXpathMode.InnerText),
                    DetailImagesCount = new CustomXpath(".//div[@class='pages']/ul/li[1]/a", CustomXpathMode.InnerText),
                }
            };
            SiteConfigList.Add(site546Z);

            #endregion

            #region sex5

            var sex5url = "https://www.sex5ntvk.com";
            var sex5 = new CustomSiteConfig
            {
                HomeUrl = sex5url,
                DisplayName = "SEX5性屋娱乐",
                ShortName = "sex5",
                SiteIconUrl = $"{sex5url}/favicon.ico",
                Config =
                {
                    IsR18Site = true,
                    IsSupportKeyword = false,
                },
                PagePara =
                {
                    ImagesList = new CustomXpath(".//div[@id='tpl-img-content']/li", CustomXpathMode.Node ,mul:true),
                    ImageItemThumbnailUrl = new CustomXpath(".//img", CustomXpathMode.Attribute, "data-original"),
                    ImageItemTitle = new CustomXpath(".//h3", CustomXpathMode.Attribute, "title"),
                    ImageItemDetailUrl = new CustomXpath(".//a", CustomXpathMode.Attribute, "href", sex5url),
                    DetailImagesList = new CustomXpath(".//div[@id='tpl-img-content']//img", CustomXpathMode.Node, mul:true),

                    DetailImageItemOriginUrl = new CustomXpath(".", CustomXpathMode.Attribute, "src"),
                    //DetailCurrentPageIndex = new CustomXpath(".//div[@class='pages']/ul/li[@class='thisclass']/a", CustomXpathMode.InnerText)
                }
            };

            sex5.Categories.AddRange($"{sex5url}/art/{{name}}/", $"{sex5url}/art/{{name}}/index_{{pagenum}}.html",
                "yazhoutupian", "亚洲色图", "oumeitupian", "欧美色图", "meituisiwa", "丝袜美腿",
                "toupaizipai", "自拍街拍", "qingchunweimei", "清纯诱惑", "lingleitupian", "另类图片",
                "katongtietu", "卡通次元", "shunvluanlun", "极品熟女", "danaidongtu", "大奶动图", "baoyudongtu", "鲍鱼动图",
                "nvxiadongtu", "上下动图", "meitundongtu", "美臀动图", "nvqishiGIF", "女骑士动图", "hourudongtu", "后入动图",
                "koujiaodongtu", "口交动图", "shejingdongtu", "射精动图");
            SiteConfigList.Add(sex5);

            #endregion

            #region 天堂图片网ivsky

            var ivskyhome = "https://www.ivsky.com";
            var ivsky = new CustomSiteConfig
            {
                HomeUrl = ivskyhome,
                DisplayName = "天堂图片网",
                ShortName = "ivsky",
                SiteIconUrl = "https://www.ivsky.com/favicon.ico",
                Config =
                {
                    IsSupportKeyword = true,
                },
                Categories = new CustomCategories
                {
                    new CustomCategory()
                    {
                        FirstPageApi = "https://www.ivsky.com/tupian/",
                        FollowUpPageApi = "https://www.ivsky.com/tupian/index_{pagenum}.html",
                        Name = "所有图片"
                    }
                },
                PagePara =
                {
                    ImagesList = new CustomXpath(".//ul[@class='ali']/li", CustomXpathMode.Node ,mul:true),
                    ImageItemThumbnailUrl = new CustomXpath(".//img", CustomXpathMode.Attribute, "src","https:"),
                    ImageItemTitle = new CustomXpath(".//img", CustomXpathMode.Attribute, "alt"),
                    ImageItemDetailUrl = new CustomXpath(".//a", CustomXpathMode.Attribute, "href", ivskyhome),
                    DetailImagesList = new CustomXpath(".//ul[@class='pli']/li", CustomXpathMode.Node,mul:true),
                    DetailImageItemThumbnailUrl = new CustomXpath(".//img", CustomXpathMode.Attribute ,"src","https:"),
                    DetailImageItemDetailUrl = new CustomXpath(".//a", CustomXpathMode.Attribute,"href",$"{ivskyhome}"),
                    DetailCurrentPageIndex = new CustomXpath(".//div[@class='pagelist']/*[@class='page-cur']", CustomXpathMode.InnerText),
                    DetailNextPageIndex = new CustomXpath(".//div[@class='pagelist']/*[@class='page-cur']/following-sibling::a[1]", CustomXpathMode.InnerText),
                    DetailMaxPageIndex = new CustomXpath(".//div[@class='pagelist']/*[@class='page-next']/preceding-sibling::*[1]", CustomXpathMode.InnerText),
                    DetailLv2ImagePreviewUrl = new CustomXpath(".//img[@id='imgis']", CustomXpathMode.Attribute,"src","https:"),
                    //DetailLv2ImageDetailUrl = new CustomXpath(".//div[@id='pic_btn']/a[2]", CustomXpathMode.Attribute,"href",ivskyhome),
                    DetailLv2ImageOriginUrl = new CustomXpath(".//img[@id='imgis']", CustomXpathMode.Attribute, "src","https:",
                        replace:"img-pre.ivsky.com/img/tupian/pre", replaceTo:"img-picdown.ivsky.com/img/tupian/pic"),
                }
            };

            ivsky.Categories.AddRange("https://www.ivsky.com/tupian/{name}/", "https://www.ivsky.com/tupian/{name}/index_{pagenum}.html",
                "ziranfengguang", "自然风光");
            SiteConfigList.Add(ivsky);

            #endregion

            #region 回车桌面

            var enterdeskHome = "https://www.enterdesk.com";

            static CustomPagePara Createenterpara()
            {
                var p = new CustomPagePara()
                {
                    ImagesList = new CustomXpath(".//div[@class='egeli_pic_li']", CustomXpathMode.Node, mul: true),
                    ImageItemThumbnailUrl = new CustomXpath(".//img", CustomXpathMode.Attribute, "src"),
                    ImageItemTitle = new CustomXpath(".//img", CustomXpathMode.Attribute, "title"),
                    ImageItemDetailUrl = new CustomXpath(".//a", CustomXpathMode.Attribute, "href"),
                    DetailImagesList = new CustomXpath(".//div[@class='swiper-wrapper']/div", CustomXpathMode.Node, mul: true,
                        pathR2: ".//img[@class='arc_main_pic_img']"),
                    DetailImageItemThumbnailUrl = new CustomXpath(".//img", CustomXpathMode.Attribute, "src"),
                    DetailImageItemOriginUrl = new CustomXpath(".//img[@class='arc_main_pic_img']", CustomXpathMode.Attribute, "src",
                        replace: "edpic", replaceTo: "edpic_source"),
                    DetailImageItemDetailUrl = new CustomXpath(".//a", CustomXpathMode.Attribute, "href", "https://www.enterdesk.com"),
                    DetailLv2ImagePreviewUrl = new CustomXpath(".//img[@class='arc_main_pic_img']", CustomXpathMode.Attribute, "src"),
                    //DetailLv2ImageDetailUrl = new CustomXpath(".//div[@id='pic_btn']/a[2]", CustomXpathMode.Attribute,"href",ivskyhome),
                    DetailLv2ImageOriginUrl = new CustomXpath(".//img[@class='arc_main_pic_img']", CustomXpathMode.Attribute, "src",
                        replace: "edpic", replaceTo: "edpic_source"),
                };
                return p;
            }

            var mmp = Createenterpara();
            mmp.DetailImageItemDetailUrl.Pre = "https://mm.enterdesk.com";
            var enterdesk = new CustomSiteConfig()
            {
                HomeUrl = enterdeskHome,
                DisplayName = "回车桌面",
                ShortName = "enterdesk",
                SiteIconUrl = "https://www.enterdesk.com/favicon.ico",
                SearchApi = "https://www.enterdesk.com/search/{pagenum}-0-6-0-0-0/{keyword}",
                Categories = new CustomCategories
                {
                    new CustomCategory()
                    {
                        FirstPageApi = "https://mm.enterdesk.com/",
                        FollowUpPageApi = "https://mm.enterdesk.com/{pagenum}.html",
                        Name = "美女图片",
                        OverridePagePara = mmp,
                    },
                    new CustomCategory()
                    {
                        FirstPageApi = "https://www.enterdesk.com/zhuomianbizhi/",
                        FollowUpPageApi = "https://www.enterdesk.com/zhuomianbizhi/{pagenum}.html",
                        Name = "壁纸"
                    },
                },
                PagePara = Createenterpara(),
            };
            SiteConfigList.Add(enterdesk);

            #endregion
        }

        public async void OutputJson(string dirpath)
        {
            foreach (var set in SiteConfigList)
            {
                var jsonSet = new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
                var json = JsonConvert.SerializeObject(set, jsonSet);
                if (!Directory.Exists(dirpath)) Directory.CreateDirectory(dirpath);
                var path = Path.Combine(dirpath, $"{set.DisplayName}.json");
                await File.WriteAllTextAsync(path, json);
            }
        }
    }
}
