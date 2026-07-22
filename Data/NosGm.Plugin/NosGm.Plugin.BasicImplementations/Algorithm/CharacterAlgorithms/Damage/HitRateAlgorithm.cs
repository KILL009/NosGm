using NosGm.Domain;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Damage
{
    public class HitRateAlgorithm : ICharacterStatAlgorithm
    {
        private const int MAX_LEVEL = 256;
        private int[,] _hitRate;

        public void Initialize()
        {
            _hitRate = new int[(int)ClassType.Unknown, MAX_LEVEL];

            var archerHitRate = 31;
            var fighterHitRate = 8;
            var swordHitRate = 23;

            for (var i = 0; i < MAX_LEVEL; i++)
            {
                _hitRate[(byte)ClassType.Adventurer, i] = i + 10;

                swordHitRate += (i - 5) % 5 == 0 ? 2 : 1;
                _hitRate[(byte)ClassType.Swordsman, i] = swordHitRate;

                archerHitRate += i != 96 && i % 2 == 0 || i > 0 && i % 5 == 0 ? 4 : 2;
                _hitRate[(byte)ClassType.Archer, i] = archerHitRate;

                _hitRate[(byte)ClassType.Magician, i] = 0;

                fighterHitRate += i == 0 || (i - 4) % 10 == 0 || i > 0 && (i - 7) % 10 == 0 || i > 0 && (i - 10) % 10 == 0 ? 2 : 1;
                _hitRate[(byte)ClassType.MartialArtist, i] = fighterHitRate;
            }
        }

        public int GetStat(ClassType type, byte level) => _hitRate[(int)type, level - 1 > 0 ? level - 1 : 0];
    }
}