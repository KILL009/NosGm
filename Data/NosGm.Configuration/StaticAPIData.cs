using System;

namespace NosGm.Configuration
{
    public class StaticApiData
    {
        private static readonly string BaseAddress =
            $"http://{Environment.GetEnvironmentVariable("CLUSTER_SERVER_ADDRESS") ?? ServerConfiguration.IPAddress}:" +
            $"{Environment.GetEnvironmentVariable("CLUSTER_SERVER_PORT") ?? "8282"}";

        public static string BAZAAR_GENERATE_RCS = $"{BaseAddress}/Bazaar/Rcs";

        public static string BAZAAR_GENERATE_RCB = $"{BaseAddress}/Bazaar/Rcb";

        public static string COUNT_BAZAAR_ITEMS = $"{BaseAddress}/Bazaar/Count";

        public static string BAZAAR_GET_ITEM = $"{BaseAddress}/Bazaar/GetItem/";

        public static string BAZAAR_DELETE_ITEM = $"{BaseAddress}/Bazaar/DeleteItem/";

        public static string BAZAAR_INSERT_OR_UPDATE = $"{BaseAddress}/Bazaar/InsertOrUpdate";

        public static string BAZAAR_COMMIT_LISTING = $"{BaseAddress}/Bazaar/CommitListing";

        public static string GET_BAZAAR_ITEM_STATE = $"{BaseAddress}/Bazaar/GetState/";

        public static string SET_BAZAAR_ITEM_STATE = $"{BaseAddress}/Bazaar/SetState";

        public static string DELETE_BAZAAR_ITEM_STATE = $"{BaseAddress}/Bazaar/DeleteState/";

        public static string PING_BAZAAR = $"{BaseAddress}/Bazaar/Ping";

        public static string SET_CHARACTER_EVENT = $"{BaseAddress}/User/SetEvent";

        public static string GET_CHARACTER_EVENT = $"{BaseAddress}/User/GetEvent/";

        public static string GET_CHARACTER_LIST_EVENTS = $"{BaseAddress}/User/GetEvents";

        public static string DELETE_IB_EVENTS = $"{BaseAddress}/User/DeleteInstantBattleEvents/";

        public static string SET_FC_PERCENT = $"{BaseAddress}/FrozenCrown";

        public static string GET_FC_PERCENT = $"{BaseAddress}/FrozenCrown";
    }
}
