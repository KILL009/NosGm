using NosGm.Domain;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Damage
{
    public class CriticalHitAlgorithm : ICharacterStatAlgorithm
    {
        private const int MAX_LEVEL = 256;
        private int[,] _criticalHit;

        public void Initialize()
        {
            _criticalHit = new int[(int)ClassType.Unknown, MAX_LEVEL];

            for (var i = 0; i < MAX_LEVEL; i++)
            {
                _criticalHit[(int)ClassType.Adventurer, i] = 0; // sure
                _criticalHit[(int)ClassType.Swordsman, i] = 0; // approx
                _criticalHit[(int)ClassType.Magician, i] = 0; // sure
                _criticalHit[(int)ClassType.Archer, i] = 0; // sure
                _criticalHit[(int)ClassType.MartialArtist, i] = 0; // sure
            }
        }

        public int GetStat(ClassType type, byte level) => _criticalHit[(int)type, level - 1 > 0 ? level - 1 : 0];
    }
}