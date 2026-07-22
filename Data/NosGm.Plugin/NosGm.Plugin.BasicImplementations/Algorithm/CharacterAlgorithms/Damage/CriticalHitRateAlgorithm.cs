using NosGm.Domain;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Damage
{
    public class CriticalHitRateAlgorithm : ICharacterStatAlgorithm
    {
        private const int MAX_LEVEL = 256;
        private int[,] _criticalHitRate;

        public void Initialize()
        {
            _criticalHitRate = new int[(int)ClassType.Unknown, MAX_LEVEL];

            for (var i = 0; i < MAX_LEVEL; i++)
            {
                _criticalHitRate[(int)ClassType.Adventurer, i] = 0; // sure
                _criticalHitRate[(int)ClassType.Swordsman, i] = 0; // approx
                _criticalHitRate[(int)ClassType.Magician, i] = 0; // sure
                _criticalHitRate[(int)ClassType.Archer, i] = 0; // sure
                _criticalHitRate[(int)ClassType.MartialArtist, i] = 0; // sure
            }
        }

        public int GetStat(ClassType type, byte level) => _criticalHitRate[(int)type, level - 1 > 0 ? level - 1 : 0];
    }
}