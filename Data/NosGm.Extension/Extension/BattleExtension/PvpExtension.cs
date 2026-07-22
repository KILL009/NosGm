using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System.Linq;

namespace NosGm.Extension.Extension.BattleExtension
{
    public static class PvpExtension
    {
        public static bool CanAttackMate(Character attacker, Mate target)
        {
            if (attacker == null || target == null)
                return false;

            if (!attacker.MapInstance.IsPVP && !attacker.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.PVPMap) && !attacker.MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4))
                return false;

            if (attacker.MapInstance != target.Owner.MapInstance)
                return false;

            if (attacker.Hp <= 0 || target.Hp <= 0)
                return false;

            if (!attacker.CanFight)
                return false;

            if (attacker == target.Owner)
                return false;

            if (attacker.MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4))
                if (attacker.Faction == target.Owner.Faction)
                    return false;

            if (attacker.MapInstanceId == ServerManager.Instance.ArenaInstance.MapInstanceId)
                if (attacker.Group != null && target.Owner.Group != null && attacker.Group == target.Owner.Group)
                    return false;

            if (attacker.MapInstanceId == ServerManager.Instance.FamilyArenaInstance.MapInstanceId)
                if (attacker.Group != null && target.Owner.Group != null && attacker.Group == target.Owner.Group || attacker.Family != null && target.Owner.Family != null && attacker.Family == target.Owner.Family)
                    return false;

            if (attacker.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.PVPMap) || attacker.MapInstance.IsPVP)
                if (attacker.Group != null && target.Owner.Group != null && attacker.Group == target.Owner.Group)
                    return false;

            bool isMuted = attacker.MuteMessage();

            if (isMuted)
                return false;

            return true;
        }

        public static bool CanAttackTarget(Character attacker, Character target)
        {
            if (attacker == null || target == null)
                return false;

            if (!attacker.MapInstance.IsPVP && !attacker.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.PVPMap) && !attacker.MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4))
                return false;

            if (attacker.MapInstance != target.MapInstance)
                return false;

            if (attacker.Hp <= 0 || target.Hp <= 0)
                return false;

            if (!attacker.CanFight)
                return false;

            if (attacker.MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4))
                if (attacker.Faction == target.Faction)
                    return false;

            if (attacker.MapInstanceId == ServerManager.Instance.ArenaInstance.MapInstanceId)
                if (attacker.Group != null && target.Group != null && attacker.Group == target.Group)
                    return false;

            if (attacker.MapInstanceId == ServerManager.Instance.FamilyArenaInstance.MapInstanceId)
                if (attacker.Group != null && target.Group != null && attacker.Group == target.Group || attacker.Family != null && target.Family != null && attacker.Family == target.Family)
                    return false;

            if (attacker.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.PVPMap) || attacker.MapInstance.IsPVP)
                if (attacker.Group != null && target.Group != null && attacker.Group == target.Group)
                    return false;

            bool isMuted = attacker.MuteMessage();

            if (isMuted)
                return false;

            return true;
        }
    }
}
