using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using MoeLoader.Core.Sites;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoeLoader.Core.Site
{
    class SiteRule34 : MoeSite
    {
        private string filterTag;
        private SiteBooru booru;
        private MoeSession Sweb = new MoeSession();
        private SessionHeadersCollection shc = new SessionHeadersCollection();
        public override string ShortName => "rule34";
        private Rule34srcType srcType => SubListIndex == 0? Rule34srcType.Filter : Rule34srcType.Full;
        public enum Rule34srcType { Filter, Full }

        public override string HomeUrl => "https://rule34.xxx";

        public override string DisplayName => "rule34.xxx";

        public virtual string ToolTip
        {
            get
            {
                if (srcType == Rule34srcType.Filter)
                    return "过滤排除部分欧美风格等作品";
                else
                    return "可获得完整的搜索结果";
            }
        }


        public SiteRule34()
        {
            booru = new SiteBooru(
                HomeUrl,
                HomeUrl + "/index.php?page=dapi&s=post&q=index&pid={0}&limit={1}&tags={2}",
                HomeUrl + "/autocomplete.php?q={0}",
                 DisplayName, ShortName, Referer, true, BooruProcessor.SourceType.XML);
            StringBuilder sb = new StringBuilder();
            sb.Append("-anthro -lisa_simpson -animal -lapis_lazuli_(steven_universe) -lapis_lazuli_(jewel_pet) -abs -yaoi -yamatopawa");
            sb.Append("-starit -horizontal_cloaca -animal_genitalia -princess_zelda -legoman -soul_calibur -soulcalibur -spiderman");
            sb.Append("-spiderman_(series) -gardevoir -dragon_ball -dragon_ball_z -buttplug -labor -zwijgen -my_little_pony -army");
            sb.Append("-nintendo -family_guy -alien -butt_grab -halo_(series) -justmegabenewell -sangheili -mammal -madeline_fenton");
            sb.Append("-onigrift -widowmaker -mastodon -mmjsoh -iontoon -zootopia -torbj?rn -noill -canine -dragon -sonic_(series) -blood");
            sb.Append("-phillipthe2 -sims_4 -dota -bull_(noill) -gats -adventure_time -undertale -xmen -disney -alyssa_bbw -pernalonga -bones");
            sb.Append("-mark -dexter's_laboratory -camp_lazlo -male_only -steven_universe -bara -princess_peach -super_mario_bros. -athorment");
            sb.Append("-male_focus -autofellatio -llortor -super_saiyan -aka6 -resident_evil -street_fighter -avian -dc -haydee -world_of_warcraft");
            sb.Append("-scalie -male_pov -animal_humanoid -kirby_(series) -mcarson -huge_cock -dickgirl -rasmustheowl -velma_dinkley -irispoplar");
            sb.Append("-the_legend_of_zelda -cuphead_(game) -male -mukucookie -exitation -don't_starve -kid -batmetal -barbara_gordon");
            filterTag = sb.ToString();

            SubMenu.Add("Filter");
            SubMenu.Add("All");
        }

        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(keyWord);
            if (srcType == Rule34srcType.Filter)
            {
                sb.Append(" ");
                sb.Append(filterTag);
            }
            return booru.GetPageString(page, count, sb.ToString(), proxy);
        }

        

        public override List<AutoHintItem> GetAutoHintItems(string word, IWebProxy proxy)
        {
            List<AutoHintItem> re = new List<AutoHintItem>();
            try
            {
                string url = string.Format(booru.tagUrl, word);
                shc.Accept = SessionHeadersValue.AcceptAppJson;
                url = Sweb.Get(url, proxy, shc);

                JArray jobj = (JArray)JsonConvert.DeserializeObject(url);
                string tmpname;

                foreach (JObject jo in jobj)
                {
                    tmpname = jo["value"].ToString();
                    if (srcType == Rule34srcType.Filter && !filterTag.Contains(tmpname) || srcType == Rule34srcType.Full)
                    {
                        re.Add(new AutoHintItem()
                        {
                            Word = tmpname,
                            Count = new Regex(@".*\(([^)]*)\)").Match(jo["label"].ToString()).Groups[1].Value
                        });
                    }
                }
            }
            catch { }

            return re.Count > 0 ? re : booru.GetAutoHintItems(word, proxy);
        }


        public override List<ImageItem> GetImages(string pageString, IWebProxy proxy)
        {
            return booru.GetImages(pageString, proxy);
        }
    }
}
