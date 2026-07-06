using Frostvein.Domain;
using System;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms
{
    public class SpeedAlgorithm : ICharacterStatAlgorithm
    {
        public void Initialize()
        {
        }

        public int GetStat(ClassType type, byte level)
        {
            switch (type)
            {
                case ClassType.Adventurer:
                    return 11;
                case ClassType.Swordsman:
                    return 11;
                case ClassType.Archer:
                    return 12;
                case ClassType.Magician:
                    return 10;
                case ClassType.MartialArtist:
                    return 11;
                case ClassType.Unknown:
                    return 11;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}