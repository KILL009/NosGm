


using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace NosGm.GameObject
{
    public abstract class BroadcastableBase : IDisposable
    {
        #region Instantiation

        protected BroadcastableBase()
        {
            LastUnregister = DateTime.Now.AddMinutes(-1);
            _sessions = new ThreadSafeSortedList<long, ClientSession>();
        }

        #endregion

        #region Members

        /// <summary>
        ///     List of all connected clients.
        /// </summary>
        private readonly ThreadSafeSortedList<long, ClientSession> _sessions;

        private bool _disposed;

        #endregion

        #region Properties

        public IEnumerable<Mate> Mates
        {
            get
            {
                var mates = new List<Mate>();
                Sessions.ToList().ForEach(s =>
                    mates.AddRange(s.Character?.Mates.Where(m => m.IsTeamMember || m.IsTemporalMate)));
                return mates;
            }
        }

        public IEnumerable<ClientSession> Sessions =>
            _sessions.Where(s => s.HasSelectedCharacter && !s.IsDisposing && s.IsConnected);

        protected DateTime LastUnregister { get; private set; }

        #endregion

        #region Methods

        public void Broadcast(string packet, ReceiverType receiver = ReceiverType.All)
        {
            Broadcast(null, packet, receiver);
        }

        public void Broadcast(string packet, int xRangeCoordinate, int yRangeCoordinate)
        {
            Broadcast(new BroadcastPacket(null, packet, ReceiverType.AllInRange, xCoordinate: xRangeCoordinate,
                yCoordinate: yRangeCoordinate));
        }

        public void Broadcast(PacketDefinition packet, ReceiverType receiver = ReceiverType.All)
        {
            Broadcast(null, packet, receiver);
        }

        public void Broadcast(PacketDefinition packet, int xRangeCoordinate, int yRangeCoordinate)
        {
            Broadcast(new BroadcastPacket(null, PacketFactory.Serialize(packet), ReceiverType.AllInRange,
                xCoordinate: xRangeCoordinate, yCoordinate: yRangeCoordinate));
        }

        public void Broadcast(ClientSession client, PacketDefinition packet, ReceiverType receiver = ReceiverType.All,
            string characterName = "", long characterId = -1)
        {
            Broadcast(client, PacketFactory.Serialize(packet), receiver, characterName, characterId);
        }

        public void Broadcast(BroadcastPacket packet)
        {
            try
            {
                spreadBroadcastpacket(packet);
            }
            catch (Exception ex)
            {
                //LOGGERServerLog($"{ex.ToString()}", LogType.ServerError);
            }
        }

        public void Broadcast(ClientSession client, string content, ReceiverType receiver = ReceiverType.All,
            string characterName = "", long characterId = -1)
        {
            try
            {
                if (client == null || client.Character == null || !client.Character.InvisibleGm ||
                    content.StartsWith("out", StringComparison.CurrentCulture))
                {
                    spreadBroadcastpacket(new BroadcastPacket(client, content, receiver, characterName, characterId));
                }
            }
            catch (Exception ex)
            {
                //LOGGERServerLog($"{ex.ToString()}", LogType.ServerError);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                _disposed = true;
                //LOGGER("[Dispose] Successfully disposed");
            }
        }

        public async Task DisposeAsync()
        {
            if (!_disposed)
            {
                await DisposeAsync(true);
                GC.SuppressFinalize(this);
                _disposed = true;
                ////await //LOGGER("[Dispose] Successfully disposed");
            }
        }

        public Character GetCharacterById(long characterId) => GetSessionByCharacterId(characterId)?.Character;

        public ClientSession GetSessionByCharacterId(long characterId) => _sessions.ContainsKey(characterId) ? _sessions[characterId] : null;

        public void RegisterSession(ClientSession session)
        {
            if (!session.HasSelectedCharacter)
            {
                return;
            }

            session.RegisterTime = DateTime.Now;

            // Create a ChatClient and store it in a collection
            _sessions[session.Character.CharacterId] = session;
            if (session.HasCurrentMapInstance)
            {
                session.CurrentMapInstance.IsSleeping = false;
            }
        }

        public void UnregisterSession(long characterId)
        {
            // Get client from client list, if not in list do not continue
            var session = _sessions[characterId];
            if (session == null)
            {
                return;
            }

            // Remove client from online clients list
            _sessions.Remove(characterId);
            if (session.HasCurrentMapInstance && _sessions.Count == 0)
            {
                session.CurrentMapInstance.IsSleeping = true;
            }

            LastUnregister = DateTime.Now;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sessions.Dispose();
            }
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                await _sessions.DisposeAsync();
            }
        }

        private void spreadBroadcastpacket(BroadcastPacket sentPacket)
        {
            IEnumerable<ClientSession> sessions = Sessions;
            if (sessions != null && !string.IsNullOrEmpty(sentPacket?.Packet))
            {
                switch (sentPacket.Receiver)
                {
                    case ReceiverType.All: // send packet to everyone
                        if (sentPacket.Packet.StartsWith("out", StringComparison.CurrentCulture))
                        {
                            foreach (var session in sessions.Where(s =>
                                    s.Character == null || !s.Character.IsChangingMapInstance))
                            {
                                if (session.HasSelectedCharacter)
                                {
                                    if (sentPacket.Sender != null)
                                    {
                                        if (!sentPacket.Sender.Character.IsBlockedByCharacter(session.Character
                                                                                                     .CharacterId))
                                        {
                                            session.SendPacket(sentPacket.Packet);
                                        }
                                    }
                                    else
                                    {
                                        session.SendPacket(sentPacket.Packet);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var session in sessions)
                            {
                                if (session?.HasSelectedCharacter == true)
                                {
                                    if (sentPacket.Sender != null)
                                    {
                                        if (!sentPacket.Sender.Character.IsBlockedByCharacter(session.Character
                                                                                                     .CharacterId) ||
                                            sentPacket.Packet.StartsWith("revive", StringComparison.CurrentCulture))
                                        {
                                            session.SendPacket(sentPacket.Packet);
                                        }
                                    }
                                    else
                                    {
                                        session.SendPacket(sentPacket.Packet);
                                    }
                                }
                            }
                        }

                        break;

                    case ReceiverType.AllExceptMe: // send to everyone except the sender
                        if (sentPacket.Packet.StartsWith("out", StringComparison.CurrentCulture))
                        {
                            foreach (var session in sessions.Where(s =>
                                    s?.SessionId != sentPacket.Sender?.SessionId &&
                                    (s.Character == null || !s.Character.IsChangingMapInstance)))
                            {
                                if (session.HasSelectedCharacter)
                                {
                                    if (sentPacket.Sender != null)
                                    //if (!sentPacket.Sender.Character.IsBlockedByCharacter(session.Character.CharacterId))
                                    {
                                        session.SendPacket(sentPacket.Packet);
                                    }
                                    else
                                    {
                                        session.SendPacket(sentPacket.Packet);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var session in sessions.Where(s => s?.SessionId != sentPacket.Sender?.SessionId))
                            {
                                if (session?.HasSelectedCharacter == true)
                                {
                                    if (sentPacket.Sender != null)
                                    {
                                        if (!sentPacket.Sender.Character.IsBlockedByCharacter(session.Character
                                                                                                     .CharacterId))
                                        {
                                            session.SendPacket(sentPacket.Packet);
                                        }
                                    }
                                    else
                                    {
                                        session.SendPacket(sentPacket.Packet);
                                    }
                                }
                            }
                        }

                        break;

                    case ReceiverType.AllExceptGroup:
                        if (sentPacket.Packet.StartsWith("out", StringComparison.CurrentCulture))
                        {
                            foreach (var session in sessions.Where(s =>
                                    s.SessionId != sentPacket.Sender.SessionId &&
                                    (s.Character == null || !s.Character.IsChangingMapInstance)))
                            {
                                if (session.HasSelectedCharacter)
                                {
                                    if (sentPacket.Sender != null)
                                    {
                                        if (!sentPacket.Sender.Character.IsBlockedByCharacter(session.Character
                                                                                                     .CharacterId))
                                        {
                                            session.SendPacket(sentPacket.Packet);
                                        }
                                    }
                                    else
                                    {
                                        session.SendPacket(sentPacket.Packet);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var session in sessions.Where(s =>
                                    s.SessionId != sentPacket.Sender.SessionId &&
                                    (s.Character?.Group == null || s.Character?.Group?.GroupId !=
                                            sentPacket.Sender?.Character?.Group?.GroupId)))
                            {
                                if (session.HasSelectedCharacter &&
                                    !sentPacket.Sender.Character.IsBlockedByCharacter(session.Character.CharacterId))
                                {
                                    session.SendPacket(sentPacket.Packet);
                                }
                            }
                        }

                        break;

                    case ReceiverType.AllExceptMeAct4: // send to everyone except the sender(Act4)
                        foreach (var session in sessions.Where(s =>
                                s.SessionId != sentPacket.Sender?.SessionId &&
                                (s.Character == null || !s.Character.IsChangingMapInstance)))
                        {
                            if (session?.HasSelectedCharacter == true)
                            {
                                if (sentPacket.Sender != null)
                                {
                                    if (!sentPacket.Sender.Character.IsBlockedByCharacter(session.Character.CharacterId)
                                    )
                                    {
                                        if (session.Character.Faction == sentPacket.Sender.Character.Faction)
                                        {
                                            session.SendPacket(sentPacket.Packet);
                                        }
                                        else if (session.Account.Authority >=
                                                 AuthorityType.GM /*|| session.Account.Authority == AuthorityType.Moderator*/)
                                        {
                                            var vals = sentPacket.Packet.Split(' ');
                                            if (vals[0] == "say" && vals[3] == "13")
                                            {
                                                vals[5] = $"[{sentPacket.Sender.Character.Faction}] {vals[5]}";
                                                sentPacket.Packet = string.Join(" ", vals);
                                            }

                                            session.SendPacket(sentPacket.Packet);
                                        }
                                    }
                                }
                                else
                                {
                                    session.SendPacket(sentPacket.Packet);
                                }
                            }
                        }

                        break;

                    case ReceiverType.AllInRange: // send to everyone which is in a range of 50x50
                        if (sentPacket.XCoordinate != 0 && sentPacket.YCoordinate != 0)
                        {
                            foreach (var session in sessions.Where(s =>
                                    s?.Character.IsInRange(sentPacket.XCoordinate, sentPacket.YCoordinate) == true &&
                                    (s.Character == null || !s.Character.IsChangingMapInstance)))
                            {
                                if (session?.HasSelectedCharacter == true)
                                {
                                    if (sentPacket.Sender != null)
                                    {
                                        if (!sentPacket.Sender.Character.IsBlockedByCharacter(session.Character
                                                                                                     .CharacterId))
                                        {
                                            session.SendPacket(sentPacket.Packet);
                                        }
                                    }
                                    else
                                    {
                                        session.SendPacket(sentPacket.Packet);
                                    }
                                }
                            }
                        }

                        break;

                    case ReceiverType.OnlySomeone:
                        if (sentPacket.SomeonesCharacterId > 0 ||
                            !string.IsNullOrEmpty(sentPacket.SomeonesCharacterName))
                        {
                            var targetSession = sessions.SingleOrDefault(s =>
                                    s.Character.CharacterId == sentPacket.SomeonesCharacterId ||
                                    s.Character.Name == sentPacket.SomeonesCharacterName);
                            if (targetSession?.HasSelectedCharacter == true)
                            {
                                if (sentPacket.Sender != null)
                                {
                                    if (!sentPacket.Sender.Character.IsBlockedByCharacter(targetSession.Character
                                                                                                       .CharacterId))
                                    {
                                        targetSession.SendPacket(sentPacket.Packet);
                                    }
                                    else
                                    {
                                        sentPacket.Sender.SendPacket(
                                                UserInterfaceHelper.GenerateInfo(
                                                        Language.Instance.GetMessageFromKey("BLACKLIST_BLOCKED")));
                                    }
                                }
                                else
                                {
                                    targetSession.SendPacket(sentPacket.Packet);
                                }
                            }
                        }

                        break;

                    case ReceiverType.AllNoEmoBlocked:
                        foreach (var session in sessions.Where(s => s?.Character.EmoticonsBlocked == false))
                        {
                            if (session?.HasSelectedCharacter == true &&
                                sentPacket.Sender?.Character.IsBlockedByCharacter(session.Character.CharacterId) ==
                                false)
                            {
                                session.SendPacket(sentPacket.Packet);
                            }
                        }

                        break;

                    case ReceiverType.AllNoHeroBlocked:
                        foreach (var session in sessions.Where(s => s?.Character.HeroChatBlocked == false))
                        {
                            if (session?.HasSelectedCharacter == true &&
                                sentPacket.Sender?.Character.IsBlockedByCharacter(session.Character.CharacterId) ==
                                false)
                            {
                                session.SendPacket(sentPacket.Packet);
                            }
                        }

                        break;

                    case ReceiverType.Group:
                        foreach (var session in sessions.Where(s =>
                                s.Character?.Group != null && sentPacket.Sender?.Character?.Group != null &&
                                s.Character.Group.GroupId == sentPacket.Sender.Character.Group.GroupId))
                        {
                            session.SendPacket(sentPacket.Packet);
                        }

                        break;

                    case ReceiverType.OnlySomeLevel:
                        if (sentPacket.LevelMinimum != 0 && sentPacket.LevelMaximum != 0)
                        {
                            Parallel.ForEach(sessions.Where(s => s?.Character.Level >= sentPacket.LevelMinimum && s?.Character.Level <= sentPacket.LevelMaximum), session =>
                            {
                                if (session?.HasSelectedCharacter == true)
                                {
                                    session.SendPacket(sentPacket.Packet);
                                }
                            });
                        }
                        break;

                    case ReceiverType.AllInTimeSpace:
                        foreach (var session in sessions.Where(s => s.Character?.Timespace != null))
                        {
                            session.SendPacket(sentPacket.Packet);
                        }
                        foreach (var session in sessions.Where(s => s.CurrentMapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance))
                        {
                            session.SendPacket(sentPacket.Packet);
                        }
                        break;

                    case ReceiverType.Unknown:
                        break;
                }
            }
        }
        #endregion
    }
}