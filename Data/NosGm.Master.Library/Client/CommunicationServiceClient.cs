using NosGm.Configuration;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Domain;
using NosGm.Master.Library.Data;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.Scs.Communication;
using NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.SCS.Communication.ScsServices.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;

namespace NosGm.Master.Library.Client
{
    public class CommunicationServiceClient : ICommunicationService
    {
        #region Instantiation

        public CommunicationServiceClient()
        {
            string ip = ServerConfiguration.IPAddress;
            int port = Convert.ToInt32(ServerConfiguration.MasterServerPort);
            _commClient = new CommunicationClient();
            _client = ScsServiceClientBuilder.CreateClient<ICommunicationService>(new ScsTcpEndPoint(ip, port),
                _commClient);
            Thread.Sleep(1000);
            while (_client.CommunicationState != CommunicationStates.Connected)
                try
                {
                    _client.Connect();
                }
                catch (Exception)
                {
                    Logger.Error(Language.Instance.GetMessageFromKey("RETRY_CONNECTION"),
                        memberName: nameof(CommunicationServiceClient));
                    Thread.Sleep(1000);
                }
        }

        #endregion

        #region Members

        private static CommunicationServiceClient _instance;

        private readonly IScsServiceClient<ICommunicationService> _client;
        private readonly CommunicationClient _commClient;

        #endregion

        #region Events

        public event EventHandler BazaarRefresh;

        public event EventHandler CharacterConnectedEvent;

        public event EventHandler CharacterDisconnectedEvent;

        public event EventHandler FamilyRefresh;

        public event EventHandler GlobalEvent;

        public event EventHandler MessageSentToCharacter;

        public event EventHandler PenaltyLogRefresh;

        public event EventHandler RelationRefresh;

        public event EventHandler RestartEvent;

        public event EventHandler SessionKickedEvent;

        public event EventHandler ShutdownEvent;

        public event EventHandler StaticBonusRefresh;

        #endregion

        #region Properties

        public static CommunicationServiceClient Instance =>
            _instance ?? (_instance = new CommunicationServiceClient());

        public CommunicationStates CommunicationState => _client.CommunicationState;

        #endregion

        #region Methods
        public List<(int, int)> GetPorts()
        {
            return _client.ServiceProxy.GetPorts();
        }

        public bool Authenticate(string authKey)
        {
            return _client.ServiceProxy.Authenticate(authKey);
        }

        public bool IsCharacterSaving(long characterId)
        {
            return _client.ServiceProxy.IsCharacterSaving(characterId);
        }

        public void AddOrRemoveSavingCharacters(long characterId, bool add)
        {
            _client.ServiceProxy.AddOrRemoveSavingCharacters(characterId, add);
        }

        public void CheckForStuckAccountsAtSaving()
        {
            _client.ServiceProxy.CheckForStuckAccountsAtSaving();
        }

        public void Cleanup()
        {
            _client.ServiceProxy.Cleanup();
        }

        public void CleanupOutdatedSession()
        {
            _client.ServiceProxy.CleanupOutdatedSession();
        }

        public bool ConnectAccount(Guid worldId, long accountId, int sessionId)
        {
            return _client.ServiceProxy.ConnectAccount(worldId, accountId, sessionId);
        }

        public bool ConnectAccountCrossServer(Guid worldId, long accountId, int sessionId)
        {
            return _client.ServiceProxy.ConnectAccountCrossServer(worldId, accountId, sessionId);
        }

        public bool ConnectCharacter(Guid worldId, long characterId)
        {
            return _client.ServiceProxy.ConnectCharacter(worldId, characterId);
        }

        public void DisconnectAccount(long accountId)
        {
            _client.ServiceProxy.DisconnectAccount(accountId);
        }

        public void DisconnectCharacter(Guid worldId, long characterId)
        {
            _client.ServiceProxy.DisconnectCharacter(worldId, characterId);
        }

        public int? GetChannelIdByWorldId(Guid worldId)
        {
            return _client.ServiceProxy.GetChannelIdByWorldId(worldId);
        }

        public int GetChannelStat(long accountid)
        {
            return _client.ServiceProxy.GetChannelStat(accountid);
        }

