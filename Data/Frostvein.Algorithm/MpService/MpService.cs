using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.Algorithm
{
    public class MpService : IMpService
    {
        private readonly long[,] _mpData = new long[Constants.ClassCount, Constants.MaxLevel];

        public MpService()
        {
            var adventurerMpAdd = 9;
            var magicianMpAdd = new[] { 9, 18, 21, 22, 25, 13, 27, 30, 31, 34 };
            var martialArtistMpAdd = new[] { 9, 9, 9, 10, 22, 11, 12, 13, 13, 27 };
            var archerMpAdd = new[] { 9, 9, 9, 10, 11, 11, 11, 12, 13, 26 };

            _mpData[(byte)ClassType.Adventurer, 0] = 60;
            _mpData[(byte)ClassType.Swordsman, 0] = 60;
            _mpData[(byte)ClassType.Magician, 0] = 60;
            _mpData[(byte)ClassType.MartialArtist, 0] = 60;
            _mpData[(byte)ClassType.Archer, 0] = 60;
            var countMagician = 9;
            var countArcher = 11;
            var substractMagician = true;
            var substractArcher = false;
            var reverseArcher = 0;
            for (var i = 1; i < Constants.MaxLevel; i++)
            {
                adventurerMpAdd += i % 4 == 0 ? 2 : 0;
                _mpData[(byte)ClassType.Adventurer, i] =
                    _mpData[(byte)ClassType.Adventurer, i - 1] + (i % 4 == 0 ? adventurerMpAdd - 1 : adventurerMpAdd);
                _mpData[(byte)ClassType.Swordsman, i] = _mpData[(byte)ClassType.Adventurer, i];
                if (i - 1 > 9)
                {
                    var switcher = !substractMagician ? 1 : -1;
                    if ((i - 1) % 5 == 0)
                    {
                        countMagician += !substractMagician ? 1 : -1;
                        substractMagician = countMagician == 10 || countMagician == 8 ? !substractMagician : substractMagician;
                        magicianMpAdd[(i - 1) % 10] = magicianMpAdd[(i - 1) % 10] + countMagician;
                    }
                    else
                    {
                        magicianMpAdd[(i - 1) % 10] += 18 + switcher / ((i - 1) % 5 % 2 == 1 ? 1 : -1);
                    }

                    if (i % 10 == 0)
                    {
                        countArcher += !substractArcher ? 1 : -1;
                        substractArcher = countArcher == 12 || countArcher == 10 ? !substractArcher : substractArcher;
                        archerMpAdd[(i - 1) % 10] = archerMpAdd[(i - 1) % 10] + countArcher;
                        reverseArcher++;
                    }
                    else
                    {
                        var switcherArcher = reverseArcher % 4 == 2 || reverseArcher % 4 == 3 ? -1 : 1;
                        archerMpAdd[(i - 1) % 10] += (switcherArcher == -1 ? 6 : 5) +
                                                     (i % 20 < 10
                                                         ? i % 4 == 2 || i % 4 == 1 ? 0 : switcherArcher
                                                         : i % 4 == 0 || i % 4 == 1 ? switcherArcher : 0);

                    }

                    martialArtistMpAdd[(i - 1) % 10] += (i - 1) % 5 == 4 ? 12 : 6;
                }
                _mpData[(byte)ClassType.Magician, i] =
                    _mpData[(byte)ClassType.Magician, i - 1] + magicianMpAdd[(i - 1) % 10];

                _mpData[(byte)ClassType.MartialArtist, i] =
                    _mpData[(byte)ClassType.MartialArtist, i - 1] + martialArtistMpAdd[(i - 1) % 10];

                _mpData[(byte)ClassType.Archer, i] =
                    _mpData[(byte)ClassType.Archer, i - 1] + archerMpAdd[(i - 1) % 10];
            }

        }
        public long GetMp(ClassType @class, byte level)
        {
            return _mpData![(byte)@class, level - 1];
        }
    }
}
