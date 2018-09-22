using MoeLoader.Core.Sites;

namespace MoeLoader.Core
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
            Sites.Add(new Kawaiinyan());
            Sites.Add(new MiniTokyo());
            Sites.Add(new Eshuushuu());
            // old
            Sites.Add(new SiteZeroChan());
            Sites.Add(new SiteMjvArt());
            Sites.Add(new SiteWCosplay());
            Sites.Add(new SitePixiv());
            if (x) Sites.Add(new SiteYuriimg());
        }
    }
}
