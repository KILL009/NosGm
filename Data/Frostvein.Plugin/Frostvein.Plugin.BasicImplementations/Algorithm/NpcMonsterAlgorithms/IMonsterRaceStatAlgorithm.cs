using Frostvein.Domain;

namespace Plugins.BasicImplementations.Algorithm.NpcMonsterAlgorithms
{
    public interface IMonsterRaceStatAlgorithm
    {
        void Initialize();

        int GetStat(NpcMonsterRaceType type, byte level, bool isMonster);
    }
}