        public int GetServerStat()
        {
            return _client.ServiceProxy.GetServerStat();
        }

        public long[][] GetOnlineCharacters()
        {
            return _client.ServiceProxy.GetOnlineCharacters();
        }

        public bool IsAccountConnected(long accountId)
        {
            return _client.ServiceProxy.IsAccountConnected(accountId);
        }

        public bool IsAct4Online()
        {
            return _client.ServiceProxy.IsAct4Online();
        }

        public bool IsChannel1Online(string worldGroup) 
        { 
            return _client.ServiceProxy.IsChannel1Online(worldGroup);
        }
        public bool IsChannel2Online(string worldGroup) 
        { 
            return _client.ServiceProxy.IsChannel2Online(worldGroup); 
        }
        public bool IsChannel3Online(string worldGroup)
        {
            return _client.ServiceProxy.IsChannel3Online(worldGroup);
        }
        public bool IsChannel4Online(string worldGroup)
        { 
            return _client.ServiceProxy.IsChannel4Online(worldGroup); 
        }
        public bool IsChannel5Online(string worldGroup) 
        { 
            return _client.ServiceProxy.IsChannel5Online(worldGroup);
        }
        public bool IsChannel6Online(string worldGroup) 
        { 
            return _client.ServiceProxy.IsChannel6Online(worldGroup); 
        }
        public bool IsChannel7Online(string worldGroup)
        { 
            return _client.ServiceProxy.IsChannel7Online(worldGroup); 
        }

        public bool IsCharacterConnected(string worldGroup, long characterId)
        {
            return _client.ServiceProxy.IsCharacterConnected(worldGroup, characterId);
        }

        public bool IsCrossServerLoginPermitted(long accountId, int sessionId)
        {
            return _client.ServiceProxy.IsCrossServerLoginPermitted(accountId, sessionId);
        }

        public bool IsLoginPermitted(long accountId, int sessionId)
        {
            return _client.ServiceProxy.IsLoginPermitted(accountId, sessionId);
        }

        public void KickSession(long? accountId, int? sessionId)
        {
            _client.ServiceProxy.KickSession(accountId, sessionId);
        }

        public void PulseAccount(long accountId)
        {
            _client.ServiceProxy.PulseAccount(accountId);
        }

        public void RefreshPenalty(int penaltyId)
        {
            _client.ServiceProxy.RefreshPenalty(penaltyId);
        }

        public void RegisterAccountLogin(long accountId, int sessionId, string ipAddress)
        {
            _client.ServiceProxy.RegisterAccountLogin(accountId, sessionId, ipAddress);
        }

        public void RegisterCrossServerAccountLogin(long accountId, int sessionId)
        {
            _client.ServiceProxy.RegisterCrossServerAccountLogin(accountId, sessionId);
        }

        public int? RegisterWorldServer(SerializableWorldServer worldServer)
        {
            return _client.ServiceProxy.RegisterWorldServer(worldServer);
        }

        public void Restart(string worldGroup, int time = 5)
        {
            _client.ServiceProxy.Restart(worldGroup, time);
        }

        public long[][] RetrieveOnlineCharacters(long characterId)
        {
            return _client.ServiceProxy.RetrieveOnlineCharacters(characterId);
        }

        public string RetrieveOriginWorld(long accountId)
        {
            return _client.ServiceProxy.RetrieveOriginWorld(accountId);
        }

        public string RetrieveRegisteredWorldServers(string username, byte regionType, int sessionId, bool ignoreUserName, long AccountId)
        {
            string packet = _client.ServiceProxy.RetrieveRegisteredWorldServers(
                username,
                regionType,
                sessionId,
                ignoreUserName,
                AccountId);
            return NormalizeNsTeSTPacketLayout(packet);
        }

