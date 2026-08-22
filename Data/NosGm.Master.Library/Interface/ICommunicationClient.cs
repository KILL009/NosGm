using NosGm.Domain;
using NosGm.Master.Library.Data;
using System;

namespace NosGm.Master.Library.Interface
{
    public interface ICommunicationClient
    {
        #region Methods

        void CharacterConnected(long characterId);

        void CharacterDisconnected(long characterId);

        void KickSession(long? accountId, int? sessionId);

        void Restart(int time = 5);

        void RunGlobalEvent(EventType eventType, byte value);

        void SendMessageToCharacter(SCSCharacterMessage message);

        void Shutdown();

        void UpdateBazaar(long bazaarItemId);

        void UpdateFamily(long familyId, bool changeFaction);

        void UpdateStaticBonus(long characterId);

        #endregion
    }

    public static class RetiredCommunicationClientPenaltyExtensions
    {
        public static void UpdatePenaltyLog(
            this ICommunicationClient client,
            int penaltyLogId)
        {
            throw new NotSupportedException(
                "UpdatePenaltyLog was retired from the SCS callback contract. " +
                "PenaltyRefresh is gRPC-authoritative and has no SCS fallback.");
        }
    }

    public static class RetiredCommunicationClientRelationExtensions
    {
        public static void UpdateRelation(
            this ICommunicationClient client,
            long relationId)
        {
            throw new NotSupportedException(
                "UpdateRelation was retired from the SCS callback contract. " +
                "RelationRefresh is gRPC-authoritative and has no SCS fallback.");
        }
    }
}
