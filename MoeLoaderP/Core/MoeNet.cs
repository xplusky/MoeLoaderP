using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Text;
using System.Threading.Tasks;

namespace MoeLoader.Core
{
    public class MoeNet
    {
        public HttpClientHandler HttpClientHandler { get; set; }
        public ProgressMessageHandler ProgressMessageHandler { get; set; }
        public HttpClient Client { get; set; }

        public MoeNet()
        {
            HttpClientHandler = new HttpClientHandler();
            ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
            Client = new HttpClient(ProgressMessageHandler);
        }

        public MoeNet(Settings settings)
        {
            HttpClientHandler = new HttpClientHandler {Proxy = settings.Proxy};
            ProgressMessageHandler = new ProgressMessageHandler(HttpClientHandler);
            Client = new HttpClient(ProgressMessageHandler);
            Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.92 Safari/537.36");
            Client.Timeout = TimeSpan.FromSeconds(20);
        }
    }
}