        private static string NormalizeNsTeSTPacketLayout(string packet)
        {
            const string header = "NsTeST";
            const string modernPrefix = "NsTeST  ";
            const string legacyPrefix = "NsTeST ";

            if (string.IsNullOrEmpty(packet) ||
                packet.StartsWith(modernPrefix, StringComparison.Ordinal) ||
                !packet.StartsWith(legacyPrefix, StringComparison.Ordinal))
            {
                return packet;
            }

            int regionStart = legacyPrefix.Length;
            int regionEnd = packet.IndexOf(' ', regionStart);
            if (regionEnd <= regionStart) return packet;

            int accountStart = regionEnd + 1;
            int accountEnd = packet.IndexOf(' ', accountStart);
            if (accountEnd <= accountStart) return packet;

            string region = packet.Substring(regionStart, regionEnd - regionStart);
            string account = packet.Substring(accountStart, accountEnd - accountStart);
            string remainder = packet.Substring(accountEnd + 1);
            if (string.IsNullOrWhiteSpace(region) ||
                string.IsNullOrWhiteSpace(account) ||
                string.IsNullOrWhiteSpace(remainder))
            {
                return packet;
            }

            return $"{header}  {region} {account} 2 0 0 0 0 0 0 {remainder}";
        }

        public IEnumerable<string> RetrieveServerStatistics(bool isStart)
        {
            return _client.ServiceProxy.RetrieveServerStatistics(isStart);
        }

        public void RunGlobalEvent(EventType eventType, byte value = 0)
        {
            _client.ServiceProxy.RunGlobalEvent(eventType, value);
        }

        public int? SendMessageToCharacter(SCSCharacterMessage message)
        {
            return _client.ServiceProxy.SendMessageToCharacter(message);
        }

        public void Shutdown(string worldGroup)
        {
            _client.ServiceProxy.Shutdown(worldGroup);
        }

        public void UnregisterWorldServer(Guid worldId)
        {
            if (worldId == null)
            {
                return;
            }
            _client.ServiceProxy.UnregisterWorldServer(worldId);
        }

        public void UpdateBazaar(string worldGroup, long bazaarItemId)
        {
            _client.ServiceProxy.UpdateBazaar(worldGroup, bazaarItemId);
        }

        public void UpdateFamily(string worldGroup, long familyId, bool changeFaction)
        {
            _client.ServiceProxy.UpdateFamily(worldGroup, familyId, changeFaction);
        }

        public void UpdateRelation(string worldGroup, long relationId)
        {
            _client.ServiceProxy.UpdateRelation(worldGroup, relationId);
        }

        internal void OnCharacterConnected(long characterId)
        {
            var characterName = DAOFactory.CharacterDAO.LoadById(characterId)?.Name;
            CharacterConnectedEvent?.Invoke(new Tuple<long, string>(characterId, characterName), null);
        }

        internal void OnCharacterDisconnected(long characterId)
        {
            var characterName = DAOFactory.CharacterDAO.LoadById(characterId)?.Name;
            CharacterDisconnectedEvent?.Invoke(new Tuple<long, string>(characterId, characterName), null);
        }

        internal void OnKickSession(long? accountId, int? sessionId)
        {
            SessionKickedEvent?.Invoke(new Tuple<long?, long?>(accountId, sessionId), null);
        }

        internal void OnRestart(int time = 5)
        {
            RestartEvent?.Invoke(time, null);
        }

        internal void OnRunGlobalEvent(EventType eventType, byte value)
        {
            GlobalEvent?.Invoke(new Tuple<EventType, byte>(eventType, value), null);
        }

        internal void OnSendMessageToCharacter(SCSCharacterMessage message)
        {
            MessageSentToCharacter?.Invoke(message, null);
        }

        internal void OnShutdown()
        {
            ShutdownEvent?.Invoke(null, null);
        }

        internal void OnUpdateBazaar(long bazaarItemId)
        {
            BazaarRefresh?.Invoke(bazaarItemId, null);
        }

        internal void OnUpdateFamily(long familyId, bool changeFaction)
        {
            FamilyRefresh?.Invoke(new Tuple<long, bool>(familyId, changeFaction), null);
        }

        internal void OnUpdatePenaltyLog(int penaltyLogId)
        {
            PenaltyLogRefresh?.Invoke(penaltyLogId, null);
        }

        internal void OnUpdateRelation(long relationId)
        {
            RelationRefresh?.Invoke(relationId, null);
        }

        internal void OnUpdateStaticBonus(long characterId)
        {
            StaticBonusRefresh?.Invoke(characterId, null);
        }

        #endregion
    }
}
