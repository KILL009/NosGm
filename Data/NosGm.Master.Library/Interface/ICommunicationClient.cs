using NosGm.Domain;
using NosGm.Master.Library.Data;

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

        void UpdatePenaltyLog(int penaltyLogId);

        void UpdateRelation(long relationId);

        void UpdateStaticBonus(long characterId);

        #endregion
    }
}