using Frostvein.Core;
using Frostvein.Master.Library.Data;
using Frostvein.SCS.Communication.ScsServices.Service;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Frostvein.Master.Server
{
    internal class MSManager
    {
        #region Members

        private static MSManager _instance;

        #endregion

        #region Instantiation

        public MSManager()
        {
            WorldServers = new List<WorldServer>();
            LoginServers = new List<IScsServiceClient>();
            CharactersUnderSaveProcess = new Dictionary<long, DateTime>();
            ConnectedAccounts = new ThreadSafeGenericList<AccountConnection>();
            AuthentificatedClients = new ThreadSafeGenericLockedList<long>();
            ConfigurationObject = new ConfigurationObject
            {

            };
        }

        #endregion

        #region Properties

        public static MSManager Instance => _instance ?? (_instance = new MSManager());

        public ThreadSafeGenericLockedList<long> AuthentificatedClients { get; set; }

        public ConfigurationObject ConfigurationObject { get; set; }

        public ThreadSafeGenericList<AccountConnection> ConnectedAccounts { get; set; }

        public List<IScsServiceClient> LoginServers { get; set; }

        public List<WorldServer> WorldServers { get; set; }

        public Dictionary<long, DateTime> CharactersUnderSaveProcess { get; set; }

        #endregion
    }
}