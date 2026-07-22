using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.Algorithm
{
    public class SecondaryDamageService : ISecondaryDamageService
    {
        private readonly long[,] _secondaryMinDamage = new long[Constants.ClassCount, Constants.MaxLevel];

        public SecondaryDamageService()
        {
            var fighterMin = 28;
            var mageMin = 8;
            var archerMin = 8;
            var swordmanMin = 8;
            for (var i = 0; i < Constants.MaxLevel; i++)
            {
                _secondaryMinDamage[(byte)ClassType.Adventurer, i] = i + 10;

                swordmanMin += i == 0 || (i - 5) % 10 == 0 || i % 10 == 0 ? 2 : 1;
                _secondaryMinDamage[(byte)ClassType.Swordsman, i] = swordmanMin;

                archerMin += i == 0 || (i - 4) % 10 == 0 || (i - 7) % 10 == 0 || i > 1 && (i - 1) % 10 == 0 ? 2 : 1;
                _secondaryMinDamage[(byte)ClassType.Archer, i] = archerMin;

                mageMin += i == 0 || (i - 5) % 10 == 0 || i % 10 == 0 ? 2 : 1;
                _secondaryMinDamage[(byte)ClassType.Magician, i] = mageMin;

                fighterMin += i == 0 || (i - 4) % 10 == 0 || (i - 7) % 10 == 0 || i > 1 && (i - 1) % 10 == 0 ? 2 : 1;
                _secondaryMinDamage[(byte)ClassType.MartialArtist, i] = fighterMin;
            }
        }

        public long GetSecondaryMinDamage(ClassType @class, byte level)
        {
            return (long)_secondaryMinDamage![(byte)@class, level - 1];
        }

        public long GetSecondaryMaxDamage(ClassType @class, byte level) => GetSecondaryMinDamage(@class, level);
    }
}
