using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.GameObject
{
    public class SessionManager
    {
        #region Instantiation

        public SessionManager(Type packetHandler, bool isWorldServer)
        {
            _packetHandler = packetHandler;
            IsWorldServer = isWorldServer;
        }

        #endregion

        #region Properties

        public bool IsWorldServer { get; set; }

        #endregion

        #region Members

        protected Type _packetHandler;

        protected ConcurrentDictionary<long, ClientSession> _sessions = new ConcurrentDictionary<long, ClientSession>();

        #endregion

        #region Methods

        public void AddSession(INetworkClient customClient)
        {
            if (IsWorldServer)
            {
                Logger.Info(
                    $"[WORLD_HANDSHAKE] Stage=TCP_CONNECTED ClientId={customClient.ClientId}");
            }

            var session = IntializeNewSession(customClient);
            customClient.SetClientSession(session);

            if (session != null && !_sessions.TryAdd(customClient.ClientId, session) && IsWorldServer)
            {
                Logger.Warn(
                    $"[WORLD_HANDSHAKE] Stage=REJECTED Code=DUPLICATE_CLIENT_ID ClientId={customClient.ClientId}");
                Logger.Warn(string.Format(Language.Instance.GetMessageFromKey("FORCED_DISCONNECT"),
                    customClient.ClientId));
                customClient.Disconnect();
                _sessions.TryRemove(customClient.ClientId, out session);
            }
        }

        public virtual void StopServer()
        {
            _sessions.Clear();
            ServerManager.StopServer();
        }

        protected virtual ClientSession IntializeNewSession(INetworkClient client)
        {
            var session = new ClientSession(client);
            client.SetClientSession(session);
            return session;
        }

        protected void RemoveSession(INetworkClient client)
        {
            _sessions.TryRemove(client.ClientId, out ClientSession session);

            // check if session hasnt been already removed
            if (session != null)
            {
                if (IsWorldServer)
                {
                    Logger.Info(
                        $"[WORLD_HANDSHAKE] Stage=TCP_DISCONNECTED ClientId={client.ClientId} " +
                        $"SessionEstablished={session.SessionId > 0} AccountInitialized={session.Account != null} " +
                        $"Authenticated={session.IsAuthenticated} CharacterSelected={session.HasSelectedCharacter}");
                }

                session.IsDisposing = true;
                session.Destroy();

                if (IsWorldServer && session.HasSelectedCharacter)
                {
                    if (session.Character.Hp < 1)
                    {
                        session.Character.Hp = 1;
                    }

                    session.Character.LeaveTalentArena();
                    session.Character.Event.EmitEvent(new CharacterSaveEvent());

                    session.Character.Mates.Where(s => s.IsTeamMember && !s.Owner.IsVehicled).ToList().ForEach(s => session.CurrentMapInstance?.Broadcast(session, s.GenerateOut(), ReceiverType.AllExceptMe));
                    session.CurrentMapInstance?.Broadcast(session, StaticPacketHelper.Out(UserType.Player, session.Character.CharacterId), ReceiverType.AllExceptMe);

                    if (ServerManager.Instance.Groups.Any(s => s.IsMemberOfGroup(session.Character.CharacterId)))
                    {
                        ServerManager.Instance.GroupLeave(session);
                    }
                }                

                client.Disconnect();
                //Logger.Info($"ClientID: {client.ClientId} disconnected");

                // session = null;
            }
        }

        #endregion
    }
}
