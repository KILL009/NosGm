using NosGm.Domain;
using System;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.HpMp
{
    public class MpRegenSitting : ICharacterStatAlgorithm
    {
        public void Initialize()
        {
        }

        public int GetStat(ClassType type, byte level)
        {
            switch (type)
            {
                case ClassType.Adventurer:
                    return 10;
                case ClassType.Swordsman:
                    return 30;
                case ClassType.Archer:
                    return 50;
                case ClassType.Magician:
                    return 80;
                case ClassType.MartialArtist:
                    return 80;
                case ClassType.Unknown:
                    return 50;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}