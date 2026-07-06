using System;
using System.Collections.Generic;
using System.Text;

namespace Frostvein.Configuration
{
    public class StaticApiData
    {
        private static string _baseAddress = $"http://{Environment.GetEnvironmentVariable("CLUSTER_SERVER_ADDRESS") ?? ServerConfiguration.IPAddress }:{Environment.GetEnvironmentVariable("CLUSTER_SERVER_PORT") ?? "8282"}";

        public static string BAZAAR_GENERATE_RCS = $"{_baseAddress}/Bazaar/Rcs";

        public static string BAZAAR_GENERATE_RCB = $"{_baseAddress}/Bazaar/Rcb";

        public static string COUNT_BAZAAR_ITEMS = $"{_baseAddress}/Bazaar/Count";

        public static string BAZAAR_GET_ITEM = $"{_baseAddress}/Bazaar/GetItem/";

        public static string BAZAAR_DELETE_ITEM = $"{_baseAddress}/Bazaar/DeleteItem/";

        public static string BAZAAR_INSERT_OR_UPDATE = $"{_baseAddress}/Bazaar/InsertOrUpdate";

        public static string GET_BAZAAR_ITEM_STATE = $"{_baseAddress}/Bazaar/GetState/";

        public static string SET_BAZAAR_ITEM_STATE = $"{_baseAddress}/Bazaar/SetState";

        public static string DELETE_BAZAAR_ITEM_STATE = $"{_baseAddress}/Bazaar/DeleteState/";

        public static string PING_BAZAAR = $"{_baseAddress}/Bazaar/Ping";

        public static string SET_CHARACTER_EVENT = $"{_baseAddress}/User/SetEvent";

        public static string GET_CHARACTER_EVENT = $"{_baseAddress}/User/GetEvent/";

        public static string GET_CHARACTER_LIST_EVENTS = $"{_baseAddress}/User/GetEvents";

        public static string DELETE_IB_EVENTS = $"{_baseAddress}/User/DeleteInstantBattleEvents/";

        public static string SET_FC_PERCENT = $"{_baseAddress}/FrozenCrown";

        public static string GET_FC_PERCENT = $"{_baseAddress}/FrozenCrown";
    }
}
