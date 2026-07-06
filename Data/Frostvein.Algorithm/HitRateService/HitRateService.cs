using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.Algorithm
{
    public class HitRateService : IHitRateService
    {
        private readonly long[,] _hitRate = new long[Constants.ClassCount, Constants.MaxLevel];
        public HitRateService()
        {
            var archerHitRate = 31;
            var fighterHitRate = 8;
            var swordHitRate = 23;
            for (var i = 0; i < Constants.MaxLevel; i++)
            {
                _hitRate[(byte)ClassType.Adventurer, i] = i + 10;

                swordHitRate += (i - 5) % 5 == 0 ? 2 : 1;
                _hitRate[(byte)ClassType.Swordsman, i] = swordHitRate;

                archerHitRate += i != 96 && i % 2 == 0 || i > 0 && i % 5 == 0 ? 4 : 2;
                _hitRate[(byte)ClassType.Archer, i] = archerHitRate;

                _hitRate[(byte)ClassType.Magician, i] = 0;

                fighterHitRate += i == 0 || (i - 4) % 10 == 0 || i > 0 && (i - 7) % 10 == 0 || i > 0 && (i - 10) % 10 == 0 ? 2 : 1;
                _hitRate[(byte)ClassType.MartialArtist, i] = fighterHitRate;
            }
        }

        public long GetHitRate(ClassType @class, byte level)
        {
            return _hitRate![(byte)@class, level - 1];
        }
    }
}
