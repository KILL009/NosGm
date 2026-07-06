using Frostvein.Domain;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Damage
{
    public class CriticalDistAlgorithm : ICharacterStatAlgorithm
    {
        private const int MAX_LEVEL = 256;
        private int[,] _criticalDist;

        public void Initialize()
        {
            _criticalDist = new int[(int)ClassType.Unknown, MAX_LEVEL];

            for (var i = 0; i < MAX_LEVEL; i++)
            {
                _criticalDist[(int)ClassType.Adventurer, i] = 0; // sure
                _criticalDist[(int)ClassType.Swordsman, i] = 0; // approx
                _criticalDist[(int)ClassType.Magician, i] = 0; // sure
                _criticalDist[(int)ClassType.Archer, i] = 0; // sure
                _criticalDist[(int)ClassType.MartialArtist, i] = 0; // sure
            }
        }

        public int GetStat(ClassType type, byte level) => _criticalDist[(int)type, level - 1 > 0 ? level - 1 : 0];
    }
}