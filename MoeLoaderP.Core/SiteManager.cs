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
            Sites.Add(new Konachan());
            Sites.Add(new Yande());
            Sites.Add(new Behoimi());
            Sites.Add(new Safebooru());
            Sites.Add(new Donmai());
            if (x) Sites.Add(new Lolibooru());
            if (x) Sites.Add(new Atfbooru());
            if (x) Sites.Add(new Rule34());
            Sites.Add(new Gelbooru());
            Sites.Add(new SankakuSite(x));
            Sites.Add(new KawaiinyanSite());
            Sites.Add(new MiniTokyoSite());
            Sites.Add(new EshuuSite());
            if (x) Sites.Add(new YuriimgSite());
            Sites.Add(new ZeroChanSite());
            Sites.Add(new PixivSite());
            if (x) Sites.Add(new AnimePicsSite());
            Sites.Add(new WCosplaySite());
        }
    }
}
