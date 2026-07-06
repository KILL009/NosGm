using Newtonsoft.Json;
using Frostvein.Configuration;
using Frostvein.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Frostvein.GameObject.HttpClients
{
    public class UserHttpClient
    {
        private readonly AnonymousHttpClientFactory _clientFactory = AnonymousHttpClientFactory.Instance;

        private static UserHttpClient _instance;

        public static UserHttpClient Instance => _instance ??= new UserHttpClient();

        public UserDataModel GetCharacterEventState(long characterId)
        {
            try
            {
                var client = _clientFactory.Create(StaticApiData.GET_CHARACTER_EVENT);

                var response = client.GetAsync(characterId.ToString()).Result;

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<UserDataModel>(response.Content.ReadAsStringAsync().Result);
                }

                return null;
            }
            catch (Exception e)
            {
                Logger.Log.Error(null, e);
                return null;
            }
        }

        public bool DeleteInstantBattleEvents()
        {
            try
            {
                var client = _clientFactory.Create(StaticApiData.DELETE_IB_EVENTS);

                var response = client.GetAsync(string.Empty).Result;

                if (response.IsSuccessStatusCode)
                {
                    return bool.Parse(response.Content.ReadAsStringAsync().Result);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Log.Error(null, e);
                return false;
            }
        }

        public bool SetCharacterEventState(UserDataModel model)
        {
            try
            {
                if (model == null)
                {
                    return false;
                }

                var client = _clientFactory.Create(StaticApiData.SET_CHARACTER_EVENT);
                client.DefaultRequestHeaders.TransferEncodingChunked = false;

                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
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

        public IEnumerable<UserDataModel> GetCharacterListEvents(IEnumerable<long> ids)
        {
            try
            {
                if (!ids.Any())
                {
                    return null;
                }

                var client = _clientFactory.Create(StaticApiData.GET_CHARACTER_LIST_EVENTS);
                client.DefaultRequestHeaders.TransferEncodingChunked = false;

                var content = new StringContent(JsonConvert.SerializeObject(ids), Encoding.UTF8, "application/json");
                var response = client.PostAsync(string.Empty, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<IEnumerable<UserDataModel>>(response.Content.ReadAsStringAsync().Result);
                }

                return null;
            }
            catch (Exception e)
            {
                Logger.Log.Error(null, e);
                return null;
            }
        }
    }
}
