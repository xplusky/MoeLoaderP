using System;
using System.IO;
using System.Linq;
using MoeLoaderP.Core.Sites;
using Newtonsoft.Json;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 站点管理器
    /// </summary>
    public class SiteManager
    {
        public Settings Settings { get; set; }
        public MoeSites Sites { get; set; } 

        public SiteManager(Settings settings)
        {
            Settings = settings;
            Sites = new MoeSites(settings);
            SetDefaultSiteList();
        }
        

        public void SetDefaultSiteList()
        {
            var x = Settings.IsXMode;

            Sites.Add(new PixivSite());
            if (x) Sites.Add(new PixivR18Site());
            Sites.Add(new BilibiliSite());
            Sites.Add(new KonachanSite());
            Sites.Add(new KonachanNetSite());
            Sites.Add(new YandeSite());
            Sites.Add(new SankakuChanSite());
            if (x) Sites.Add(new SankakuIdolSite());
            Sites.Add(new YuriimgSite());
            Sites.Add(new BehoimiSite());
            Sites.Add(new SafebooruSite());
            Sites.Add(new DonmaiSite());
            if (x) Sites.Add(new LolibooruSite());
            if (x) Sites.Add(new AtfbooruSite());
            if (x) Sites.Add(new Rule34Site());
            Sites.Add(new GelbooruSite());
            Sites.Add(new KawaiinyanSite());
            Sites.Add(new MiniTokyoSite());
            Sites.Add(new EshuuSite());
            Sites.Add(new ZeroChanSite());
            if (x) Sites.Add(new AnimePicsSite());
            Sites.Add(new WCosplaySite());
        }
        
        public async void SetCustomSitesFormJson(string dir)
        {
            var files = dir.GetDirFiles().Where(i => i.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase)).ToArray();
            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file.FullName);
                var set = JsonConvert.DeserializeObject<CustomSiteConfig>(json);
                if (set == null) continue;
                if (set.Config.IsR18Site)
                {
                    if(Settings.IsXMode) Sites.Add(new CustomSite(set));
                }
                else
                {
                    Sites.Add(new CustomSite(set));
                }
            }
        }

        private int ClickTimes { get; set; }
        public bool R18Check()
        {
            if (!Settings.HaveEnteredXMode)
            {
                ClickTimes++;
                if (ClickTimes <= 3) return false;
                if (ClickTimes is > 3 and < 10)
                {
                    Ex.ShowMessage($"还剩 {10 - ClickTimes} 次粉碎！");
                    return false;
                }
                if (ClickTimes >= 10) Settings.HaveEnteredXMode = true;
            }
            Ex.ShowMessage(Settings.IsXMode ? "已关闭 R18 模式" : "已开启 R18 模式");
            Settings.IsXMode = !Settings.IsXMode;
            Sites.Clear();
            SetDefaultSiteList();
            return true;
        }
    }
}
