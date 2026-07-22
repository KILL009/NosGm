using System;
using System.Net.Http;
using System.Security.Authentication;

namespace NosGm.Core
{
    public class AnonymousHttpClientFactory
    {
        private const int REQUEST_TIMEOUT_SEC = 90;

        public HttpClient Create(string route)
        {
            var handler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.None
            };

            return new HttpClient(handler)
            {
                BaseAddress = new Uri(route),
                Timeout = TimeSpan.FromSeconds(REQUEST_TIMEOUT_SEC)
            };
        }

        private static AnonymousHttpClientFactory _instance;

        public static AnonymousHttpClientFactory Instance => _instance ??= new AnonymousHttpClientFactory();
    }
}
