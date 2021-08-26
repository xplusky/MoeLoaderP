using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites
{
    public class EHentai:MoeSite
    {
        public override string HomeUrl => "https://e-hentai.org";
        public override string DisplayName => "e-hentai";
        public override string ShortName => "e-hentai";

        public EHentai()
        {

        }
        public override Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
