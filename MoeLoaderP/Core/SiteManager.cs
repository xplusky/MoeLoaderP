using MoeLoader.Core.Site;
using MoeLoader.Core.Sites;

namespace MoeLoader.Core
{
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
            // new
            Sites.Add(new Bilibili());
            Sites.Add(new Konachan());
            Sites.Add(new Yande());
            Sites.Add(new Behoimi());
            Sites.Add(new Safebooru());
            Sites.Add(new Donmai());
            if (x) Sites.Add(new Lolibooru());
            if (x) Sites.Add(new Atfbooru());
            if (x) Sites.Add(new Rule34());
            Sites.Add(new Gelbooru());
            Sites.Add(new Sankaku(x));
            // old
            Sites.Add(new SiteSankaku());
            Sites.Add(new SiteEshuu());
            Sites.Add(new SiteZeroChan());
            Sites.Add(new SiteMjvArt());
            Sites.Add(new SiteWCosplay());
            Sites.Add(new SitePixiv());
            Sites.Add(new SiteMiniTokyo());
            Sites.Add(new SiteYuriimg());
            Sites.Add(new SiteKawaiinyan());
        }
    }
}
