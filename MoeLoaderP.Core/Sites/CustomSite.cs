using System;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    public abstract class CustomSite : MoeSite
    {
        
    }

    public class CustomSiteItem : CustomSite
    {
        public override string HomeUrl => null;
        public override string DisplayName => "自定义站点";
        public override string ShortName => "custom-site";
        public override Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public CustomSite CurrentCustomSite { get; set; }

    }
}
