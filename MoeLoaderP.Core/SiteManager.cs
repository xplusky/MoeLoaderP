using MoeLoaderP.Core.Sites;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 站点管理器
    /// </summary>
    public class SiteManager
    {
        public Settings Settings { get; set; }
        public MoeSites Sites { get; set; } = new MoeSites();

        public SiteManager(Settings settings)
        {
            Settings = settings;
            Sites.Settings = settings;
            SetDefaultSiteList();
        }

        public void SetDefaultSiteList()
        {
            var x = Settings.IsXMode;

            Sites.Add(new BilibiliSite());
            Sites.Add(new PixivSite());
            if (x) Sites.Add(new PixivR18Site());
            Sites.Add(new KonachanSite());
            Sites.Add(new YandeSite());
            if (x) Sites.Add(new SankakuChanSite());
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
            //Sites.Add(new IntellianalysisWebSite());
        }
    }
}
