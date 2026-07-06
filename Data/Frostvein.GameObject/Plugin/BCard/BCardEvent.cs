using Frostvein.GameObject;
using Frostvein.GameObject.Battle;

namespace Game.Configuration.BCards
{
    public class BCardEvent
    {
        #region Properties

        public BCard BCard { get; set; }

        public Card? Card { get; set; }

        public int LevelUpgraded { get; set; }

        public BattleEntity Target { get; set; }

        public BattleEntity Caster { get; set; }

        public Skill? Skill { get; set; }

        public short X { get; set; }

        public short Y { get; set; }

        public int FirstData { get; set; }

        public int CasterLevel { get; set; }

        public int DelayTime { get; set; }

        public int Duration { get; set; }

        #endregion Properties
    }
}