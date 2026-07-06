using Frostvein.Configuration;
using Frostvein.Core;
using System;

namespace Frostvein.GameObject.HttpClients
{
    public class KeepAliveClient
    {
        private readonly AnonymousHttpClientFactory _clientFactory = AnonymousHttpClientFactory.Instance;

        private static KeepAliveClient _instance;

        public static KeepAliveClient Instance => _instance ??= new KeepAliveClient();

        public bool IsBazaarOnline()
        {
            try
            {
                var client = _clientFactory.Create(StaticApiData.PING_BAZAAR);

                var response = client.GetAsync(string.Empty).Result;

                return response.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                Logger.Log.Error(null, e);
                return false;
            }
        }
    }
}
