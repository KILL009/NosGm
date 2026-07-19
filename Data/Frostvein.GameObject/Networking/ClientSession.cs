

using Frostvein.Core;
using Frostvein.Core.Handling;
using Frostvein.Core.Networking.Communication.Scs.Communication.Messages;
using Frostvein.Domain;
using Frostvein.GameObject.Characters.Events;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Frostvein.Thread.System;
using Frostvein.GameObject.ThreadEnum;
using Frostvein.Master.Library.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;


namespace Frostvein.GameObject
{
    public class ClientSession
    {
        #region Instantiation

        public ClientSession(INetworkClient client)
        {
            // set the time of last received packet
            _lastPacketReceive = DateTime.Now.Ticks;

            // lag mode
            _random = new Random((int)client.ClientId);

            // initialize network client
            _client = client;

            // absolutely new instantiated Client has no SessionId
            SessionId = 0;

            // register for NetworkClient events
            _client.MessageReceived += OnNetworkClientMessageReceived;

            // start observer for receiving packets
            _receiveQueue = new ConcurrentQueue<byte[]>();
            _receiveQueueObservable = Observable.Interval(new TimeSpan(0, 0, 0, 0, 10)).Subscribe(x => HandlePackets());
        }

        #endregion

        #region Members

        public bool HealthStop;

        private static CryptographyBase _encryptor;

        private readonly INetworkClient _client;

        private readonly Random _random;

        private readonly ConcurrentQueue<byte[]> _receiveQueue;

        private readonly object _receiveQueueObservable;

        private readonly IList<string> _waitForPacketList = new List<string>();

        private Character _character;

        private IDictionary<string[], HandlerMethodReference> _handlerMethods;

        private int _lastPacketId;

        private bool _isWorldServer;

        // private byte countPacketReceived;

        private long _lastPacketReceive;

        private int? _waitForPacketsAmount;

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
                CommunicationServiceClient.Instance.DisconnectAccount(Account.AccountId);
            }

            ClearReceiveQueue();
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
            return $"Account: {Account.Name}";
        }

        public void Initialize(CryptographyBase encryptor, Type packetHandler, bool isWorldServer)
        {
            _encryptor = encryptor;
            _client.Initialize(encryptor);
            _isWorldServer = isWorldServer;

            // dynamically create packethandler references
            GenerateHandlerReferences(packetHandler, isWorldServer);
        }

        public void InitializeAccount(Account account, bool crossServer = false)
        {
            Account = account;
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
            _lastPacketId++;
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
            while (_receiveQueue.TryDequeue(out _))
            {
                // do nothing
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

        /// <summary>
        ///     Handle the packet received by the Client.
        /// </summary>
        private void HandlePackets()
        {
            try
            {
                while (_receiveQueue.TryDequeue(out var packetData))
                {
                    // determine first packet
                    if (_encryptor.HasCustomParameter && SessionId == 0)
                    {
                        var sessionPacket = _encryptor.DecryptCustomParameter(packetData);

                        var sessionParts = sessionPacket.Split(' ');

                        if (sessionParts.Length == 0)
                        {
                            return;
                        }
                        if (!int.TryParse(sessionParts[0], out int packetId))
                        {
                            Disconnect();
                        }
                        _lastPacketId = packetId;

                        // set the SessionId if Session Packet arrives
                        if (sessionParts.Length < 2)
                        {
                            return;
                        }
                        if (int.TryParse(sessionParts[1].Split('\\').FirstOrDefault(), out var sessid))
                        {
                            SessionId = sessid;
                            //Logger.Info($"{SessionId} entered the World Server");

                            if (!_waitForPacketsAmount.HasValue)
                            {
                                TriggerHandler("Frostvein.EntryPoint", string.Empty, false);
                            }
                        }
                        return;
                    }

                    // Decrypts the packet at the beginning
                    var packetConcatenated = _encryptor.Decrypt(packetData, SessionId);
                    foreach (var packet in packetConcatenated.Split(new[] { (char)0xFF },StringSplitOptions.RemoveEmptyEntries))
                    {
                        // FIxes the packet string
                        var packetstring = packet.Replace('^', ' ');
                        var packetsplit = packetstring.Split(' ');

                        if (_encryptor.HasCustomParameter)
                        {
                            var nextRawPacketId = packetsplit[0];
                            if (!int.TryParse(nextRawPacketId, out var nextPacketId) && nextPacketId != _lastPacketId + 1)
                            {
                                //LOGGERServerLog($"KeepAlive was corrupt. Removed Session", LogType.ServerError);
                                _client.Disconnect();
                                return;
                            }

                            if (nextPacketId == 0)
                            {
                                if (_lastPacketId == ushort.MaxValue)
                                {
                                    _lastPacketId = nextPacketId;
                                }
                            }
                            else
                            {
                                _lastPacketId = nextPacketId;
                            }

                            if (_waitForPacketsAmount.HasValue)
                            {
                                _waitForPacketList.Add(packetstring);

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
                                    TriggerHandler(header, queuedPackets, true);
                                    _waitForPacketList.Clear();
                                    return;
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
                }
            }
            catch (Exception ex)
            {
                //LOGGERServerLog($"[Invalid Packet] {ex.ToString()}", LogType.ServerError);
                Disconnect();
            }
        }

        /// <summary>
        ///     This will be triggered when the underlying NetworkClient receives a packet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNetworkClientMessageReceived(object sender, MessageEventArgs e)
        {
            var message = e.Message as ScsRawDataMessage;
            if (message == null) return;
            if (message.MessageData.Length > 0 && message.MessageData.Length > 2)
                _receiveQueue.Enqueue(message.MessageData);
            _lastPacketReceive = e.ReceivedTimestamp.Ticks;
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
                
                var key = HandlerMethods.Keys.FirstOrDefault(s => s.Any(m => string.Equals(m, packetHeader, StringComparison.CurrentCultureIgnoreCase)));
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

                    }
                    catch (Exception e)
                    {

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
                    if (packetHeader.ToLower() == "$commander")
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