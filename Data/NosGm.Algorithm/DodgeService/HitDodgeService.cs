using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.Algorithm
{
    public class HitDodgeService : IHitDodgeService
    {
        private readonly long[,] _hitDodge = new long[Constants.ClassCount, Constants.MaxLevel];

        public HitDodgeService()
        {
            var swordmanDodge = 8;
            var archerDodge = 18;
            var mageDodge = 18;
            var fighterDodge = 28;
            for (var i = 0; i < Constants.MaxLevel; i++)
            {
                _hitDodge[(byte)ClassType.Adventurer, i] = i + 10;

                swordmanDodge += (i - 5) % 5 == 0 ? 2 : 1;
                _hitDodge[(byte)ClassType.Swordsman, i] = swordmanDodge;

                archerDodge += ((i - 2) % 10 == 0 || (i - 4) % 10 == 0 || (i - 5) % 5 == 0 || (i - 7) % 10 == 0 || (i - 9) % 10 == 0 || (i - 10) % 10 == 0) ? 2 : 1;
                _hitDodge[(byte)ClassType.Archer, i] = archerDodge;

                mageDodge += (i - 5) % 5 == 0 ? 2 : 1;
                _hitDodge[(byte)ClassType.Magician, i] = mageDodge;

                fighterDodge += ((i - 4) % 10 == 0 || (i - 7) % 10 == 0 || (i - 10) % 10 == 0) ? 2 : 1;
                _hitDodge[(byte)ClassType.MartialArtist, i] = fighterDodge;
            }
        }

        public long GetHitDodge(ClassType @class, byte level)
        {
            return _hitDodge![(byte)@class, level - 1];
        }
    }
}
