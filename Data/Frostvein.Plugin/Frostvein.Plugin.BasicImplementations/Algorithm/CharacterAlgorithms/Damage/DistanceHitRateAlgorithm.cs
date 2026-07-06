using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Damage
{
    public class DistanceHitRateAlgorithm : ICharacterStatAlgorithm
    {
        private const int MAX_LEVEL = 256;
        private int[,] _hitRate;

        public void Initialize()
        {
            var adventurerHit = 18;
            var adventurerHitUp = 2;
            var fighterHit = 16;
            var mageHit = 16;
            var archerHit = 23;
            var swordmanHit = 16;

            for (var i = 0; i < MAX_LEVEL; i++)
            {
                adventurerHit += adventurerHitUp;
                _hitRate[(byte)ClassType.Adventurer, i] = adventurerHit;

                swordmanHit += (i - 5) % 5 == 0 ? 4 : 2;
                _hitRate[(byte)ClassType.Swordsman, i] = swordmanHit;

                archerHit += i != 0 && ((i - 1) % 10 == 0 || (i - 3) % 10 == 0 || (i - 5) % 10 == 0 || (i - 8) % 10 == 0) ? 1 : 2;
                _hitRate[(byte)ClassType.Archer, i] = archerHit;

                mageHit += (i - 5) % 5 == 0 ? 4 : 2;
                _hitRate[(byte)ClassType.Magician, i] = mageHit;

                fighterHit += (i - 4) % 4 == 0 || (i - 10) % 10 == 0 ? 4 : 2;
                _hitRate[(byte)ClassType.MartialArtist, i] = fighterHit;
            }
        }

        public int GetStat(ClassType type, byte level) => _hitRate[(int)type, level - 1 > 0 ? level - 1 : 0];
    }
}
