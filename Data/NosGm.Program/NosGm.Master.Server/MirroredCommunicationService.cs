using NosGm.Domain;
using NosGm.Master.Library.Interface;
using System;

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
                string worldGroup = MSManager.Instance.WorldServers
                    .Find(world => world.Id == worldId)
                    ?.WorldGroup;
                MasterCommunicationCallbackMirror.Instance
                    .TryCharacterPresence(
                        worldGroup,
                        characterId,
                        true);
            }
            return connected;
        }

        public new void DisconnectCharacter(Guid worldId, long characterId)
        {
            string worldGroup = MSManager.Instance.WorldServers
                .Find(world => world.Id == worldId)
                ?.WorldGroup;
            base.DisconnectCharacter(worldId, characterId);
            MasterCommunicationCallbackMirror.Instance
                .TryCharacterPresence(
                    worldGroup,
                    characterId,
                    false);
        }

        public new void KickSession(long? accountId, int? sessionId)
        {
            base.KickSession(accountId, sessionId);
            MasterCommunicationCallbackMirror.Instance
                .TryKickSession(accountId, sessionId);
        }

        public new void RefreshPenalty(int penaltyId)
        {
            base.RefreshPenalty(penaltyId);
            MasterCommunicationCallbackMirror.Instance
                .TryPenaltyRefresh(penaltyId);
        }

        public new void Restart(string worldGroup, int time = 5)
        {
            base.Restart(worldGroup, time);
            MasterCommunicationCallbackMirror.Instance
                .TryRestart(worldGroup, time);
        }

        public new void RunGlobalEvent(EventType eventType, byte value)
        {
            base.RunGlobalEvent(eventType, value);
            MasterCommunicationCallbackMirror.Instance
                .TryGlobalEvent(eventType, value);
        }

        public new void Shutdown(string worldGroup)
        {
            base.Shutdown(worldGroup);
            MasterCommunicationCallbackMirror.Instance
                .TryShutdown(worldGroup);
        }

        public new void UpdateBazaar(string worldGroup, long bazaarItemId)
        {
            base.UpdateBazaar(worldGroup, bazaarItemId);
            MasterCommunicationCallbackMirror.Instance
                .TryBazaarRefresh(worldGroup, bazaarItemId);
        }

        public new void UpdateFamily(
            string worldGroup,
            long familyId,
            bool changeFaction)
        {
            base.UpdateFamily(worldGroup, familyId, changeFaction);
            MasterCommunicationCallbackMirror.Instance
                .TryFamilyRefresh(
                    worldGroup,
                    familyId,
                    changeFaction);
        }

        public new void UpdateRelation(string worldGroup, long relationId)
        {
            base.UpdateRelation(worldGroup, relationId);
            MasterCommunicationCallbackMirror.Instance
                .TryRelationRefresh(worldGroup, relationId);
        }
    }
}
