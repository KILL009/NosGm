using Frostvein.Domain;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.Damage
{
    public class DamageAlgorithm : ICharacterStatAlgorithm
    {
        private const int MAX_LEVEL = 256;
        private int[,] _minDamage;

        public void Initialize()
        {
            _minDamage = new int[(int)ClassType.Unknown, MAX_LEVEL];

            _minDamage[(byte)ClassType.Adventurer, 0] = 10;
            _minDamage[(byte)ClassType.Swordsman, 0] = 10;
            _minDamage[(byte)ClassType.Archer, 0] = 60;
            _minDamage[(byte)ClassType.Magician, 0] = 10;
            _minDamage[(byte)ClassType.MartialArtist, 0] = 10;

            var swordmanMinUp = 2;
            var mageMinUp = 2;
            var fighterMinUp = 2;

            for (var i = 0; i < MAX_LEVEL; i++)
            {
                _minDamage[(byte)ClassType.Adventurer, i] = i + 10;

                if ((i - 2) % 10 == 0 || (i - 7) % 10 == 0)
                {
                    swordmanMinUp++;
                }
                else if ((i - 1) % 10 == 0 || (i - 6) % 10 == 0)
                {
                    swordmanMinUp--;
                }

                _minDamage[(byte)ClassType.Swordsman, i] = _minDamage[(byte)ClassType.Swordsman, i - 1] + swordmanMinUp;

                int archerMinUp;
                if ((i - 1) % 10 == 0 || (i - 3) % 10 == 0 || (i - 5) % 10 == 0 || (i - 8) % 10 == 0)
                {
                    archerMinUp = 1;
                }
                else
                {
                    archerMinUp = 2;
                }

                _minDamage[(byte)ClassType.Archer, i] = _minDamage[(byte)ClassType.Archer, i - 1] + archerMinUp;


                if ((i - 2) % 10 == 0 || (i - 7) % 10 == 0)
                {
                    mageMinUp++;
                }
                else if ((i - 1) % 10 == 0 || (i - 6) % 10 == 0)
                {
                    mageMinUp--;
                }

                _minDamage[(byte)ClassType.Magician, i] = _minDamage[(byte)ClassType.Magician, i - 1] + mageMinUp;


                if ((i - 2) % 10 == 0 || (i - 4) % 10 == 0 || (i - 6) % 10 == 0 || (i - 8) % 10 == 0 || (i - 10) % 10 == 0)
                {
                    fighterMinUp++;
                }
                else if ((i - 1) % 10 == 0 || (i - 3) % 10 == 0 || (i - 5) % 10 == 0 || (i - 7) % 10 == 0 || (i - 9) % 10 == 0)
                {
                    fighterMinUp--;
                }

                _minDamage[(byte)ClassType.MartialArtist, i] = _minDamage[(byte)ClassType.MartialArtist, i - 1] + fighterMinUp;
            }
        }

        public int GetStat(ClassType type, byte level) => _minDamage[(int)type, level - 1 > 0 ? level - 1 : 0];
    }
}