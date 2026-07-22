using NosGm.Domain;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Magical
{
    public class MagicDodgeAlgorithm : ICharacterStatAlgorithm
    {
        public void Initialize()
        {
            // no dodge possible without shells effects
        }

        public int GetStat(ClassType type, byte level) => 0;
    }
}