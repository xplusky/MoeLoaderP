using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MoeLoaderP.Core.Sites;
using Newtonsoft.Json;

namespace MoeLoaderP.Core;

/// <summary>
///     站点管理器
/// </summary>
public class SiteManager : BindingObject
{
    private MoeSite _currentSelectedSite;

    public SiteManager(Settings settings)
    {
        Settings = settings;
        Sites = new MoeSites(settings);
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        RefreshSiteList();
    }

    public Settings Settings { get; set; }
    public MoeSites Sites { get; set; }

    public MoeSite CurrentSelectedSite
    {
        get => _currentSelectedSite;
        set => SetField(ref _currentSelectedSite, value, nameof(CurrentSelectedSite));
    }

    private int ClickTimes { get; set; }


    public void SetDefaultSiteList()
    {
        var x = Settings.IsXMode;

        Sites.Add(new PixivSite());
        if (x) Sites.Add(new PixivR18Site());

        Sites.Add(new KonachanSite());
        Sites.Add(new KonachanNetSite());
        Sites.Add(new YandeSite());
        Sites.Add(new GelbooruSite());
        Sites.Add(new SankakuChanSite());
        if (x) Sites.Add(new SankakuIdolSite());
        Sites.Add(new DanbooruSite());
        Sites.Add(new DeviantartSite());
        
        Sites.Add(new BilibiliSite());
        Sites.Add(new YuriimgSite());
        Sites.Add(new BehoimiSite());
        Sites.Add(new SafebooruSite());
        Sites.Add(new LolibooruSite());
        if (x) Sites.Add(new AtfbooruSite());
        if (x) Sites.Add(new Rule34Site());

        Sites.Add(new KawaiinyanSite());
        Sites.Add(new MiniTokyoSite());
        Sites.Add(new EshuuSite());
        Sites.Add(new ZeroChanSite());
        if (x) Sites.Add(new AnimePicturesSite());
        Sites.Add(new WorldCosplaySite());
    }

    public async void SetCustomSitesFormJson(string dir)
    {
        var files = dir.GetDirFiles().Where(i => i.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file.FullName);
                var set = JsonConvert.DeserializeObject<CustomSiteConfig>(json);
                if (set == null) continue;
                if (set.Config.IsR18Site)
                {
                    if (Settings.IsXMode) Sites.Add(new CustomSite(set));
                }
                else
                {
                    Sites.Add(new CustomSite(set));
                }
            }
            catch (Exception e)
            {
                Ex.Log($"读取{file.Name}失败");
                Ex.Log(e);

                if (Debugger.IsAttached) throw;
            }
        }
    }

    public bool R18Check()
    {
        if (!Settings.HaveEnteredXMode)
        {
            ClickTimes++;
            switch (ClickTimes)
            {
                case <= 3:
                    return false;
                case > 3 and < 10:
                    Ex.ShowMessage($"还剩 {10 - ClickTimes} 次击破！");
                    return false;
                case >= 10:
                    Settings.HaveEnteredXMode = true;
                    break;
            }
        }

        Ex.ShowMessage(Settings.IsXMode ? "已关闭 NSFW 模式" : "已开启 NSFW 模式");
        Settings.IsXMode = !Settings.IsXMode;
        Sites.Clear();
        RefreshSiteList();
        return true;
    }

    private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        //if (e.PropertyName == nameof(Settings.IsCustomSiteMode))
        //{
        //    Sites.Clear();
        //    if (Settings.IsCustomSiteMode) SetCustomSitesFormJson(Settings.CustomSitesDir);
        //    else SetDefaultSiteList();
        //}
    }

    public void RefreshSiteList()
    {
        SetDefaultSiteList();
        SetCustomSitesFormJson(Settings.CustomSitesDir);
    }
}