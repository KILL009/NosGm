using Newtonsoft.Json;
using Frostvein.Configuration;
using Frostvein.Core;
using Frostvein.Data;
using Frostvein.GameObject.Modules.Bazaar.Commands;
using Frostvein.GameObject.Modules.Bazaar.Queries;
using System;
using System.Net.Http;
using System.Text;

namespace Frostvein.GameObject.HttpClients
{
    public class BazaarHttpClient
    {
        private readonly AnonymousHttpClientFactory _clientFactory = AnonymousHttpClientFactory.Instance;

        private static BazaarHttpClient _instance;

        public static BazaarHttpClient Instance => _instance ??= new BazaarHttpClient();

        public BazaarItemDTO GetBazaarItem(GetBazaarItemQuery query)
        {
            try
            {
                var client = _clientFactory.Create(StaticApiData.BAZAAR_GET_ITEM);

                var response = client.GetAsync(query.Id.ToString()).Result;

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<BazaarItemDTO>(response.Content.ReadAsStringAsync().Result);
                }

                return null;
            }
            catch (Exception e)
            {
                Logger.Log.Error(null, e);
                return null;
            }
        }

        public long InsertOrUpdateBazaar(InsertOrUpdateBazaarItemCommand item)
        {
            try
            {
                if (item == null)
                {
                    return -1;
                }

                var client = _clientFactory.Create(StaticApiData.BAZAAR_INSERT_OR_UPDATE);
                client.DefaultRequestHeaders.TransferEncodingChunked = false;

                var content = new StringContent(JsonConvert.SerializeObject(item), Encoding.UTF8, "application/json");

                var response = client.PostAsync(string.Empty, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    return long.Parse(response.Content.ReadAsStringAsync().Result);
                }

                return -1;
            }
            catch (Exception e)
            {
                Logger.Log.Error(null, e);
                return -1;
            }
        }

        public bool DeleteBazaarItem(DeleteBazaarItemCommand command)
        {
            try
            {
                var client = _clientFactory.Create(StaticApiData.BAZAAR_DELETE_ITEM);

                var response = client.GetAsync(command.Id.ToString()).Result;

                if (response.IsSuccessStatusCode)
                {
                    return bool.Parse(response.Content.ReadAsStringAsync().Result);
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.Log.Error(null, e);
                return false;
            }
        }

        public string GenerateRcsList(GetRcsListQuery model)
        {
            try
            {
                if (model == null)
                {
                    return string.Empty;
                }

                var client = _clientFactory.Create(StaticApiData.BAZAAR_GENERATE_RCS);
                client.DefaultRequestHeaders.TransferEncodingChunked = false;

                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var response = client.PostAsync(string.Empty, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }

                return string.Empty;
            }
            catch (Exception e)
            {
                Logger.Log.Error(null, e);
                return string.Empty;
            }
        }

        public string GenerateRcbList(GetRcbListQuery model)
        {
            try
            {
                if (model == null)
                {
                    return string.Empty;
                }

                var client = _clientFactory.Create(StaticApiData.BAZAAR_GENERATE_RCB);
                client.DefaultRequestHeaders.TransferEncodingChunked = false;

                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                var response = client.PostAsync(string.Empty, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }

                return string.Empty;
            }
            catch (Exception e)
            {
                Logger.Log.Error(null, e);
                return string.Empty;
            }
        }

        public bool GetItemState(GetStateQuery query)
        {
            try
            {
                var client = _clientFactory.Create(StaticApiData.GET_BAZAAR_ITEM_STATE);

                var result = client.GetAsync(query.Id.ToString()).Result;

                if (result.IsSuccessStatusCode)
                {
                    return bool.Parse(result.Content.ReadAsStringAsync().Result);
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.Log.Error(null, e);
                return false;
            }
        }

        public bool SetItemState(SetStateCommand id)
        {
            try
            {
                var client = _clientFactory.Create(StaticApiData.SET_BAZAAR_ITEM_STATE);
                client.DefaultRequestHeaders.TransferEncodingChunked = false;

                var content = new StringContent(JsonConvert.SerializeObject(id), Encoding.UTF8, "application/json");

                var response = client.PostAsync(string.Empty, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    return bool.Parse(response.Content.ReadAsStringAsync().Result);
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.Log.Error(null, e);
                return false;
            }
        }

        public bool DeleteItemState(DeleteStateCommand command)
        {
            try
            {
                var client = _clientFactory.Create(StaticApiData.DELETE_BAZAAR_ITEM_STATE);

                var response = client.DeleteAsync(command.Id.ToString()).Result;

                if (response.IsSuccessStatusCode)
                {
                    return bool.Parse(response.Content.ReadAsStringAsync().Result);
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.Log.Error(null, e);
                return false;
            }
        }
    }
}
