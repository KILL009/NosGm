using NosGm.Communication.Client;
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Master.Library.Client
{
    public class CommunicationServiceClient : ICommunicationService
    {
        #region Instantiation

        public CommunicationServiceClient()
        {
            _communicationMode =
                CommunicationTransportModeParser.ParseEnvironment();
            if (_communicationMode == CommunicationTransportMode.Grpc)
            {
                throw new InvalidOperationException(
                    "Communication gRPC cutover is blocked until callback, cross-server, and administrative state slices are migrated together.");
            }

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

            var scsTransport = new ScsClusterCommunicationTransport(
                () => _client.ServiceProxy);
            _communicationTransport = new CommunicationTransportRouter(
                _communicationMode,
                scsTransport,
                null);
            _deferredSessionTeardownQueue =
                new DeferredSessionTeardownQueue(this);
        }

        #endregion

        #region Members

        [ThreadStatic]
        private static DeferredSessionTeardownContext _deferredSessionTeardown;

        private static CommunicationServiceClient _instance;

        private readonly IScsServiceClient<ICommunicationService> _client;
        private readonly CommunicationClient _commClient;
        private readonly CommunicationTransportMode _communicationMode;
        private readonly IClusterCommunicationTransport _communicationTransport;
        private readonly DeferredSessionTeardownQueue _deferredSessionTeardownQueue;

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

        public CommunicationTransportMode TransportMode => _communicationMode;

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

        public IDisposable BeginDeferredSessionTeardown(
            long clientId,
            Guid worldId,
            long characterId,
            long accountId,
            int sessionId,
            bool preserveSessionRegistration)
        {
            if (_deferredSessionTeardown != null)
            {
                throw new InvalidOperationException(
                    "A deferred communication teardown scope is already active on this thread.");
            }

            var context = new DeferredSessionTeardownContext
            {
                Owner = this,
                ClientId = clientId,
                WorldId = worldId,
                CharacterId = characterId,
                AccountId = accountId,
                SessionId = sessionId,
                PreserveSessionRegistration = preserveSessionRegistration
            };
            _deferredSessionTeardown = context;
            return new DeferredSessionTeardownScope(this, context);
        }

        public bool ConnectAccount(Guid worldId, long accountId, int sessionId)
        {
            CommunicationTransportResultCode result = Await(
                _communicationTransport.ConnectAccountAsync(
                    worldId,
                    accountId,
                    sessionId,
                    CancellationToken.None));
            return ToMutationBoolean(result, nameof(ConnectAccount));
        }

        public bool ConnectAccountCrossServer(Guid worldId, long accountId, int sessionId)
        {
            return _client.ServiceProxy.ConnectAccountCrossServer(worldId, accountId, sessionId);
        }

        public bool ConnectCharacter(Guid worldId, long characterId)
        {
            CommunicationTransportResultCode result = Await(
                _communicationTransport.ConnectCharacterAsync(
                    worldId,
                    0,
                    0,
                    characterId,
                    CancellationToken.None));
            return ToMutationBoolean(result, nameof(ConnectCharacter));
        }

        public bool ConnectCharacter(
            Guid worldId,
            long accountId,
            int sessionId,
            long characterId)
        {
            CommunicationTransportResultCode result = Await(
                _communicationTransport.ConnectCharacterAsync(
                    worldId,
                    accountId,
                    sessionId,
                    characterId,
                    CancellationToken.None));
            return ToMutationBoolean(result, nameof(ConnectCharacter));
        }

        public void DisconnectAccount(long accountId, int sessionId = 0, bool preserveSessionRegistration = false)
        {
            if (TryDeferDisconnectAccount(
                    accountId,
                    sessionId,
                    preserveSessionRegistration))
            {
                return;
            }

            RequireSuccess(
                Await(
                    _communicationTransport.DisconnectAccountAsync(
                        accountId,
                        sessionId,
                        preserveSessionRegistration,
                        CancellationToken.None)),
                nameof(DisconnectAccount));
        }

        public void DisconnectCharacter(Guid worldId, long characterId)
        {
            if (TryDeferDisconnectCharacter(worldId, characterId))
            {
                return;
            }

            RequireSuccess(
                Await(
                    _communicationTransport.DisconnectCharacterAsync(
                        worldId,
                        0,
                        0,
                        characterId,
                        CancellationToken.None)),
                nameof(DisconnectCharacter));
        }

        public void DisconnectCharacter(
            Guid worldId,
            long accountId,
            int sessionId,
            long characterId)
        {
            if (TryDeferDisconnectCharacter(worldId, characterId))
            {
                DeferredSessionTeardownContext context = _deferredSessionTeardown;
                if (context != null)
                {
                    if (context.AccountId <= 0)
                    {
                        context.AccountId = accountId;
                    }
                    if (context.SessionId <= 0)
                    {
                        context.SessionId = sessionId;
                    }
                }
                return;
            }

            RequireSuccess(
                Await(
                    _communicationTransport.DisconnectCharacterAsync(
                        worldId,
                        accountId,
                        sessionId,
                        characterId,
                        CancellationToken.None)),
                nameof(DisconnectCharacter));
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
            CommunicationBooleanResult result = Await(
                _communicationTransport.IsAccountConnectedAsync(
                    accountId,
                    CancellationToken.None));
            return RequireBoolean(result, nameof(IsAccountConnected));
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
            CommunicationBooleanResult result = Await(
                _communicationTransport.IsLoginPermittedAsync(
                    accountId,
                    sessionId,
                    CancellationToken.None));
            return RequireBoolean(result, nameof(IsLoginPermitted));
        }

        public bool IsAccountSessionRegistered(long accountId, int sessionId)
        {
            CommunicationBooleanResult result = Await(
                _communicationTransport.IsAccountSessionRegisteredAsync(
                    accountId,
                    sessionId,
                    CancellationToken.None));
            return RequireBoolean(
                result,
                nameof(IsAccountSessionRegistered));
        }

        public void KickSession(long? accountId, int? sessionId)
        {
            _client.ServiceProxy.KickSession(accountId, sessionId);
        }

        public void PulseAccount(long accountId)
        {
            PulseAccount(accountId, 0);
        }

        public void PulseAccount(long accountId, int sessionId)
        {
            RequireSuccess(
                Await(
                    _communicationTransport.PulseAccountAsync(
                        accountId,
                        sessionId,
                        CancellationToken.None)),
                nameof(PulseAccount));
        }

        public void RefreshPenalty(int penaltyId)
        {
            _client.ServiceProxy.RefreshPenalty(penaltyId);
        }

        public void RegisterAccountLogin(long accountId, int sessionId, string ipAddress)
        {
            RequireSuccess(
                Await(
                    _communicationTransport.RegisterAccountLoginAsync(
                        accountId,
                        sessionId,
                        ipAddress,
                        CancellationToken.None)),
                nameof(RegisterAccountLogin));
        }

        public void RegisterCrossServerAccountLogin(long accountId, int sessionId)
        {
            _client.ServiceProxy.RegisterCrossServerAccountLogin(accountId, sessionId);
        }

        public int? RegisterWorldServer(SerializableWorldServer worldServer)
        {
            if (worldServer == null)
            {
                return null;
            }

            CommunicationWorldRegistrationResult result = Await(
                _communicationTransport.RegisterWorldServerAsync(
                    worldServer.Id,
                    worldServer.EndPointIP,
                    worldServer.EndPointPort,
                    worldServer.AccountLimit,
                    worldServer.WorldGroup,
                    CancellationToken.None));
            if (result.Result == CommunicationTransportResultCode.Success)
            {
                return result.ChannelId;
            }
            if (result.Result == CommunicationTransportResultCode.NotFound ||
                result.Result == CommunicationTransportResultCode.Unavailable ||
                result.Result == CommunicationTransportResultCode.CapacityExceeded)
            {
                return null;
            }

            throw CreateTransportException(
                nameof(RegisterWorldServer),
                result.Result);
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

            // Modern clients require the double space after NsTeST and the
            // fixed mode field "2". The remainder already begins with the
            // four character-slot pairs generated by Master. Injecting extra
            // zero fields here shifts the SessionId and breaks channel entry.
            return $"{header}  {region} {account} 2 {remainder}";
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
            if (worldId == Guid.Empty)
            {
                return;
            }

            RequireSuccess(
                Await(
                    _communicationTransport.UnregisterWorldServerAsync(
                        worldId,
                        CancellationToken.None)),
                nameof(UnregisterWorldServer));
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

        private static T Await<T>(Task<T> operation)
        {
            return operation.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static bool RequireBoolean(
            CommunicationBooleanResult result,
            string operation)
        {
            if (result == null)
            {
                throw new InvalidOperationException(
                    operation + " returned no communication result.");
            }

            RequireSuccess(result.Result, operation);
            return result.Value;
        }

        private static bool ToMutationBoolean(
            CommunicationTransportResultCode result,
            string operation)
        {
            if (result == CommunicationTransportResultCode.Success)
            {
                return true;
            }
            if (result == CommunicationTransportResultCode.NotFound ||
                result == CommunicationTransportResultCode.Conflict)
            {
                return false;
            }

            throw CreateTransportException(operation, result);
        }

        private static void RequireSuccess(
            CommunicationTransportResultCode result,
            string operation)
        {
            if (result != CommunicationTransportResultCode.Success)
            {
                throw CreateTransportException(operation, result);
            }
        }

        private static InvalidOperationException CreateTransportException(
            string operation,
            CommunicationTransportResultCode result)
        {
            return new InvalidOperationException(
                "Communication operation " + operation +
                " failed with " + result + ".");
        }

        private bool TryDeferDisconnectCharacter(Guid worldId, long characterId)
        {
            DeferredSessionTeardownContext context = _deferredSessionTeardown;
            if (context == null || !ReferenceEquals(context.Owner, this))
            {
                return false;
            }

            if (context.WorldId == Guid.Empty)
            {
                context.WorldId = worldId;
            }
            if (context.CharacterId <= 0)
            {
                context.CharacterId = characterId;
            }
            return true;
        }

        private bool TryDeferDisconnectAccount(
            long accountId,
            int sessionId,
            bool preserveSessionRegistration)
        {
            DeferredSessionTeardownContext context = _deferredSessionTeardown;
            if (context == null || !ReferenceEquals(context.Owner, this))
            {
                return false;
            }

            if (context.AccountId <= 0)
            {
                context.AccountId = accountId;
            }
            if (context.SessionId <= 0)
            {
                context.SessionId = sessionId;
            }
            context.PreserveSessionRegistration = preserveSessionRegistration;
            return true;
        }

        private void CompleteDeferredSessionTeardown(
            DeferredSessionTeardownContext context)
        {
            if (!ReferenceEquals(_deferredSessionTeardown, context))
            {
                return;
            }

            _deferredSessionTeardown = null;
            if (context.CharacterId <= 0 && context.AccountId <= 0)
            {
                return;
            }

            if (!_deferredSessionTeardownQueue.TryEnqueue(
                    new DeferredSessionTeardownWorkItem
                    {
                        ClientId = context.ClientId,
                        WorldId = context.WorldId,
                        CharacterId = context.CharacterId,
                        AccountId = context.AccountId,
                        SessionId = context.SessionId,
                        PreserveSessionRegistration =
                            context.PreserveSessionRegistration
                    }))
            {
                Logger.Error(
                    "[WORLD_REMOTE_TEARDOWN_QUEUE_FULL] ClientId=" +
                    context.ClientId +
                    " AccountId=" + context.AccountId +
                    " CharacterId=" + context.CharacterId);
            }
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

        private sealed class DeferredSessionTeardownContext
        {
            public CommunicationServiceClient Owner { get; set; }

            public long ClientId { get; set; }

            public Guid WorldId { get; set; }

            public long CharacterId { get; set; }

            public long AccountId { get; set; }

            public int SessionId { get; set; }

            public bool PreserveSessionRegistration { get; set; }
        }

        private sealed class DeferredSessionTeardownScope : IDisposable
        {
            private readonly CommunicationServiceClient _owner;
            private readonly DeferredSessionTeardownContext _context;
            private int _disposed;

            public DeferredSessionTeardownScope(
                CommunicationServiceClient owner,
                DeferredSessionTeardownContext context)
            {
                _owner = owner;
                _context = context;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }

                _owner.CompleteDeferredSessionTeardown(_context);
            }
        }

        private sealed class DeferredSessionTeardownWorkItem
        {
            public long ClientId { get; set; }

            public Guid WorldId { get; set; }

            public long CharacterId { get; set; }

            public long AccountId { get; set; }

            public int SessionId { get; set; }

            public bool PreserveSessionRegistration { get; set; }
        }

        private sealed class DeferredSessionTeardownQueue
        {
            private const int QueueCapacity = 16384;
            private const int WorkerCount = 8;

            private readonly CommunicationServiceClient _owner;
            private readonly BlockingCollection<DeferredSessionTeardownWorkItem>
                _queue;
            private int _highWatermark;

            public DeferredSessionTeardownQueue(
                CommunicationServiceClient owner)
            {
                _owner = owner;
                _queue =
                    new BlockingCollection<DeferredSessionTeardownWorkItem>(
                        new ConcurrentQueue<DeferredSessionTeardownWorkItem>(),
                        QueueCapacity);

                for (int workerIndex = 0;
                     workerIndex < WorkerCount;
                     workerIndex++)
                {
                    var worker = new Thread(Drain)
                    {
                        IsBackground = true,
                        Name = "NosGm.SCS.Teardown." + (workerIndex + 1)
                    };
                    worker.Start();
                }
            }

            public bool TryEnqueue(DeferredSessionTeardownWorkItem item)
            {
                bool added = _queue.TryAdd(item);
                if (!added)
                {
                    return false;
                }

                int depth = _queue.Count;
                RecordHighWatermark(depth);
                return true;
            }

            private void RecordHighWatermark(int depth)
            {
                while (true)
                {
                    int current = Volatile.Read(ref _highWatermark);
                    if (depth <= current)
                    {
                        return;
                    }
                    if (Interlocked.CompareExchange(
                            ref _highWatermark,
                            depth,
                            current) != current)
                    {
                        continue;
                    }

                    if (depth >= 250 && depth % 250 == 0)
                    {
                        Logger.Warn(
                            "[WORLD_REMOTE_TEARDOWN_QUEUE_HIGH_WATERMARK] Depth=" +
                            depth +
                            " Workers=" + WorkerCount);
                    }
                    return;
                }
            }

            private void Drain()
            {
                foreach (DeferredSessionTeardownWorkItem item in
                         _queue.GetConsumingEnumerable())
                {
                    Process(item);
                }
            }

            private void Process(DeferredSessionTeardownWorkItem item)
            {
                if (item.CharacterId > 0 && item.WorldId != Guid.Empty)
                {
                    try
                    {
                        _owner.DisconnectCharacter(
                            item.WorldId,
                            item.CharacterId);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(
                            "[WORLD_REMOTE_TEARDOWN_FAILED] ClientId=" +
                            item.ClientId +
                            " Stage=DISCONNECT_CHARACTER" +
                            " AccountId=" + item.AccountId +
                            " CharacterId=" + item.CharacterId,
                            ex);
                    }
                }

                if (item.AccountId > 0)
                {
                    try
                    {
                        _owner.DisconnectAccount(
                            item.AccountId,
                            item.SessionId,
                            item.PreserveSessionRegistration);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(
                            "[WORLD_REMOTE_TEARDOWN_FAILED] ClientId=" +
                            item.ClientId +
                            " Stage=DISCONNECT_ACCOUNT" +
                            " AccountId=" + item.AccountId +
                            " CharacterId=" + item.CharacterId,
                            ex);
                    }
                }
            }
        }

        #endregion
    }
}
