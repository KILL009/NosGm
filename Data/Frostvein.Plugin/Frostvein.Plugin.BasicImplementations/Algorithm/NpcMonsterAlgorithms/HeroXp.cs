using Frostvein.Domain;

namespace Plugins.BasicImplementations.Algorithm.NpcMonsterAlgorithms
{
    public class HeroXp : IMonsterRaceStatAlgorithm
    {
        private const int MAX_LEVEL = 256;

        public void Initialize()
        {
        }

        public int GetStat(NpcMonsterRaceType type, byte level, bool isMonster) => 0;
    }
}