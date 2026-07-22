using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.Algorithm
{
    public class DistanceDefenceService : IDistanceDefenceService
    {
        private readonly long[,] _distanceDefence = new long[Constants.ClassCount, Constants.MaxLevel];

        public DistanceDefenceService()
        {
            var adventurerDefence = 4;
            var swordmanDefence = 4;
            var archerDefence = 4;
            var mageDefence = 24;
            var fighterDefence = 14;
            for (var i = 0; i < Constants.MaxLevel; i++)
            {
                adventurerDefence += i % 2 == 0 ? 1 : 0;
                _distanceDefence[(byte)ClassType.Adventurer, i] = adventurerDefence;

                swordmanDefence += i == 0 || (i - 2) % 10 == 0 || (i - 4) % 10 == 0 || (i - 5) % 5 == 0 || (i - 7) % 10 == 0 || (i - 9) % 10 == 0 ? 1 : 0;
                _distanceDefence[(byte)ClassType.Swordsman, i] = swordmanDefence;

                archerDefence += i == 0 || (i - 2) % 10 == 0 || (i - 3) % 10 == 0 || (i - 4) % 10 == 0 || (i - 5) % 5 == 0 || (i - 7) % 10 == 0 || (i - 8) % 10 == 0 || (i - 9) % 10 == 0 ? 1 : 0;
                _distanceDefence[(byte)ClassType.Archer, i] = archerDefence;

                mageDefence += i == 0 || (i - 2) % 10 == 0 || (i - 4) % 10 == 0 || (i - 5) % 5 == 0 || (i - 7) % 10 == 0 || (i - 9) % 10 == 0 ? 1 : 0;
                _distanceDefence[(byte)ClassType.Magician, i] = mageDefence;

                fighterDefence += i > 0 && ((i - 1) % 20 == 0 || (i - 3) % 20 == 0 || (i - 6) % 20 == 0 || (i - 9) % 20 == 0 || (i - 12) % 20 == 0 || (i - 15) % 20 == 0 || (i - 18) % 20 == 0) ? 0 : 1;
                _distanceDefence[(byte)ClassType.MartialArtist, i] = fighterDefence;
            }
        }

        public long GetDistanceDefence(ClassType @class, byte level)
        {
            return _distanceDefence![(byte)@class, level - 1];
        }
    }
}
