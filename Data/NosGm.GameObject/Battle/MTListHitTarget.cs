using NosGm.Domain;

namespace NosGm.GameObject.Battle
{
    public class MTListHitTarget
    {
        #region Instantiation

        public MTListHitTarget(UserType entityType, long targetId, TargetHitType targetHitType)
        {
            EntityType = entityType;
            TargetId = targetId;
            TargetHitType = targetHitType;
        }

        #endregion

        #region Properties

        public UserType EntityType { get; set; }

        public TargetHitType TargetHitType { get; set; }

        public long TargetId { get; set; }

        #endregion
    }
}