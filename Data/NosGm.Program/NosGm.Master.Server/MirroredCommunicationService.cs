using NosGm.Core;
using NosGm.Domain;
using NosGm.Master.Library.Data;
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
            bool connected = base.ConnectCharacter(worldId, characterId);
            if (connected)
            {
                AccountConnection account = FindConnectedCharacter(
                    worldId,
                    characterId);
                MirrorPresence(
                    account?.ConnectedWorld?.WorldGroup,
                    characterId,
                    true);
            }
            return connected;
        }

        public new void DisconnectCharacter(Guid worldId, long characterId)
        {
            bool authenticated = IsCurrentClientAuthenticated();
            AccountConnection account = authenticated
                ? FindConnectedCharacter(worldId, characterId)
                : null;
            string worldGroup = account?.ConnectedWorld?.WorldGroup;

            base.DisconnectCharacter(worldId, characterId);
            if (authenticated && account != null)
            {
                MirrorPresence(worldGroup, characterId, false);
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
}
