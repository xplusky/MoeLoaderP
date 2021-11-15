using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    public class DeviantartSite : MoeSite
    {
        public override string HomeUrl => "https://www.deviantart.com";
        public override string DisplayName => "Deviantart";
        public override string ShortName => "deviantart";
        public override Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
        {
            //var api = "https://www.deviantart.com/_napi/da-browse/api/networkbar/rfy/deviations?cursor=MTQwYWI2MjA9NCY1OTBhY2FkMD03MiZkMTc0YjZiYz04ZTFkYjk0ZjMxMGQ3Y2ExY2Q4MWUzZGEwNGNmNWRlZQ";
            return null;
        }
    }
}
