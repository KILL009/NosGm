using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.Algorithm
{
    public class SecondaryHitRateService : ISecondaryHitRateService
    {
        private readonly long[,] _secondaryHitRate = new long[Constants.ClassCount, Constants.MaxLevel];

        public SecondaryHitRateService()
        {
            var adventurerHit = 18;
            var adventurerHitUp = 2;
            var fighterHit = 16;
            var mageHit = 16;
            var archerHit = 23;
            var swordmanHit = 16;
            for (var i = 0; i < Constants.MaxLevel; i++)
            {
                adventurerHit += adventurerHitUp;
                _secondaryHitRate[(byte)ClassType.Adventurer, i] = adventurerHit;

                swordmanHit += (i - 5) % 5 == 0 ? 4 : 2;
                _secondaryHitRate[(byte)ClassType.Swordsman, i] = swordmanHit;

                archerHit += i != 0 && ((i - 1) % 10 == 0 || (i - 3) % 10 == 0 || (i - 5) % 10 == 0 || (i - 8) % 10 == 0) ? 1 : 2;
                _secondaryHitRate[(byte)ClassType.Archer, i] = archerHit;

                mageHit += (i - 5) % 5 == 0 ? 4 : 2;
                _secondaryHitRate[(byte)ClassType.Magician, i] = mageHit;

                fighterHit += (i - 4) % 4 == 0 || (i - 10) % 10 == 0 ? 4 : 2;
                _secondaryHitRate[(byte)ClassType.MartialArtist, i] = fighterHit;
            }
        }

        public long GetSecondaryHitRate(ClassType @class, byte level)
        {
            return (long)_secondaryHitRate![(byte)@class, level - 1];
        }
    }
}
