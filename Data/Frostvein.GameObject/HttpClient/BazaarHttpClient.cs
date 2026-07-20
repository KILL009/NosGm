using Newtonsoft.Json;
using Frostvein.Configuration;
using Frostvein.Core;
using Frostvein.Data;
using Frostvein.GameObject.Modules.Bazaar.Commands;
using Frostvein.GameObject.Modules.Bazaar.Queries;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;

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

        public BazaarListingCommitResponseDTO CommitListing(CommitBazaarListingCommand command)
        {
            if (command?.Plan == null)
            {
                return Failure(BazaarListingResult.Error, "The World produced an empty bazaar listing plan.");
            }

            // Build a transport-only plan containing exact DTO instances. The live objects in
            // World inherit from ItemInstanceDTO and expose calculated Session/Item properties;
            // serializing those runtime types would create a large or circular HTTP payload.
            var transportCommand = new CommitBazaarListingCommand
            {
                Plan = CreateTransportPlan(command.Plan)
            };
            string payload = JsonConvert.SerializeObject(transportCommand);
            Exception lastException = null;

            for (int attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    var client = _clientFactory.Create(StaticApiData.BAZAAR_COMMIT_LISTING);
                    client.DefaultRequestHeaders.TransferEncodingChunked = false;
                    using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
                    {
                        var response = client.PostAsync(string.Empty, content).Result;
                        string body = response.Content.ReadAsStringAsync().Result;

                        if (response.IsSuccessStatusCode)
                        {
                            BazaarListingCommitResponseDTO result =
                                JsonConvert.DeserializeObject<BazaarListingCommitResponseDTO>(body);
                            if (result != null)
                            {
                                return result;
                            }

                            return Failure(
                                BazaarListingResult.Error,
                                "The NosBazaar service returned an empty or invalid commit response.");
                        }

                        Logger.Log.Error(
                            $"NosBazaar CommitListing returned HTTP {(int)response.StatusCode} " +
                            $"for operation {command.Plan.OperationId}. Body={Limit(body, 512)}");
                    }
                }
                catch (Exception exception)
                {
                    lastException = exception;
                    Logger.Log.Error(
                        $"NosBazaar CommitListing attempt {attempt} failed for operation " +
                        $"{command.Plan.OperationId}.", exception);
                }

                if (attempt < 2)
                {
                    Thread.Sleep(100);
                }
            }

            return Failure(
                BazaarListingResult.Error,
                lastException == null
                    ? "The NosBazaar service rejected the HTTP request."
                    : "The NosBazaar service could not be reached: " + lastException.Message);
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

        private static BazaarListingDTO CreateTransportPlan(BazaarListingDTO source)
        {
            return new BazaarListingDTO
            {
                OperationId = source.OperationId,
                SellerAccountId = source.SellerAccountId,
                SellerCharacterId = source.SellerCharacterId,
                GoldBefore = source.GoldBefore,
                GoldAfter = source.GoldAfter,
                Tax = source.Tax,
                MaximumGold = source.MaximumGold,
                SourceBefore = CreateItemSnapshot(source.SourceBefore),
                SourceAfter = CreateItemSnapshot(source.SourceAfter),
                BazaarItemAfter = CreateItemSnapshot(source.BazaarItemAfter),
                Listing = source.Listing
            };
        }

        private static ItemInstanceDTO CreateItemSnapshot(ItemInstanceDTO source)
        {
            if (source == null)
            {
                return null;
            }

            var snapshot = new ItemInstanceDTO();
            foreach (PropertyInfo property in typeof(ItemInstanceDTO).GetProperties(
                         BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                {
                    property.SetValue(snapshot, property.GetValue(source, null), null);
                }
            }

            return snapshot;
        }

        private static BazaarListingCommitResponseDTO Failure(
            BazaarListingResult result,
            string message)
        {
            return new BazaarListingCommitResponseDTO
            {
                Result = result,
                CacheRefreshed = false,
                Message = message
            };
        }

        private static string Limit(string value, int maximumLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maximumLength)
            {
                return value;
            }

            return value.Substring(0, maximumLength);
        }
    }
}
