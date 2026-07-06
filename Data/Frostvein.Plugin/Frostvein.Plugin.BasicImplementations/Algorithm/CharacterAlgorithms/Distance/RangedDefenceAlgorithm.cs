using Frostvein.Domain;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Distance
{
    public class RangedDefenceAlgorithm : ICharacterStatAlgorithm
    {
        private const int MAX_LEVEL = 256;
        private int[,] _stats;

        public void Initialize()
        {
            _stats = new int[(int)ClassType.Unknown, MAX_LEVEL];

            var adventurerDefence = 4;
            var swordmanDefence = 4;
            var archerDefence = 4;
            var mageDefence = 24;
            var fighterDefence = 14;

            for (var i = 0; i < MAX_LEVEL; i++)
            {
                adventurerDefence += i % 2 == 0 ? 1 : 0;
                _stats[(byte)ClassType.Adventurer, i] = adventurerDefence;

                swordmanDefence += i == 0 || (i - 2) % 10 == 0 || (i - 4) % 10 == 0 || (i - 5) % 5 == 0 || (i - 7) % 10 == 0 || (i - 9) % 10 == 0 ? 1 : 0;
                _stats[(byte)ClassType.Swordsman, i] = swordmanDefence;

                archerDefence += i == 0 || (i - 2) % 10 == 0 || (i - 3) % 10 == 0 || (i - 4) % 10 == 0 || (i - 5) % 5 == 0 || (i - 7) % 10 == 0 || (i - 8) % 10 == 0 || (i - 9) % 10 == 0 ? 1 : 0;
                _stats[(byte)ClassType.Archer, i] = archerDefence;

                mageDefence += i == 0 || (i - 2) % 10 == 0 || (i - 4) % 10 == 0 || (i - 5) % 5 == 0 || (i - 7) % 10 == 0 || (i - 9) % 10 == 0 ? 1 : 0;
                _stats[(byte)ClassType.Magician, i] = mageDefence;

                fighterDefence += i > 0 && ((i - 1) % 20 == 0 || (i - 3) % 20 == 0 || (i - 6) % 20 == 0 || (i - 9) % 20 == 0 || (i - 12) % 20 == 0 || (i - 15) % 20 == 0 || (i - 18) % 20 == 0) ? 0 : 1;
                _stats[(byte)ClassType.MartialArtist, i] = fighterDefence;
            }
        }

        public int GetStat(ClassType type, byte level) => _stats[(int)type, level - 1 > 0 ? level - 1 : 0];
    }
}