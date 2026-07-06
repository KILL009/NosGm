using Frostvein.GameObject;

namespace Frostvein.Extension.Extension.BattleExtension
{
    public static class PveExtension
    {
        public static bool CanAttack(Character character, MapMonster monster = null)
        {
            if (character == null)
                return false;

            if (character.MapInstance == null)
                return false;

            if (!character.CanFight)
                return false;

            if (character.Hp <= 0)
                return false;

            if (character.NoAttack)
                return false;

            if (character.IsVehicled)
                return false;

            bool isMuted = character.MuteMessage();

            if (isMuted)
                return false;

            if (character.InvisibleGm)
                return false;

            if (monster?.IsAlive == false)
                return false;

            return true;
        }
    }
}
