using Frostvein.Domain;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Magical
{
    public class MagicDefenceAlgorithm : ICharacterStatAlgorithm
    {
        private const int MAX_LEVEL = 256;
        private int[,] _stats;

        public void Initialize()
        {
            _stats = new int[(int)ClassType.Unknown, MAX_LEVEL];

            var adventurerDefence = 4;
            var swordmanDefence = 4;
            var archerDefence = 4;
            var mageDefence = 4;
            var fighterDefence = 4;

            for (var i = 0; i < MAX_LEVEL; i++)
            {
                adventurerDefence += i % 2 == 0 ? 1 : 0;
                _stats[(byte)ClassType.Adventurer, i] = adventurerDefence;

                swordmanDefence += (i % 2 == 0) ? 1 : 0;
                _stats[(byte)ClassType.Swordsman, i] = swordmanDefence;

                bool plus = i > 10 && i < 20 || i > 30 && i < 40 || i > 50 && i < 60 || i > 70 && i < 80 || i > 90 && i < 99;
                archerDefence += plus ? ((i + 1) % 2 == 0 ? 1 : 0) : i % 2 == 0 ? 1 : 0;
                _stats[(byte)ClassType.Archer, i] = archerDefence;

                mageDefence += (i % 2 == 0 || (i - 3) % 10 == 0 || (i - 5) % 5 == 0 || (i - 7) % 10 == 0 || (i - 9) % 10 == 0) ? 1 : 0;
                _stats[(byte)ClassType.Magician, i] = mageDefence;

                fighterDefence += ((i - 2) % 10 == 0 || (i - 4) % 10 == 0 || (i - 5) % 5 == 0 || (i - 7) % 10 == 0 || (i - 9) % 10 == 0) ? 1 : 0;
                _stats[(byte)ClassType.MartialArtist, i] = fighterDefence;
            }
        }

        public int GetStat(ClassType type, byte level) => _stats[(int)type, level - 1 > 0 ? level - 1 : 0];
    }
}