using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
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

            // A disconnect callback can be raised more than once while a socket is
            // being torn down. Only the callback that successfully removed the
            // session owns cleanup.
            if (session == null)
            {
                return;
            }

            if (IsWorldServer)
            {
                Logger.Info(
                    $"[WORLD_HANDSHAKE] Stage=TCP_DISCONNECTED ClientId={client.ClientId} " +
                    $"SessionEstablished={session.SessionId > 0} AccountInitialized={session.Account != null} " +
                    $"Authenticated={session.IsAuthenticated} CharacterSelected={session.HasSelectedCharacter}");
            }

            session.IsDisposing = true;

            // Do not block the socket/disconnect callback on database persistence.
            // The asynchronous teardown awaits the bounded CharacterSaveEvent before
            // destroying Character, which prevents both SqlClient pool stampedes and
            // save-vs-dispose races during mass disconnects.
            _ = RunSessionTeardownAsync(client, session);
        }

        private async Task RunSessionTeardownAsync(INetworkClient client, ClientSession session)
        {
            try
            {
                // Character cleanup must run while the Character is still alive.
                // ClientSession.Destroy() calls Character.Dispose(), unregisters the
                // character and emits the map leave broadcasts.
                if (IsWorldServer && session.HasSelectedCharacter)
                {
                    RunDisconnectCleanupStep(client, "RESTORE_HP", () =>
                    {
                        if (session.Character.Hp < 1)
                        {
                            session.Character.Hp = 1;
                        }
                    });

                    RunDisconnectCleanupStep(
                        client,
                        "LEAVE_TALENT_ARENA",
                        () => session.Character.LeaveTalentArena());

                    await RunDisconnectCleanupStepAsync(
                            client,
                            "SAVE_CHARACTER",
                            () => session.Character.Event.EmitEventAsync(new CharacterSaveEvent()))
                        .ConfigureAwait(false);

                    RunDisconnectCleanupStep(client, "GROUP_LEAVE", () =>
                    {
                        if (ServerManager.Instance.Groups.Any(
                                group => group.IsMemberOfGroup(session.Character.CharacterId)))
                        {
                            ServerManager.Instance.GroupLeave(session);
                        }
                    });
                }

                // World TCP disconnect callbacks must never block for the legacy SCS
                // request/reply timeout. Capture the remote account/character lifecycle
                // mutations and let CommunicationServiceClient drain them on its
                // dedicated bounded worker queue after local teardown has completed.
                RunDisconnectCleanupStep(client, "DESTROY_SESSION", () =>
                {
                    if (!IsWorldServer)
                    {
                        session.Destroy();
                        return;
                    }

                    long characterId = session.HasSelectedCharacter && session.Character != null
                        ? session.Character.CharacterId
                        : 0;
                    long accountId = session.Account?.AccountId ?? 0;

                    using (CommunicationServiceClient.Instance.BeginDeferredSessionTeardown(
                               client.ClientId,
                               ServerManager.Instance.WorldId,
                               characterId,
                               accountId,
                               session.SessionId,
                               session.PreserveAccountRegistrationOnDisconnect))
                    {
                        session.Destroy();
                    }
                });
                RunDisconnectCleanupStep(client, "DISCONNECT_SOCKET", client.Disconnect);
            }
            catch (Exception ex)
            {
                string scope = IsWorldServer ? "WORLD" : "SESSION";
                Logger.Error(
                    $"[{scope}_DISCONNECT_FAILED] ClientId={client.ClientId} Stage=ASYNC_TEARDOWN",
                    ex);
            }
        }

        private async Task RunDisconnectCleanupStepAsync(
            INetworkClient client,
            string stage,
            Func<Task> cleanup)
        {
            try
            {
                await cleanup().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string scope = IsWorldServer ? "WORLD" : "SESSION";
                Logger.Error(
                    $"[{scope}_DISCONNECT_FAILED] ClientId={client.ClientId} Stage={stage}",
                    ex);
            }
        }

        private void RunDisconnectCleanupStep(
            INetworkClient client,
            string stage,
            Action cleanup)
        {
            try
            {
                cleanup();
            }
            catch (Exception ex)
            {
                string scope = IsWorldServer ? "WORLD" : "SESSION";
                Logger.Error(
                    $"[{scope}_DISCONNECT_FAILED] ClientId={client.ClientId} Stage={stage}",
                    ex);
            }
        }

        #endregion
    }
}
