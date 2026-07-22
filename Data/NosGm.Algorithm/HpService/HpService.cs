using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.Algorithm
{
    public class HpService : IHpService
    {
        private readonly long[,] _hpData = new long[Constants.ClassCount, Constants.MaxLevel];

        public HpService()
        {
            // Adventurer HP
            var basicHp = 205;
            var basicInc = 15;
            for (var i = 0; i < Constants.MaxLevel; i++)
            {
                basicInc++;
                basicHp += basicInc;

                _hpData[(byte)ClassType.Adventurer, i] = basicHp;
            }

            var swordHp = 190;
            var swordInc = 14;
            for (var i = 0; i < Constants.MaxLevel; i++)
            {
                var increase2 = (i - 2) % 10 == 0;
                var increase3 = (i - 3) % 10 == 0;
                var increase4 = (i - 4) % 10 == 0;
                var increase5 = (i - 5) % 10 == 0;
                var increase7 = (i - 7) % 10 == 0;
                var increase8 = (i - 8) % 10 == 0;
                var increase9 = (i - 9) % 10 == 0;

                swordInc++;
                swordHp += swordInc;

                if (increase2 || increase3 || increase4 || increase5 || increase7 || increase8 || increase9 || i % 10 == 0)
                {
                    swordInc++;
                    swordHp += swordInc;
                }

                _hpData[(byte)ClassType.Swordsman, i] = swordHp;
            }

            var magecHp = 205;
            var mageInc = 15;
            for (var i = 0; i < Constants.MaxLevel; i++)
            {
                mageInc++;
                magecHp += mageInc;

                _hpData[(byte)ClassType.Magician, i] = magecHp;
            }

            var archerHp = 190;
            var archerInc = 14;
            for (var i = 0; i < Constants.MaxLevel; i++)
            {
                var increase4 = (i - 4) % 10 == 0;
                var increase7 = (i - 7) % 10 == 0;

                archerInc++;
                archerHp += archerInc;

                if (increase4 || increase7 || i % 10 == 0)
                {
                    archerInc++;
                    archerHp += archerInc;
                }

                _hpData[(byte)ClassType.Archer, i] = archerHp;
            }

            var fighterHp = 190;
            var fighterInc = 14;
            for (var i = 0; i < Constants.MaxLevel; i++)
            {
                var increase2 = (i - 2) % 10 == 0;
                var increase4 = (i - 4) % 10 == 0;
                var increase6 = (i - 6) % 10 == 0;
                var increase7 = (i - 7) % 10 == 0;

                fighterInc++;
                fighterHp += fighterInc;

                if (increase2 || increase4 || increase6 || increase7 || i % 10 == 0)
                {
                    fighterInc++;
                    fighterHp += fighterInc;
                }

                _hpData[(byte)ClassType.MartialArtist, i] = fighterHp;
            }
        }
        public long GetHp(ClassType @class, byte level)
        {
            return _hpData![(byte)@class, level - 1];
        }

        public async Task<long> GetHpAsync(ClassType @class, byte level)
        {
            return await Task.FromResult(_hpData![(byte)@class, level - 1]);
        }
    }
}
