using Grpc.Core;
using NosGm.Cluster.Contracts.Communication.V1;
using NosGm.Communication.Client;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Domain;
using NosGm.Master.Library.Client;
using NosGm.Master.Library.Data;
using NosGm.Master.Library.Interface;
using System;
using System.Linq;
using System.Threading;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Master.Server
{
    internal sealed class MirroredCommunicationService
        : CommunicationService,
          ICommunicationService
    {
        public new bool ConnectCharacter(Guid worldId, long characterId)
        {
            if (!IsCurrentClientAuthenticated())
            {
                return false;
            }

            // The legacy SCS service used to fan CharacterConnected back to the
            // World that originated this mutation. Every selected ClientSession
            // in that World subscribes to the callback and then ignores its own
            // character, so the echo only creates O(local players) work for each
            // login. Keep the legacy callback for the other Worlds in the group,
            // where cross-channel presence information is actually useful.
            long accountId = DAOFactory.CharacterDAO.LoadById(characterId)?.AccountId ?? 0;
            AccountConnection account = MSManager.Instance.ConnectedAccounts.Find(
                candidate =>
                    candidate.AccountId == accountId &&
                    candidate.ConnectedWorld?.Id == worldId);
            if (account == null)
            {
                return false;
            }

            account.CharacterId = characterId;
            string worldGroup = account.ConnectedWorld?.WorldGroup;
            BroadcastLegacyCharacterPresence(
                worldId,
                worldGroup,
                characterId,
                true);
            MirrorPresence(worldGroup, characterId, true);
            return true;
        }

        public new void DisconnectCharacter(Guid worldId, long characterId)
        {
            if (!IsCurrentClientAuthenticated())
            {
                return;
            }

            AccountConnection account = FindConnectedCharacter(
                worldId,
                characterId);
            if (account == null)
            {
                return;
            }

            string worldGroup = account.ConnectedWorld?.WorldGroup;

            // Do not synchronously call CharacterDisconnected back into the
            // origin World while that same World is blocked waiting for this SCS
            // request/reply. Under mass disconnect this circular path can saturate
            // RequestReplyMessenger and time out hundreds of session teardowns.
            BroadcastLegacyCharacterPresence(
                worldId,
                worldGroup,
                characterId,
                false);

            if (!account.CanLoginCrossServer)
            {
                account.CharacterId = 0;
                account.ConnectedWorld = null;
            }

            MirrorPresence(worldGroup, characterId, false);
        }

        public new void KickSession(long? accountId, int? sessionId)
        {
            base.KickSession(accountId, sessionId);
            Mirror(
                "KickSession",
                () => MasterCommunicationCallbackMirror.Instance
                    .TryKickSession(accountId, sessionId));
        }

        public new void RefreshPenalty(int penaltyId)
        {
            if (!IsCurrentClientAuthenticated())
            {
                return;
            }

            MasterPenaltyRefreshGrpcAuthority.Instance.Publish(penaltyId);
        }

        public new void Restart(string worldGroup, int time = 5)
        {
            base.Restart(worldGroup, time);
            Mirror(
                "Restart",
                () => MasterCommunicationCallbackMirror.Instance
                    .TryRestart(worldGroup, time));
        }

        public new void RunGlobalEvent(EventType eventType, byte value)
        {
            base.RunGlobalEvent(eventType, value);
            Mirror(
                "RunGlobalEvent",
                () => MasterCommunicationCallbackMirror.Instance
                    .TryGlobalEvent(eventType, value));
        }

        public new void Shutdown(string worldGroup)
        {
            base.Shutdown(worldGroup);
            Mirror(
                "Shutdown",
                () => MasterCommunicationCallbackMirror.Instance
                    .TryShutdown(worldGroup));
        }

        public new void UpdateBazaar(string worldGroup, long bazaarItemId)
        {
            base.UpdateBazaar(worldGroup, bazaarItemId);
            Mirror(
                "UpdateBazaar",
                () => MasterCommunicationCallbackMirror.Instance
                    .TryBazaarRefresh(worldGroup, bazaarItemId));
        }

        public new void UpdateFamily(
            string worldGroup,
            long familyId,
            bool changeFaction)
        {
            base.UpdateFamily(worldGroup, familyId, changeFaction);
            Mirror(
                "UpdateFamily",
                () => MasterCommunicationCallbackMirror.Instance
                    .TryFamilyRefresh(
                        worldGroup,
                        familyId,
                        changeFaction));
        }

        public new void UpdateRelation(string worldGroup, long relationId)
        {
            base.UpdateRelation(worldGroup, relationId);
            Mirror(
                "UpdateRelation",
                () => MasterCommunicationCallbackMirror.Instance
                    .TryRelationRefresh(worldGroup, relationId));
        }

        private bool IsCurrentClientAuthenticated()
        {
            return MSManager.Instance.AuthentificatedClients.Any(
                clientId => clientId.Equals(CurrentClient.ClientId));
        }

        private static AccountConnection FindConnectedCharacter(
            Guid worldId,
            long characterId)
        {
            return MSManager.Instance.ConnectedAccounts.Find(
                account =>
                    account.CharacterId == characterId &&
                    account.ConnectedWorld?.Id == worldId);
        }

        private static void BroadcastLegacyCharacterPresence(
            Guid sourceWorldId,
            string worldGroup,
            long characterId,
            bool connected)
        {
            if (string.IsNullOrWhiteSpace(worldGroup))
            {
                return;
            }

            string operation = connected
                ? "CharacterConnected"
                : "CharacterDisconnected";

            foreach (SerializableWorldServer world in MSManager.Instance.WorldServers
                         .Where(candidate =>
                             candidate.Id != sourceWorldId &&
                             string.Equals(
                                 candidate.WorldGroup,
                                 worldGroup,
                                 StringComparison.Ordinal)))
            {
                try
                {
                    ICommunicationClient callback = world.CommunicationServiceClient
                        .GetClientProxy<ICommunicationClient>();
                    if (connected)
                    {
                        callback.CharacterConnected(characterId);
                    }
                    else
                    {
                        callback.CharacterDisconnected(characterId);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        "[LEGACY_PRESENCE_CALLBACK_ISOLATED_FAILURE] Operation=" +
                        operation +
                        " SourceWorldId=" + sourceWorldId.ToString("D") +
                        " TargetWorldId=" + world.Id.ToString("D") +
                        " CharacterId=" + characterId,
                        ex);
                }
            }
        }

        private static void MirrorPresence(
            string worldGroup,
            long characterId,
            bool connected)
        {
            string operation = connected
                ? "CharacterConnected"
                : "CharacterDisconnected";
            if (string.IsNullOrWhiteSpace(worldGroup))
            {
                Logger.Warn(
                    "[CALLBACK_MIRROR_DROPPED] Operation=" + operation +
                    " Reason=WORLD_GROUP_NOT_FOUND");
                return;
            }

            Mirror(
                operation,
                () => MasterCommunicationCallbackMirror.Instance
                    .TryCharacterPresence(
                        worldGroup,
                        characterId,
                        connected));
        }

        private static void Mirror(string operation, Func<bool> enqueue)
        {
            try
            {
                enqueue();
            }
            catch (Exception ex)
            {
                Logger.Error(
                    "[CALLBACK_MIRROR_ISOLATED_FAILURE] Operation=" + operation +
                    " SCS remains authoritative.",
                    ex);
            }
        }
    }

    internal sealed class MasterPenaltyRefreshGrpcAuthority
    {
        private static readonly Lazy<MasterPenaltyRefreshGrpcAuthority>
            LazyInstance =
                new Lazy<MasterPenaltyRefreshGrpcAuthority>(
                    () => new MasterPenaltyRefreshGrpcAuthority());

        private const int MaximumAttempts = 3;
        private const int InitialRetryDelayMilliseconds = 100;

        private MasterPenaltyRefreshGrpcAuthority()
        {
        }

        public static MasterPenaltyRefreshGrpcAuthority Instance =>
            LazyInstance.Value;

        public ulong Publish(int penaltyLogId)
        {
            if (penaltyLogId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(penaltyLogId),
                    "PenaltyRefresh requires a positive penalty log ID.");
            }

            var options = MasterCommunicationGrpcIdentityOptions.Load();
            string eventId = Guid.NewGuid().ToString("D");
            var template = new WireV1.PublishCommunicationCallbackRequest
            {
                EventId = eventId,
                TtlSeconds =
                    CommunicationCallbackContractLimits.DefaultEventTtlSeconds,
                Target = new WireV1.CommunicationCallbackTarget
                {
                    Kind = WireV1.CommunicationCallbackTargetKind.AllNodes
                },
                PenaltyRefresh = new WireV1.PenaltyRefreshCallback
                {
                    PenaltyLogId = penaltyLogId
                }
            };

            using (var publisher =
                   new GrpcCommunicationCallbackPublisher(options))
            using (var timeout =
                   new CancellationTokenSource(
                       TimeSpan.FromMilliseconds(
                           options.DeadlineMilliseconds)))
            {
                int retryDelayMilliseconds =
                    InitialRetryDelayMilliseconds;
                Exception lastException = null;

                for (int attempt = 1; attempt <= MaximumAttempts; attempt++)
                {
                    timeout.Token.ThrowIfCancellationRequested();
                    try
                    {
                        WireV1.PublishCommunicationCallbackResponse response =
                            publisher.PublishAsync(
                                    template,
                                    timeout.Token)
                                .ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();
                        if (response.Result ==
                                WireV1.CommunicationResultCode.Success &&
                            response.AcceptedSequence > 0)
                        {
                            Logger.Info(
                                "[CALLBACK_PENALTY_GRPC_ACCEPTED] PenaltyLogId=" +
                                penaltyLogId +
                                " Sequence=" +
                                response.AcceptedSequence +
                                " MatchedSubscribers=" +
                                response.MatchedSubscribers);
                            return response.AcceptedSequence;
                        }

                        if (response.Result !=
                                WireV1.CommunicationResultCode.Unavailable &&
                            response.Result !=
                                WireV1.CommunicationResultCode.CapacityExceeded)
                        {
                            throw new InvalidOperationException(
                                "Authoritative PenaltyRefresh publication failed with " +
                                response.Result + ".");
                        }

                        lastException = new InvalidOperationException(
                            "Authoritative PenaltyRefresh publication returned " +
                            response.Result + ".");
                    }
                    catch (RpcException exception)
                        when (exception.StatusCode == StatusCode.Unavailable ||
                              exception.StatusCode == StatusCode.DeadlineExceeded)
                    {
                        lastException = exception;
                    }

                    if (attempt == MaximumAttempts)
                    {
                        break;
                    }

                    if (timeout.Token.WaitHandle.WaitOne(
                            retryDelayMilliseconds))
                    {
                        timeout.Token.ThrowIfCancellationRequested();
                    }
                    retryDelayMilliseconds = checked(
                        retryDelayMilliseconds * 2);
                }

                throw new InvalidOperationException(
                    "Authoritative PenaltyRefresh gRPC publication failed closed; no SCS callback was attempted.",
                    lastException);
            }
        }
    }
}
