using NosGm.Domain;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;

namespace NosGm.GameObject.Helpers
{
    public class MateHelper
    {
        #region Members

        private static MateHelper _instance;

        #endregion

        #region Instantiation

        public MateHelper()
        {
            LoadConcentrate();
            LoadHpData();
            LoadMinDamageData();
            LoadMaxDamageData();
            LoadMpData();
            LoadXpData();
            LoadDefences();
            LoadPetSkills();
            LoadMateBuffs();
            LoadTrainerUpgradeHits();
            LoadTrainerUpRate();
            LoadTrainerDownRate();
            LoadPartnerSkills();
        }

        #endregion

        #region Properties

        public static MateHelper Instance => _instance ?? (_instance = new MateHelper());

        public int[,] Concentrate { get; private set; }

        public int[,] HpData { get; private set; }

        public int[,] MagicDefenseData { get; private set; }

        // Vnum - CardId
        public Dictionary<int, int> MateBuffs { get; set; }

        public int[,] MaxDamageData { get; private set; }

        public int[,] MeleeDefenseData { get; private set; }

        public int[,] MeleeDefenseDodgeData { get; private set; }

        public int[,] MinDamageData { get; private set; }

        public int[,] MpData { get; private set; }

        public List<short> PartnerSpBuffs { get; set; }

        public Dictionary<int, int> PartnerBuffs { get; set; }

        public List<int> PetSkills { get; set; }

        public List<short> WingBuff { get; set; }

        public Dictionary<int, short> PartnerSkills { get; set; }

        public int[,] RangeDefenseData { get; private set; }

        public int[,] RangeDefenseDodgeData { get; private set; }

        public int[] TrainerDownRate { get; private set; }

        public int[] TrainerUpgradeHits { get; private set; }

        public int[] TrainerUpRate { get; private set; }

        public double[] XpData { get; private set; }

        #endregion

        #region Methods

        public void AddPartnerBuffs(ClientSession session, Mate mate)
        {
            if (PartnerBuffs.TryGetValue(mate.Sp.Instance.ItemVNum, out var cardId) && session.Character.Buff.All(b => b.Card.CardId != cardId))
            {
                var sum = mate.Sp.GetLevelForAllSkill() / 3;
                if (sum < 1) sum = 1;
                session.Character.AddBuff(new Buff((short)(cardId + (sum - 1)), mate.Level, true), mate.BattleEntity);
            }
        }

        public void LoadConcentrate()
        {
            Concentrate = new int[3, 151];

            var baseValue = 0;

            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 424;
                        break;

                    case 1:
                        baseValue = 702;
                        break;

                    case 2:
                        baseValue = 702;
                        break;
                }

