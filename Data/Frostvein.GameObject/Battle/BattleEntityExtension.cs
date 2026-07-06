using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using System;

namespace Frostvein.GameObject.Battle
{
    public static class BattleEntityExtension
    {
        public static int ReflectDamage(this BattleEntity attacker, BattleEntity target, int damage, bool isPrimary)
        {
            int data;
            int reflectedDamage;

            data = isPrimary
                ? target.GetBuff(BCardType.CardType.TauntSkill,
                    (byte)AdditionalTypes.TauntSkill.ReflectMaximumDamageFrom)[0]
                : target.GetBuff(BCardType.CardType.TauntSkill,
                    (byte)AdditionalTypes.TauntSkill.ReflectsMaximumDamageFromNegated, addValue: 1)[0];

            if (data < 0) return 0;

            reflectedDamage = Math.Min(damage, data);
            attacker.GetDamage(reflectedDamage, target, true);
            target.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(attacker.UserType, attacker.MapEntityId,
                (byte)attacker.UserType, attacker.MapEntityId,
                -1, 0, 0, 0, 0, 0, attacker.Hp > 0, (int)(attacker.Hp / attacker.HPLoad() * 100), reflectedDamage,
                0, 1));
            target.Character?.Session?.SendPacket(target.Character.GenerateStat());

            return damage -= reflectedDamage;
        }
    }
}