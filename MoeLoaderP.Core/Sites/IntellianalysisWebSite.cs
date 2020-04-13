using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    public class IntellianalysisWebSite:MoeSite
    {
        public override string HomeUrl => "AnyPicSite";
        public override string DisplayName => "智能解析网页";
        public override string ShortName => "intelli-site";
        public override Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