                for (var j = 0; j < Concentrate.GetLength(1); j++) Concentrate[i, j] = baseValue * j / 100;
            }
        }

        public void LoadDefences()
        {
            MeleeDefenseData = new int[3, 151];
            var baseValue = 0;
            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 640;
                        break;

                    case 1:
                        baseValue = 682;
                        break;

                    case 2:
                        baseValue = 520;
                        break;
                }

                for (var j = 0; j < MeleeDefenseData.GetLength(1); j++) MeleeDefenseData[i, j] = baseValue * j / 100;
            }

            RangeDefenseData = new int[3, 151];
            baseValue = 0;
            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 612;
                        break;

                    case 1:
                        baseValue = 510;
                        break;

                    case 2:
                        baseValue = 425;
                        break;
                }

                for (var j = 0; j < RangeDefenseData.GetLength(1); j++) RangeDefenseData[i, j] = baseValue * j / 100;
            }

            MagicDefenseData = new int[3, 151];
            baseValue = 0;
            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 510;
                        break;

                    case 1:
                        baseValue = 425;
                        break;

                    case 2:
                        baseValue = 680;
                        break;
                }

                for (var j = 0; j < MagicDefenseData.GetLength(1); j++) MagicDefenseData[i, j] = baseValue * j / 100;
            }

            MeleeDefenseDodgeData = new int[3, 151];
            baseValue = 0;
            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 600;
                        break;

                    case 1:
                        baseValue = 799;
                        break;

                    case 2:
                        baseValue = 575;
                        break;
                }

                for (var j = 0; j < MeleeDefenseDodgeData.GetLength(1); j++)
                    MeleeDefenseDodgeData[i, j] = baseValue * j / 100;
            }

            RangeDefenseDodgeData = new int[3, 151];
            baseValue = 0;
            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 640;
                        break;

                    case 1:
                        baseValue = 745;
                        break;

                    case 2:
                        baseValue = 575;
                        break;
                }

                for (var j = 0; j < RangeDefenseDodgeData.GetLength(1); j++)
                    RangeDefenseDodgeData[i, j] = baseValue * j / 100;
            }
        }

        public void LoadHpData()
        {
            HpData = new int[3, 151];

            var baseValue = 0;

            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 32100; //22628
                        break;

                    case 1:
                        baseValue = 22728; //12728
                        break;

                    case 2:
                        baseValue = 19900; //9900
                        break;
                }

                for (var j = 0; j < HpData.GetLength(1); j++) HpData[i, j] = baseValue * j / 100;
            }
        }

        public void LoadMaxDamageData()
        {
            MaxDamageData = new int[3, 151];

            var baseValue = 0;

            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 4000;
                        break;

                    case 1:
                        baseValue = 3000;
                        break;

                    case 2:
                        baseValue = 2500;
                        break;
                }

                for (var j = 0; j < MaxDamageData.GetLength(1); j++) MaxDamageData[i, j] = baseValue * j / 100;
            }
        }

        public void LoadMinDamageData()
        {
            MinDamageData = new int[3, 151];

            var baseValue = 0;

            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 2200;
                        break;

                    case 1:
                        baseValue = 1900;
                        break;

                    case 2:
                        baseValue = 1200;
                        break;
                }

                for (var j = 0; j < MinDamageData.GetLength(1); j++) MinDamageData[i, j] = baseValue * j / 100;
            }
        }

        public void LoadMpData()
        {
            MpData = new int[3, 151];

            var baseValue = 0;

            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        baseValue = 5071;
                        break;

                    case 1:
                        baseValue = 7900;
                        break;

                    case 2:
                        baseValue = 12385;
                        break;
                }

                for (var j = 0; j < MpData.GetLength(1); j++) MpData[i, j] = baseValue * j / 100;
            }
        }

        public void LoadPetSkills()
        {
            PetSkills = new List<int>
            {
                1513, // Purcival
                1514, // Baron scratch ?
                1515, // Amiral (le chat chelou)
                1516, // roi des pirates pussifer
                1524, // Miaou fouP
                1575, // Marié Bouhmiaou
                1576, // Marie Bouhmiaou
                1601, // Mechamiaou
                1627, // Boris the polar bear
                663, // Otter
                969, // Wisden
                1021, //Draco?
                1022, //Glacerus?
                1023, //Liruti
                2051, //Caligor
                790, // Golden Gloopy Cake
                1673, // Baby Raptop Blue
            };
        }

        public void LoadWingBuff()
        {
            WingBuff = new List<short>
            {
                387, 395, 396, 397, 398, 410, 411, 444, 663, 686, 755, 838, 851, 926, 985, 1444, 4002
            };
        }

        public void LoadTrainerDownRate()
        {
            TrainerDownRate = new[] { 0, 7, 13, 16, 28, 29, 33, 36, 50, 60 };
        }

        public void LoadTrainerUpgradeHits()
        {
            TrainerUpgradeHits = new int[10];

            var baseValue = 0;

            for (var i = 0; i < 10; i++)
            {
                baseValue += 50;
                TrainerUpgradeHits[i] = baseValue;
            }
        }

        public void LoadTrainerUpRate()
        {
            TrainerUpRate = new[] { 67, 67, 44, 34, 22, 15, 14, 8, 1, 0 };
        }

        public void LoadXpData()
        {
            XpData = new double[256];
            var v = new double[256];
            double var = 1;
            v[0] = 540;
            v[1] = 960;
            XpData[0] = 300;
            for (var i = 2; i < v.Length; i++) v[i] = v[i - 1] + 420 + 120 * (i - 1);

            for (var i = 1; i < XpData.Length; i++)
            {
                if (i < 79)
                {
                    switch (i)
                    {
                        case 14:
                            var = 6 / 3d;
                            break;

                        case 39:
                            var = 19 / 3d;
                            break;

                        case 59:
                            var = 70 / 3d;
                            break;
                    }

                    XpData[i] = Convert.ToInt64(XpData[i - 1] + var * v[i - 1]);
                }

                if (i < 79) continue;

                switch (i)
                {
                    case 79:
                        var = 5000;
                        break;

                    case 82:
                        var = 9000;
                        break;

                    case 84:
                        var = 13000;
                        break;
                }

                XpData[i] = Convert.ToInt64(XpData[i - 1] + var * (i + 2) * (i + 2));
            }
        }

        public void RemovePartnerBuffs(ClientSession session)
        {
            if (session == null) return;

            foreach (var val in PartnerSpBuffs) session.Character.RemoveBuff(val, true);
        }

        public void AddPartnerSkill(ClientSession session, Mate mate)
        {
            if (session == null || mate == null)
            {
                return;
            }

            if (mate.MateType != MateType.Partner)
            {
                return;
            }

            NpcMonster mateNpc = ServerManager.GetNpcMonster(mate.NpcMonsterVNum);
            if (mateNpc.Skills != null)
            {
                foreach (NpcMonsterSkill skill in mateNpc.Skills)
                {
                    if (mate.Level >= skill.Skill.LevelMinimum)
                    {
                        CharacterSkill characterSkillMate = new CharacterSkill
                        {
                            SkillVNum = skill.SkillVNum,
                            CharacterId = session.Character.CharacterId
                        };
                        session.Character.Skills[skill.SkillVNum] = new CharacterSkill(characterSkillMate);
                    }
                }
            }

            session.SendPacket(session.Character.GenerateSki());
            session.SendPackets(session.Character.GenerateQuicklist());
            session.SendPacket(session.Character.GenerateStat());
            session.SendPackets(session.Character.GenerateStatChar());
            session.CurrentMapInstance?.Broadcast(session.Character.GenerateEq());
        }


        #region PartnerSPSkills

        public short GetUpgradeType(short morph)
        {
            switch (morph)
            {
                case 2043:
                    return 1;
                case 2044:
                    return 2;
                case 2045:
                    return 3;
                case 2046:
                    return 4;
                case 2047:
                    return 5;
                case 2048:
                    return 6;
                case 2310:
                    return 7;
                case 2317:
                    return 8;
                case 2323:
                    return 9;
                case 2325:
                    return 10;
                case 2333:
                    return 11;
                case 2334:
                    return 12;
                case 2343:
                    return 13;
                case 2355:
                    return 14;
                case 2356:
                    return 15;
                case 2367:
                    return 16;
                case 2368:
                    return 17;
                case 2371:
                    return 18;
                case 2372:
                    return 19;
                case 2373:
                    return 20;
                case 2374:
                    return 21;
                case 2376:
                    return 22;
                case 2377:
                    return 23;
                case 2378:
                    return 24;
                case 2537:
                    return 25;
                case 2538:
                    return 26;
            }
            return -1;
        }

        #endregion

        private void LoadMateBuffs()
        {
            MateBuffs = new Dictionary<int, int>
            {
                {536, 162}, // LUCKY PIG
                {178, 108}, // LUCKY PIG
                {670, 374}, // FIBI
                {829, 377}, // RUDY ROWDY
                {836, 381}, // FLUFFY BALLY
                {838, 385}, // NAVY BUSHTAIL
                {840, 442}, // LEO THE COWARD
                {841, 394}, // RATUFU NINJA
                {842, 399}, // INDIAN BUSHTAIL
                {843, 403}, // VIKING BUSHTAIL
                {844, 391}, // COWBOY BUSHTAIL
                {997, 825}, // DEATH LANCER
                {1456, 828}, // SPIRIT KING AVATAR
                {1470, 827}, // BEAST KING AVATAR
                {682, 1011}, // RAINBOW PEGASUS
                {671,  687}, // MINI FIRE DEVIL
                {683, 742}, // OTTER
                {2708, 708}, // POLAR BEAR
                {2704, 710}, // FORTUNE BUSHTAIL
                {2709, 711}, // SUPER FORTUNE BUSHTAIL
                {2710, 783}, // MR TRIKLES
                {673, 295}, // REINDER RED
                {815, 298}, // CENTURIO BUSHI
                {2682, 300}, // BARON BLESING
                {709, 840}, // GOLDEN GLOOPY CAKE
                {687, 842}, // GLOOPY CAKE
                {581, 309}, // AQUA BUSHTAIL
                {846, 310}, // PUSS IN THE BOOTS
                {814, 311}, // SHOGUN BUSHI
                {4015, 1242}, // BLUE MAGICIAN
                {4016, 1243}, // DESTROYER
                {4017, 1244}, // CRUSADER
                {1703, 258}, // DARK GUNNER
                {1704, 260}, // WILD KEEPER
                {1705, 262}, // BERSEKER
                {1716, 1100}, // AMON
                {1717, 1101}, // ARCHANGEL LUCIFER
                {3128, 855}, // WISDEN
                {2106, 275}, // Flare
                {2107, 276}, // Blaze
                {2105, 277}, // Inferno
                {2118, 278}, // Yellow Lamb
                {2119, 279}, // Yellow Sheep
                {2120, 280}, // Golden Fleece
                {692, 281}, // Shadow^Hamster
                {719, 282}, // Nightshade^Hamster
                {720, 283}, // Blackthorn^Hamster
                {2713, 284}, // Ice Wolf
                {2714, 285}, // Frost Wolf
                {2715, 286}, // Blizzard Wolf
                {2681, 885}, // Sir Purcival
                {784, 886}, // Rusty Robby
                {448, 898}, // Grasslin Fairy
                {1446, 940}, // Small Ice Witch
                {642, 1152}, // Football Bushi (ES)
                {2121, 1156}, // Creepy Imp
                {714, 844}, // Gecko Paladin Blessing
                {806, 937}, // Baby Raptor Blue
                {3203, 946}, // Baby Sky Dragon
                {3285, 5001},
                {2015, 5002},
                {3286, 5003},
                {828, 4134}, // Seraphim Blessing
            };
        }

        private void LoadPartnerSkills()
        {
            PartnerSkills = new Dictionary<int, short>
            {
                { 2620, 1482 },
                { 2617, 1475 },
                { 2618, 1473 },
                { 2673, 1507 },
                { 2640, 1488 },
            };
            PartnerBuffs = new Dictionary<int, int>
            {
                {4825, 3000}, // Vénus
                {8247, 3000}, // Vénus
                {4326, 3007}, // Guerrier Squelettique Ragnar
                {8300, 3007}, // Guerrier Squelettique Ragnar
                {4405, 3014}, // Yuna
                {8367, 3014}, // Yuna
                {4413, 3021}, // Cupidia
                {8375, 3021}, // Cupidia
                {4446, 3028}, // Perti
                {8398, 3028}, // Perti
                {4547, 3035}, // Akhenaton
                {8420, 3035}, // Akhenaton
            };
            PartnerSpBuffs = new List<short>
            {
                3000,
                3001,
                3002,
                3003,
                3004,
                3005,
                3006,
                3007,
                3008,
                3009,
                3010,
                3011,
                3012,
                3013,
                3014,
                3015,
                3016,
                3017,
                3018,
                3019,
                3020,
                3021,
                3022,
                3023,
                3024,
                3025,
                3026,
                3027,
                3028,
                3029,
                3030,
                3031,
                3032,
                3033,
                3034,
                3035,
                3036,
                3037,
                3038,
                3039,
                3040,
                3041
            };
        }

        #endregion
    }
}
