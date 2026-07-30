using NosGm.Communication.Client;
using NosGm.Core;
using NosGm.Domain;
using NosGm.Master.Library.Interface;
using System;
using System.Linq;

namespace NosGm.Master.Server
{
    internal sealed class MirroredCommunicationService
        : CommunicationService,
          ICommunicationService
    {
        public new bool ConnectCharacter(Guid worldId, long characterId)
        {
            Guid[] peerWorldIds = ResolvePeerWorldIds(worldId);
            bool connected = base.ConnectCharacter(worldId, characterId);
            if (connected)
            {
                MirrorPresence(peerWorldIds, characterId, true);
            }
            return connected;
        }

        public new void DisconnectCharacter(Guid worldId, long characterId)
        {
            bool wasConnected = MSManager.Instance.ConnectedAccounts.Any(
                account =>
                    account.CharacterId == characterId &&
                    account.ConnectedWorld?.Id == worldId);
            Guid[] peerWorldIds = wasConnected
                ? ResolvePeerWorldIds(worldId)
                : Array.Empty<Guid>();

            base.DisconnectCharacter(worldId, characterId);
            if (wasConnected)
            {
                MirrorPresence(peerWorldIds, characterId, false);
            }
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
            base.RefreshPenalty(penaltyId);
            Mirror(
                "UpdatePenaltyLog",
                () => MasterCommunicationCallbackMirror.Instance
                    .TryPenaltyRefresh(penaltyId));
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

        private static Guid[] ResolvePeerWorldIds(Guid sourceWorldId)
        {
            return CharacterPresenceMirrorRoutePlanner.ResolvePeerWorldIds(
                    MSManager.Instance.WorldServers.Select(
                        world => new CommunicationCallbackWorldRoute
                        {
                            WorldId = world.Id,
                            WorldGroup = world.WorldGroup
                        }),
                    sourceWorldId)
                .ToArray();
        }

        private static void MirrorPresence(
            Guid[] peerWorldIds,
            long characterId,
            bool connected)
        {
            foreach (Guid targetWorldId in peerWorldIds)
            {
                Mirror(
                    connected
                        ? "CharacterConnected"
                        : "CharacterDisconnected",
                    () => MasterCommunicationCallbackMirror.Instance
                        .TryCharacterPresence(
                            targetWorldId,
                            characterId,
                            connected));
            }
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
}
