using NosGm.Configuration;
using NosGm.Core;
using NosGm.Master.Library.Data;
using NosGm.SCS.Communication.ScsServices.Service;
using System;
using System.Collections.Generic;

namespace NosGm.Master.Server
{
    internal class MSManager
    {
        private static MSManager _instance;

        public MSManager()
        {
            WorldServers = new List<WorldServer>();
            LoginServers = new List<IScsServiceClient>();
            CharactersUnderSaveProcess = new Dictionary<long, DateTime>();
            ConnectedAccounts = new ThreadSafeGenericList<AccountConnection>();
            AuthentificatedClients = new ThreadSafeGenericLockedList<long>();
            AuthenticationServiceClients = new ThreadSafeGenericLockedList<long>();
            ConfigurationObject = new ConfigurationObject
            {
                MaxGold = GameConfiguration.MaxGold
            };
        }

        public static MSManager Instance => _instance ?? (_instance = new MSManager());

        /// <summary>
        /// Clients authenticated for the legacy Master communication service.
        /// </summary>
        public ThreadSafeGenericLockedList<long> AuthentificatedClients { get; set; }

        /// <summary>
        /// Clients authenticated specifically with AuthServiceKey. This list is deliberately
        /// separate so a World or Login communication client cannot issue modern login tickets.
        /// </summary>
        public ThreadSafeGenericLockedList<long> AuthenticationServiceClients { get; set; }

        public ConfigurationObject ConfigurationObject { get; set; }

        public ThreadSafeGenericList<AccountConnection> ConnectedAccounts { get; set; }

        public List<IScsServiceClient> LoginServers { get; set; }

        public List<WorldServer> WorldServers { get; set; }

        public Dictionary<long, DateTime> CharactersUnderSaveProcess { get; set; }
    }
}
