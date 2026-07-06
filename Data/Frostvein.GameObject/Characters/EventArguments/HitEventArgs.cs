using Frostvein.Domain;
using System;

namespace Frostvein.GameObject.EventArguments
{
    public class HitEventArgs : EventArgs
    {
        public HitEventArgs(UserType type, object senderEntity, Skill skill, int damage, object targetEntity = null, long targetId = -1, TargetHitType targetHitType = TargetHitType.SingleTargetHit)
        {
            UserType = type;
            SenderEntity = senderEntity;
            Damage = damage;
            Skill = skill;
            TargetEntity = targetEntity;
            TargetId = targetId;
            TargetHitType = targetHitType;
        }

        public UserType UserType { get; }

        public object SenderEntity { get; }

        public Skill Skill { get; }

        public int Damage { get; }

        public object TargetEntity { get; }

        public long TargetId { get; }

        public TargetHitType TargetHitType { get; }
    }
}
