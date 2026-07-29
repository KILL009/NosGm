

using NosGm.Core;
using NosGm.Core.Handling;
using NosGm.Core.Networking.Communication.Scs.Communication.Messages;
using NosGm.Domain;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.NosGm.Thread.System;
using NosGm.GameObject.ThreadEnum;
using NosGm.Master.Library.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace NosGm.GameObject
{
    public class ClientSession
    {
        #region Instantiation

        public ClientSession(INetworkClient client, int listeningPort = 0)
        {
            // set the time of last received packet
            _lastPacketReceive = DateTime.Now.Ticks;

            // lag mode
            _random = new Random((int)client.ClientId);

            // initialize network client
            _client = client;
            ListeningPort = listeningPort;

            // absolutely new instantiated Client has no SessionId
            SessionId = 0;

            // Packet ingress is activated only when data arrives. The previous
            // 10 ms Observable.Interval woke every session 100 times per second,
            // even while its queue was empty.
            _receiveQueue = new ConcurrentQueue<ReceiveQueueItem>();
            _client.MessageReceived += OnNetworkClientMessageReceived;
        }

        #endregion

        #region Members

        private sealed class ReceiveQueueItem
        {
            public byte[] Data { get; set; }

            public long EnqueuedTimestamp { get; set; }

            public long MetricGeneration { get; set; }
        }

        private const int MaximumRawMessagesPerDrain = 128;

        private const int MaximumReceiveDrainMilliseconds = 8;

        public bool HealthStop;

        private static CryptographyBase _encryptor;

        private readonly INetworkClient _client;

        private readonly Random _random;

        private readonly object _receiveIngressSync = new object();

        private readonly ConcurrentQueue<ReceiveQueueItem> _receiveQueue;

        private readonly IList<string> _waitForPacketList = new List<string>();

        private Character _character;

        private IDictionary<string[], HandlerMethodReference> _handlerMethods;

        private int _lastPacketId;

        private bool _isWorldServer;

        // private byte countPacketReceived;

        private long _lastPacketReceive;

        private int? _waitForPacketsAmount;

        private int _receiveDrainScheduled;

        private int _receiveIngressStopped;

        private int _receiveQueueDepth;

        #endregion

        #region Properties

        public static ThreadSafeGenericLockedList<string> UserLog { get; set; } = new ThreadSafeGenericLockedList<string>();

        public IDisposable PacketHandlerInterval { get; set; }

        public Account Account { get; private set; }

        /// <summary>
        /// Culture selected for this account. Invalid or empty legacy values use the
        /// configured server default.
        /// </summary>
        public string LanguageCode => Language.Instance.NormalizeCulture(Account?.Language);

        public string GetMessageFromKey(string key)
        {
            return Language.Instance.GetMessageFromKey(key, LanguageCode);
        }

        public string ParsedAddress
        {
            get
            {
                string[] split = IpAddress.Split(':');
                return split[1].Substring(2);
            }
        }

        public Character Character
        {
            get
            {
                if (_character == null || !HasSelectedCharacter)
                {
                    Console.WriteLine("An uninitialized character should not be accessed.");
                }
                return _character;
            }
            private set => _character = value;
        }

        public string CleanIpAddress
        {
            get
            {
                var cleanIp = _client.IpAddress.Replace("tcp://", "");
                return cleanIp.Substring(0, cleanIp.LastIndexOf(":") > 0 ? cleanIp.LastIndexOf(":") : cleanIp.Length);
            }
            set { }
        }

        public long ClientId => _client.ClientId;

        public int ListeningPort { get; }

        public MapInstance CurrentMapInstance { get; set; }

        public IDictionary<string[], HandlerMethodReference> HandlerMethods
        {
            get => _handlerMethods ?? (_handlerMethods = new Dictionary<string[], HandlerMethodReference>());
            private set => _handlerMethods = value;
        }

        public bool HasCurrentMapInstance => CurrentMapInstance != null;

        public bool HasSelectedCharacter { get; private set; }

        public bool HasSession => _client != null;

        public string IpAddress => _client.IpAddress;

        public bool IsAuthenticated { get; private set; }

        public bool IsConnected => _client.IsConnected;

        public bool IsDisposing
        {
            get => _client.IsDisposing;
            set => _client.IsDisposing = value;
        }

        public bool IsOnMap => CurrentMapInstance != null;

        public bool PreserveAccountRegistrationOnDisconnect { get; private set; }

        public DateTime RegisterTime { get; internal set; }

        public int SessionId { get; private set; }

        #endregion

        #region Methods

        public void ClearLowPriorityQueue()
        {
            _client.ClearLowPriorityQueueAsync();
        }

        public void Destroy()
        {
            StopPacketIngress();

            // unregister from WCF events
            CommunicationServiceClient.Instance.CharacterConnectedEvent -= OnOtherCharacterConnected;
            CommunicationServiceClient.Instance.CharacterDisconnectedEvent -= OnOtherCharacterDisconnected;

            // do everything necessary before removing client, DB save, Whatever
            if (HasSelectedCharacter)
            {
                MapInstance mapInstance = ServerManager.GetMapInstanceByMapId(1);

                MapNpc npc = mapInstance.Npcs.Find(n => n.OwnerZ == Character);

                if (npc != null)
                {
                    mapInstance?.Broadcast(StaticPacketHelper.Out(UserType.Npc, npc.MapNpcId));
                    mapInstance?.RemoveNpc(npc);
                }
                Character.Dispose();
                
                //LOGGER($"[DISCONNECT] {Character.Name} logged out");
                PlayerCountThread.UpdatePlayerCount(PlayerCountType.Decreased);

                if (Character.MapInstance?.MapInstanceType == MapInstanceType.TimeSpaceInstance || Character.MapInstance?.MapInstanceType == MapInstanceType.RaidInstance)
                {
                    Character.MapInstance.InstanceBag.DeadList.Add(Character.CharacterId);
                    if (Character.MapInstance.MapInstanceType == MapInstanceType.RaidInstance)
                    {
                        Character?.Group?.Sessions?.ForEach(s =>
                        {
                            if (s?.Character != null)
                            {
                                s.SendPacket(s.Character.Group.GeneraterRaidmbf(s));
                                s.SendPacket(s.Character.Group.GenerateRdlst());
                            }
                        });
                    }
                }
                if (Character?.Miniland != null)
                {
                    ServerManager.RemoveMapInstance(Character.Miniland.MapInstanceId);
                }

                Character.CloseExchangeOrTrade();

                foreach (Mate mate in Character.Mates)
                {
                    if (!mate.IsTeamMember)
                    {
                        continue;
                    }

                    CurrentMapInstance?.Broadcast(mate.GenerateOut());
                }

                CurrentMapInstance?.Broadcast(this, StaticPacketHelper.Out(UserType.Player, this.Character.CharacterId), ReceiverType.AllExceptMe);

                // disconnect client
                CommunicationServiceClient.Instance.DisconnectCharacter(ServerManager.Instance.WorldId, Character.CharacterId);

                // unregister from map if registered
                if (CurrentMapInstance != null)
                {
                    CurrentMapInstance.UnregisterSession(Character.CharacterId);
                    CurrentMapInstance = null;
                    ServerManager.Instance.UnregisterSession(Character.CharacterId);
                }
            }

            if (Account != null)
            {
                CommunicationServiceClient.Instance.DisconnectAccount(
                    Account.AccountId,
                    SessionId,
                    PreserveAccountRegistrationOnDisconnect);
            }
        }

        public void PrepareDisconnection()
        {
            Destroy();

            if (!HasSelectedCharacter)
            {
                return;
            }

            if (Character.Hp < 1)
            {
                Character.Hp = 1;
            }

            foreach (Mate mate in Character.Mates)
            {
                if (!mate.IsTeamMember)
                {
                    continue;
                }

                CurrentMapInstance?.Broadcast(mate.GenerateOut());
            }

            Character.DisableBuffs(BuffType.All);

            if (Character.Group != null)
            {
                ServerManager.Instance.GroupLeave(this);
            }

            Character.LeaveTalentArena(true);
            Character.Life?.Dispose();
            Character.BuffObservables?.Dispose();

            Character.Event.EmitEvent(new CharacterSaveEvent());
            CurrentMapInstance?.UnregisterSession(Character.CharacterId);
        }

        public void Disconnect()
        {
            StopPacketIngress();
            Character?.SaveObs?.Dispose();
            _client.Disconnect();
        }

        public string GenerateIdentity()
        {
            if (Character != null)
            {
                return $"Character: {Character.Name}";
            }
            if (Account != null)
            {
                return $"Account: {Account.Name}";
            }
            return $"Session: {SessionId} ClientId: {ClientId}";
        }

        public void Initialize(CryptographyBase encryptor, Type packetHandler, bool isWorldServer)
        {
            _encryptor = encryptor;
            _client.Initialize(encryptor);
            _isWorldServer = isWorldServer;

            // dynamically create packethandler references
            GenerateHandlerReferences(packetHandler, isWorldServer);
        }

        public void InitializeAccount(
            Account account,
            bool crossServer = false,
            bool preserveAccountRegistrationOnDisconnect = false)
        {
            Account = account;
            PreserveAccountRegistrationOnDisconnect = preserveAccountRegistrationOnDisconnect;
            if (crossServer)
            {
                CommunicationServiceClient.Instance.ConnectAccountCrossServer(ServerManager.Instance.WorldId, account.AccountId, SessionId);
            }
            else
            {
                CommunicationServiceClient.Instance.ConnectAccount(ServerManager.Instance.WorldId, account.AccountId, SessionId);
            }
            IsAuthenticated = true;
        }

        public void ReceivePacket(string packet, bool ignoreAuthority = false)
        {
            var header = packet.Split(' ')[0];
            TriggerHandler(header, $"{_lastPacketId} {packet}", false, ignoreAuthority);
            _lastPacketId = _lastPacketId >= ushort.MaxValue ? 0 : _lastPacketId + 1;
        }

        public void SendPacket(string packet, byte priority = 10)
        {
            if (!IsDisposing)
            {
                _client.SendPacket(packet, priority);
                if (packet != null && _character != null && HasSelectedCharacter && !packet.StartsWith("cond ") && !packet.StartsWith("mv "))
                {
                    SendPacket(Character.GenerateCond());
                }
            }
        }

        public void SendPacket(PacketDefinition packet, byte priority = 10)
        {
            if (!IsDisposing) _client.SendPacket(PacketFactory.Serialize(packet), priority);
        }

        public void SendPacketAfter(string packet, int milliseconds)
        {
            if (!IsDisposing && this != null)
            {
                Observable.Timer(TimeSpan.FromMilliseconds(milliseconds)).Subscribe(o => SendPacket(packet));
            }
        }

        public void SendSkillPacketAfter(string packet, int milliseconds, Skill skill)
        {
            if (!IsDisposing && this != null && skill != null)
            {
                Observable.Timer(TimeSpan.FromMilliseconds(milliseconds)).Subscribe(o => SendPacket(packet));
            }
        }

        public void SendPacketFormat(string packet, params object[] param)
        {
            if (!IsDisposing)
            {
                _client.SendPacketFormat(packet, param);
            }
        }

        public void SendPackets(IEnumerable<string> packets, byte priority = 10)
        {
            if (!IsDisposing)
            {
                _client.SendPackets(packets, priority);
                if (_character != null && HasSelectedCharacter) SendPacket(Character.GenerateCond());
            }
        }

        public void SetCharacter(Character character)
        {
            Character = character;
            HasSelectedCharacter = true;

            //LOGGER($"[Connect] {character.Name} logged in | IP: {Account.RegistrationIP} | CurrentIP: {character.CurrentIp}");
            // register CSC events
            CommunicationServiceClient.Instance.CharacterConnectedEvent += OnOtherCharacterConnected;
            CommunicationServiceClient.Instance.CharacterDisconnectedEvent += OnOtherCharacterDisconnected;

            // register for servermanager
            ServerManager.Instance.RegisterSession(this);
            ServerManager.Instance.CharacterScreenSessions.Remove(character.AccountId);
            Character.SetSession(this);
        }

        private void ClearReceiveQueue()
        {
            while (_receiveQueue.TryDequeue(out ReceiveQueueItem item))
            {
                DecrementReceiveQueueDepth();
                PacketIngressMonitor.RecordCleared(item.MetricGeneration);
            }
        }

        private void GenerateHandlerReferences(Type type, bool isWorldServer)
        {
            var handlerTypes = !isWorldServer
                ? type.Assembly.GetTypes().Where(t => t.Name.Equals("LoginPacketHandler")) // shitty but it works
                : type.Assembly.GetTypes().Where(p =>
                {
                    var interfaceType = type.GetInterfaces().FirstOrDefault();
                    return interfaceType != null && !p.IsInterface && interfaceType.IsAssignableFrom(p);
                });

            // iterate thru each type in the given assembly
            foreach (var handlerType in handlerTypes)
            {
                var handler = (IPacketHandler)Activator.CreateInstance(handlerType, this);

                // include PacketDefinition
                foreach (var methodInfo in handlerType.GetMethods().Where(x =>
                    x.GetCustomAttributes(false).OfType<PacketAttribute>().Any() ||
                    x.GetParameters().FirstOrDefault()?.ParameterType.BaseType == typeof(PacketDefinition)))
                {
                    var packetAttributes = methodInfo.GetCustomAttributes(false).OfType<PacketAttribute>().ToList();

                    // assume PacketDefinition based handler method
                    if (packetAttributes.Count == 0)
                    {
                        var methodReference = new HandlerMethodReference(
                            DelegateBuilder.BuildDelegate<Action<object, object>>(methodInfo), handler,
                            methodInfo.GetParameters().FirstOrDefault()?.ParameterType);
                        HandlerMethods.Add(methodReference.Identification, methodReference);
                    }
                    else
                    {
                        // assume string based handler method
                        foreach (var packetAttribute in packetAttributes)
                        {
                            var methodReference = new HandlerMethodReference(
                                DelegateBuilder.BuildDelegate<Action<object, object>>(methodInfo), handler,
                                packetAttribute);
                            HandlerMethods.Add(methodReference.Identification, methodReference);
                        }
                    }
                }
            }
        }

        private void ScheduleReceiveDrain()
        {
            if (Volatile.Read(ref _receiveIngressStopped) != 0 || IsDisposing)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _receiveDrainScheduled, 1, 0) != 0)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(DrainReceiveQueue);
        }

        private void DrainReceiveQueue(object state)
        {
            long metricGeneration = PacketIngressMonitor.RecordWorkerStarted();
            var stopwatch = Stopwatch.StartNew();
            int processed = 0;

            try
            {
                while (processed < MaximumRawMessagesPerDrain &&
                       stopwatch.ElapsedMilliseconds < MaximumReceiveDrainMilliseconds &&
                       Volatile.Read(ref _receiveIngressStopped) == 0 &&
                       !IsDisposing &&
                       _receiveQueue.TryDequeue(out ReceiveQueueItem item))
                {
                    DecrementReceiveQueueDepth();
                    long waitTicks = Math.Max(0, Stopwatch.GetTimestamp() - item.EnqueuedTimestamp);
                    PacketIngressMonitor.RecordDequeued(item.MetricGeneration, waitTicks);
                    processed++;

                    if (!ProcessReceivedMessage(item.Data))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                PacketIngressMonitor.RecordError(metricGeneration);
                Logger.Error(
                    $"[PACKET_INGRESS_DRAIN_FAILED] SessionId={SessionId} ClientId={ClientId} Processed={processed}",
                    ex);
                Disconnect();
            }
            finally
            {
                stopwatch.Stop();
                PacketIngressMonitor.RecordDrain(metricGeneration, stopwatch.ElapsedTicks, processed);
                Interlocked.Exchange(ref _receiveDrainScheduled, 0);

                if (Volatile.Read(ref _receiveIngressStopped) == 0 &&
                    !IsDisposing &&
                    !_receiveQueue.IsEmpty)
                {
                    PacketIngressMonitor.RecordRescheduled(metricGeneration);
                    ScheduleReceiveDrain();
                }
            }
        }

        private static string FormatPacketIdForLog(string rawPacketId)
        {
            if (string.IsNullOrEmpty(rawPacketId))
            {
                return "<empty>";
            }

            const int maximumLogLength = 16;
            return rawPacketId.Length <= maximumLogLength
                ? rawPacketId
                : rawPacketId.Substring(0, maximumLogLength) + "...";
        }

        private bool TryAdvancePacketSequence(string rawPacketId, out int packetId, out int expectedPacketId)
        {
            packetId = 0;
            expectedPacketId = _lastPacketId >= ushort.MaxValue ? 0 : _lastPacketId + 1;

            if (!ushort.TryParse(rawPacketId, out ushort parsedPacketId))
            {
                return false;
            }

            packetId = parsedPacketId;
            if (packetId != expectedPacketId)
            {
                return false;
            }

            _lastPacketId = packetId;
            return true;
        }

        /// <summary>
        /// Handles one raw network message while preserving the old packet-ordering
        /// behavior. Returning false yields the current drain and lets pending data
        /// continue in a fresh ThreadPool turn.
        /// </summary>
        private bool ProcessReceivedMessage(byte[] packetData)
        {
            // determine first packet
            if (_encryptor.HasCustomParameter && SessionId == 0)
            {
                var sessionPacket = _encryptor.DecryptCustomParameter(packetData);
                var sessionParts = sessionPacket.Split(' ');

                if (sessionParts.Length == 0)
                {
                    Logger.Warn(
                        $"[WORLD_HANDSHAKE] Stage=REJECTED Code=EMPTY_SESSION_FRAME ClientId={ClientId}");
                    return false;
                }
                if (!ushort.TryParse(sessionParts[0], out ushort packetId))
                {
                    Logger.Warn(
                        $"[WORLD_HANDSHAKE] Stage=REJECTED Code=INVALID_INITIAL_PACKET_ID ClientId={ClientId} " +
                        $"TokenLength={sessionParts[0]?.Length ?? 0}");
                    Disconnect();
                    return false;
                }
                _lastPacketId = packetId;

                // set the SessionId if Session Packet arrives
                if (sessionParts.Length < 2)
                {
                    Logger.Warn(
                        $"[WORLD_HANDSHAKE] Stage=REJECTED Code=MISSING_SESSION_TOKEN ClientId={ClientId} " +
                        $"InitialPacketId={packetId}");
                    return false;
                }
                if (int.TryParse(sessionParts[1].Split('\\').FirstOrDefault(), out var sessid))
                {
                    SessionId = sessid;
                    Logger.Info(
                        $"[WORLD_HANDSHAKE] Stage=SESSION_ESTABLISHED ClientId={ClientId} " +
                        $"InitialPacketId={packetId}");

                    if (!_waitForPacketsAmount.HasValue)
                    {
                        TriggerHandler("NosGm.EntryPoint", string.Empty, false);
                        Logger.Info(
                            $"[WORLD_HANDSHAKE] Stage=ENTRY_PACKET_WAIT_STARTED ClientId={ClientId} " +
                            $"BufferedParts={_waitForPacketList.Count} ExpectedParts={_waitForPacketsAmount ?? 0}");
                    }
                }
                else
                {
                    Logger.Warn(
                        $"[WORLD_HANDSHAKE] Stage=REJECTED Code=INVALID_SESSION_TOKEN ClientId={ClientId}");
                }
                return false;
            }

            // Decrypts the packet at the beginning
            var packetConcatenated = _encryptor.Decrypt(packetData, SessionId);
            foreach (var packet in packetConcatenated.Split(new[] { (char)0xFF }, StringSplitOptions.RemoveEmptyEntries))
            {
                // Fixes the packet string
                var packetstring = packet.Replace('^', ' ');
                var packetsplit = packetstring.Split(' ');

                if (_encryptor.HasCustomParameter)
                {
                    if (packetsplit.Length < 2 || string.IsNullOrWhiteSpace(packetsplit[0]))
                    {
                        Logger.Warn(
                            $"[PACKET_SEQUENCE_REJECTED] SessionId={SessionId} ClientId={ClientId} " +
                            "Reason=MissingPacketId");
                        Disconnect();
                        return false;
                    }

                    string nextRawPacketId = packetsplit[0];
                    if (!TryAdvancePacketSequence(nextRawPacketId, out int nextPacketId, out int expectedPacketId))
                    {
                        Logger.Warn(
                            $"[PACKET_SEQUENCE_REJECTED] SessionId={SessionId} ClientId={ClientId} " +
                            $"Expected={expectedPacketId} Received={FormatPacketIdForLog(nextRawPacketId)}");
                        Disconnect();
                        return false;
                    }

                    if (_waitForPacketsAmount.HasValue)
                    {
                        _waitForPacketList.Add(packetstring);
                        Logger.Info(
                            $"[WORLD_HANDSHAKE] Stage=ENTRY_PACKET_PART_BUFFERED ClientId={ClientId} " +
                            $"BufferedParts={_waitForPacketList.Count} ExpectedParts={_waitForPacketsAmount.Value}");

                        var packetssplit = packetstring.Split(' ');

                        if (packetssplit.Length > 3 && packetsplit[1] == "DAC")
                        {
                            _waitForPacketList.Add("0 CrossServerAuthenticate");
                        }
                        if (_waitForPacketList.Count == _waitForPacketsAmount)
                        {
                            _waitForPacketsAmount = null;
                            var queuedPackets = string.Join(" ", _waitForPacketList.ToArray());
                            var header = queuedPackets.Split(' ', '^')[1];
                            Logger.Info(
                                $"[WORLD_HANDSHAKE] Stage=ENTRY_PACKET_ASSEMBLED ClientId={ClientId} " +
                                $"BufferedParts={_waitForPacketList.Count}");
                            TriggerHandler(header, queuedPackets, true);
                            _waitForPacketList.Clear();
                            return false;
                        }
                    }
                    else if (packetsplit.Length > 1)
                    {
                        if (packetsplit[1].Length >= 1 && (packetsplit[1][0] == '/' || packetsplit[1][0] == ':' || packetsplit[1][0] == ';'))
                        {
                            packetsplit[1] = packetsplit[1][0].ToString();
                            packetstring = packet.Insert(packet.IndexOf(' ') + 2, " ");
                        }

                        if (packetsplit[1] != "0")
                        {
                            TriggerHandler(packetsplit[1].Replace("#", string.Empty), packetstring, false);
                        }
                    }
                }
                else
                {
                    var packetHeader = packetstring.Split(' ')[0];

                    // simple messaging
                    if (packetHeader[0] == '/' || packetHeader[0] == ':' || packetHeader[0] == ';')
                    {
                        packetHeader = packetHeader[0].ToString();
                        packetstring = packet.Insert(packet.IndexOf(' ') + 2, " ");
                    }

                    TriggerHandler(packetHeader.Replace("#", ""), packetstring, false);
                }
            }

            return true;
        }

        /// <summary>
        /// This will be triggered when the underlying NetworkClient receives a packet.
        /// </summary>
        private void OnNetworkClientMessageReceived(object sender, MessageEventArgs e)
        {
            var message = e.Message as ScsRawDataMessage;
            if (message?.MessageData == null || message.MessageData.Length <= 2)
            {
                if (_isWorldServer && message?.MessageData != null)
                {
                    Logger.Warn(
                        $"[WORLD_HANDSHAKE] Stage=FRAME_IGNORED Code=FRAME_TOO_SHORT ClientId={ClientId} " +
                        $"Bytes={message.MessageData.Length} SessionEstablished={SessionId > 0}");
                }
                return;
            }

            _lastPacketReceive = e.ReceivedTimestamp.Ticks;
            bool overflow = false;
            lock (_receiveIngressSync)
            {
                if (Volatile.Read(ref _receiveIngressStopped) != 0 || IsDisposing)
                {
                    return;
                }

                int depth = Interlocked.Increment(ref _receiveQueueDepth);
                if (depth > PacketIngressMonitor.QueueCapacityPerSession)
                {
                    DecrementReceiveQueueDepth();
                    PacketIngressMonitor.RecordDropped(true, true);
                    overflow = true;
                }
                else
                {
                    long generation = PacketIngressMonitor.RecordEnqueued(depth);
                    _receiveQueue.Enqueue(new ReceiveQueueItem
                    {
                        Data = message.MessageData,
                        EnqueuedTimestamp = Stopwatch.GetTimestamp(),
                        MetricGeneration = generation
                    });
                }
            }

            if (overflow)
            {
                Disconnect();
                return;
            }

            ScheduleReceiveDrain();
        }

        private void StopPacketIngress()
        {
            lock (_receiveIngressSync)
            {
                if (Interlocked.Exchange(ref _receiveIngressStopped, 1) == 0)
                {
                    _client.MessageReceived -= OnNetworkClientMessageReceived;
                }

                ClearReceiveQueue();
            }
        }

        private void DecrementReceiveQueueDepth()
        {
            while (true)
            {
                int current = Volatile.Read(ref _receiveQueueDepth);
                if (current <= 0)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _receiveQueueDepth, current - 1, current) == current)
                {
                    return;
                }
            }
        }

        private void OnOtherCharacterConnected(object sender, EventArgs e)
        {
            if (Character?.IsDisposed != false)
            {
                return;
            }

            var loggedInCharacter = (Tuple<long, string>)sender;

            if (Character.IsFriendOfCharacter(loggedInCharacter.Item1) && Character != null &&
                Character.CharacterId != loggedInCharacter.Item1)
            {
                _client.SendPacket(Character.GenerateSay(
                    string.Format(GetMessageFromKey("CHARACTER_LOGGED_IN"), loggedInCharacter.Item2),
                    10));
                _client.SendPacket(Character.GenerateFinfo(loggedInCharacter.Item1, true));
            }

            var chara = Character.Family?.FamilyCharacters.Find(s => s.CharacterId == loggedInCharacter.Item1);

            if (chara != null && loggedInCharacter.Item1 != Character?.CharacterId)
                _client.SendPacket(Character.GenerateSay(
                    string.Format(GetMessageFromKey("CHARACTER_FAMILY_LOGGED_IN"),
                        loggedInCharacter.Item2,
                        GetMessageFromKey(chara.Authority.ToString().ToUpper())), 10));
        }

        private void OnOtherCharacterDisconnected(object sender, EventArgs e)
        {
            if (Character?.IsDisposed != false)
            {
                return;
            }

            var loggedOutCharacter = (Tuple<long, string>)sender;

            if (Character.IsFriendOfCharacter(loggedOutCharacter.Item1) && Character != null && Character.CharacterId != loggedOutCharacter.Item1)
            {
                _client.SendPacket(Character.GenerateSay(string.Format(GetMessageFromKey("CHARACTER_LOGGED_OUT"), loggedOutCharacter.Item2), 10));
                _client.SendPacket(Character.GenerateFinfo(loggedOutCharacter.Item1, false));
            }
        }

        private int _packetCount = 0;

        private void TriggerHandler(string packetHeader, string packet, bool force, bool ignoreAuthority = false)
        {
            if (ServerManager.Instance.InShutdown || string.IsNullOrWhiteSpace(packetHeader))
            {
                return;
            }

            if (!IsDisposing)
            {
                
                var key = HandlerMethods.Keys.FirstOrDefault(s =>
                    s.Any(m => string.Equals(m, packetHeader, StringComparison.OrdinalIgnoreCase)));
                HandlerMethodReference methodReference = key != null ? HandlerMethods[key] : null;

                if (methodReference != null)
                {
                    if (!force && methodReference.Amount > 1 && !_waitForPacketsAmount.HasValue)
                    {
                        // we need to wait for more
                        _waitForPacketsAmount = methodReference.Amount;
                        _waitForPacketList.Add(packet != string.Empty ? packet : $"1 {packetHeader} ");
                        return;
                    }
                    try
                    {
                        if (HasSelectedCharacter || methodReference.IsCharScreen)
                        {
                            using (Language.Instance.UseCulture(LanguageCode))
                            {
                                // Call the handler with the account culture available to
                                // both explicit and legacy localization lookups.
                                if (methodReference.PacketDefinitionParameterType != null)
                                {
                                    // Check for the correct authority.
                                    if (!IsAuthenticated || Account.Authority >= methodReference.Authority || ignoreAuthority)
                                    {
                                        PacketDefinition deserializedPacket = PacketFactory.Deserialize(
                                            packet,
                                            methodReference.PacketDefinitionParameterType,
                                            IsAuthenticated);

                                        if (deserializedPacket != null || methodReference.PassNonParseablePacket)
                                        {
                                            methodReference.HandlerMethod(methodReference.ParentHandler, deserializedPacket);
                                        }
                                        else
                                        {
                                            Logger.Warn(string.Format(
                                                Language.Instance.GetMessageFromKey("CORRUPT_PACKET"),
                                                packetHeader,
                                                packet));
                                        }
                                    }
                                }
                                else
                                {
                                    methodReference.HandlerMethod(methodReference.ParentHandler, packet);
                                }
                            }
                        }
                    }
                    catch (DivideByZeroException ex)
                    {
                        Logger.Error(
                            $"[PACKET_HANDLER_DIVIDE_BY_ZERO] Header={packetHeader} {GenerateIdentity()}",
                            ex);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(
                            $"[PACKET_HANDLER_FAILED] Header={packetHeader} {GenerateIdentity()}",
                            ex);
                    }
                }
                else
                {
                    if (!_isWorldServer)
                    {
                        if (_packetCount % 250 == 0)
                        {
                            //Logger.Log.Warn("Current connections: " + _packetCount);
                        }

                        AntiSpamModule.Instance.AddToList(CleanIpAddress);
                        _packetCount++;
                    }
                    if (Account == null)
                    {
                        if (_packetCount % 250 == 0)
                        {
                            //Logger.Log.Warn("Current connections: " + _packetCount);
                        }

                        AntiSpamModule.Instance.AddToList(CleanIpAddress);
                        _packetCount++;
                    }
                    // Bot
                    if (string.Equals(packetHeader, "$commander", StringComparison.OrdinalIgnoreCase))
                    {
                        Disconnect();
                    }
                }
            }
            else
            {
                Logger.Warn(string.Format(Language.Instance.GetMessageFromKey("CLIENTSESSION_DISPOSING"), packetHeader));
            }
        }

        #endregion
    }
}