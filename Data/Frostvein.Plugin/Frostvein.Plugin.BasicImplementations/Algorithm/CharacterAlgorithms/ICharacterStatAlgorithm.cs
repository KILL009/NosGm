using Frostvein.Domain;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms
{
    public interface ICharacterStatAlgorithm
    {
        void Initialize();

        int GetStat(ClassType type, byte level);
    }
}