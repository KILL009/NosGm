using Frostvein.Domain;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Close
{
    public class CloseDodgeAlgorithm : ICharacterStatAlgorithm
    {
        private const int MAX_LEVEL = 256;
        private int[,] _stats;

        public void Initialize()
        {
            _stats = new int[(int)ClassType.Unknown, MAX_LEVEL];

            var swordmanDodge = 8;
            var archerDodge = 18;
            var mageDodge = 18;
            var fighterDodge = 28;

            for (var i = 0; i < MAX_LEVEL; i++)
            {
                _stats[(byte)ClassType.Adventurer, i] = i + 10;

                swordmanDodge += (i - 5) % 5 == 0 ? 2 : 1;
                _stats[(byte)ClassType.Swordsman, i] = swordmanDodge;

                archerDodge += ((i - 2) % 10 == 0 || (i - 4) % 10 == 0 || (i - 5) % 5 == 0 || (i - 7) % 10 == 0 || (i - 9) % 10 == 0 || (i - 10) % 10 == 0) ? 2 : 1;
                _stats[(byte)ClassType.Archer, i] = archerDodge;

                mageDodge += (i - 5) % 5 == 0 ? 2 : 1;
                _stats[(byte)ClassType.Magician, i] = mageDodge;

                fighterDodge += ((i - 4) % 10 == 0 || (i - 7) % 10 == 0 || (i - 10) % 10 == 0) ? 2 : 1;
                _stats[(byte)ClassType.MartialArtist, i] = fighterDodge;
            }
        }

        public int GetStat(ClassType type, byte level) => _stats[(int)type, level - 1 > 0 ? level - 1 : 0];
    }
}