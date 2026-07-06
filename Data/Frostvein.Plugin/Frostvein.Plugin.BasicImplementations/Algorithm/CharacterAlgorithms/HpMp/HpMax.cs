using Frostvein.Domain;

namespace Plugins.BasicImplementations.Algorithm.CharacterAlgorithms.HpMp
{
    public class HpMax : ICharacterStatAlgorithm
    {
        private const int MAX_LEVEL = 256;
        private int[,] _hpData;

        public void Initialize()
        {
            _hpData = new int[(int)ClassType.Unknown, MAX_LEVEL];

            // Adventurer HP
            var basicHp = 205;
            var basicInc = 15;
            for (var i = 0; i < MAX_LEVEL; i++)
            {
                basicInc++;
                basicHp += basicInc;

                _hpData[(byte)ClassType.Adventurer, i] = basicHp;
            }

            var swordHp = 190;
            var swordInc = 14;
            for (var i = 0; i < MAX_LEVEL; i++)
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
            for (var i = 0; i < MAX_LEVEL; i++)
            {
                mageInc++;
                magecHp += mageInc;

                _hpData[(byte)ClassType.Magician, i] = magecHp;
            }

            var archerHp = 190;
            var archerInc = 14;
            for (var i = 0; i < MAX_LEVEL; i++)
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
            for (var i = 0; i < MAX_LEVEL; i++)
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

        public int GetStat(ClassType type, byte level) => _hpData[(int)type, level - 1 > 0 ? level - 1 : 0];
    }
}