using Frostvein.Domain;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Damage
{
    public class SecondaryDamageAlgorithm : ICharacterStatAlgorithm
    {
        private const int MAX_LEVEL = 256;
        private int[,] _maxDist;

        public void Initialize()
        {
            _maxDist = new int[(int)ClassType.Unknown, MAX_LEVEL];

            var fighterMin = 28;
            var mageMin = 8;
            var archerMin = 8;
            var swordmanMin = 8;

            for (var i = 0; i < MAX_LEVEL; i++)
            {
                _maxDist[(byte)ClassType.Adventurer, i] = i + 10;

                swordmanMin += i == 0 || (i - 5) % 10 == 0 || i % 10 == 0 ? 2 : 1;
                _maxDist[(byte)ClassType.Swordsman, i] = swordmanMin;

                archerMin += i == 0 || (i - 4) % 10 == 0 || (i - 7) % 10 == 0 || i > 1 && (i - 1) % 10 == 0 ? 2 : 1;
                _maxDist[(byte)ClassType.Archer, i] = archerMin;

                mageMin += i == 0 || (i - 5) % 10 == 0 || i % 10 == 0 ? 2 : 1;
                _maxDist[(byte)ClassType.Magician, i] = mageMin;

                fighterMin += i == 0 || (i - 4) % 10 == 0 || (i - 7) % 10 == 0 || i > 1 && (i - 1) % 10 == 0 ? 2 : 1;
                _maxDist[(byte)ClassType.MartialArtist, i] = fighterMin;
            }
        }

        public int GetStat(ClassType type, byte level) => _maxDist[(int)type, level - 1 > 0 ? level - 1 : 0];
    }
}