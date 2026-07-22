using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.Algorithm
{
    public class DistanceDodgeService : IDistanceDodgeService
    {
        private readonly long[,] _distanceDodge = new long[Constants.ClassCount, Constants.MaxLevel];

        public DistanceDodgeService()
        {
            var swordmanDodge = 8;
            var archerDodge = 18;
            var mageDodge = 8;
            var fighterDodge = 18;
            for (var i = 0; i < Constants.MaxLevel; i++)
            {
                _distanceDodge[(byte)ClassType.Adventurer, i] = i + 10;

                swordmanDodge += (i - 5) % 5 == 0 ? 2 : 1;
                _distanceDodge[(byte)ClassType.Swordsman, i] = swordmanDodge;

                archerDodge += ((i - 2) % 10 == 0 || (i - 4) % 10 == 0 || (i - 5) % 5 == 0 || (i - 7) % 10 == 0 || (i - 9) % 10 == 0 || (i - 10) % 10 == 0) ? 2 : 1;
                _distanceDodge[(byte)ClassType.Archer, i] = archerDodge;

                mageDodge += (i - 5) % 5 == 0 ? 2 : 1;
                _distanceDodge[(byte)ClassType.Magician, i] = mageDodge;

                fighterDodge += ((i - 4) % 10 == 0 || (i - 7) % 10 == 0 || (i - 10) % 10 == 0) ? 2 : 1;
                _distanceDodge[(byte)ClassType.MartialArtist, i] = fighterDodge;

            }
        }

        public long GetDistanceDodge(ClassType @class, byte level)
        {
            return _distanceDodge![(byte)@class, level - 1];
        }
    }
}
