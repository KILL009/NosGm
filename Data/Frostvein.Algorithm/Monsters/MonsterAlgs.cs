using System;

namespace Frostvein.Algorithm.Monsters
{
    public static class MonsterAlgs
    {
        private static readonly double[] DefenseRace0 = { 16, 13.5, 11, 50, 50, 50 };
        private static readonly double[] DefenseRace1 = { 20, 17, 19, 100, 100, 100 };
        private static readonly double[] DefenseRace2 = { 15, 15, 15, 75, 50, 40 };
        private static readonly double[] DefenseRace3 = { 15, 15, 15, 50, 50, 50 };
        private static readonly double[] DefenseRace4 = { 17.4, 17.4, 17.4, 60, 60, 100 };
        private static readonly double[] DefenseRace5 = { 13.4, 13.4, 13.4, 40, 40, 40 };
        private static readonly double[] DefenseRace6 = { 11.5, 15, 25, 50, 50, 75 };
        private static readonly double[] DefenseRace8 = { 10, 10, 10, 100, 100, 100 };
        private static readonly double[] DefaultDefense = { 0, 0, 0, 0, 0, 0 };

        public static int GetBasicHp(int race, int level, int modifier, int additionalHp, bool isMonster = true)
        {
            double hp = 0;
            int a = 0;
            int b = 0;
            int c = 0;
            int x = 0;

            if (isMonster)
            {
                modifier = 0;
            }

            switch (race)
            {
                case 0:
                    a = 0;
                    b = 2;
                    c = 138;
                    break;
                case 1:
                    a = 10;
                    b = 10;
                    c = 610;
                    break;
                case 2:
                    a = 5;
                    b = 0;
                    c = 105;
                    break;
                case 3:
                    a = 0;
                    b = 0;
                    c = 205;
                    break;
                case 4:
                    a = 2;
                    b = 5;
                    c = 695;
                    break;
                case 5:
                    a = -2;
                    b = -3;
                    c = 263;
                    break;
                case 6:
                    a = 0;
                    b = -7;
                    c = 21;
                    break;
                default:
                    a = 0;
                    b = 0;
                    c = 0;
                    break;
            }

            if (race == 8)
            {
                hp = 7;
            }
            else
            {
                x = level;
                if ((modifier + a) != 0)
                {
                    x += (int)Math.Floor(d: (level - 1) / (decimal)(10.0 / (modifier + a)));
                }

                hp = 0.5 * (x * x) + (15.5 + b) * x + c;
            }

            if (!isMonster)
            {
                return (int)Math.Floor(hp + additionalHp);
            }

            if (level >= 37 && level <= 51)
            {
                hp *= 1.2;
            }
            else if (level >= 52 && level <= 61)
            {
                hp *= 1.5;
            }
            else if (level >= 62 && level <= 71)
            {
                hp *= 1.8;
            }
            else if (level >= 72 && level <= 81)
            {
                hp *= 2.5;
            }
            else if (level >= 81)
            {
                hp *= 3.5;
            }

            return (int)Math.Floor(hp + additionalHp);
        }

        public static int GetBasicMp(int race, int level, int modifier, int additionalHp = 0, bool isMonster = true)
        {
            double mp = 0;
            int z = 0;
            double d = 0;
            int a = 0;
            int g = 0;
            int x = 0;

            if (isMonster)
            {
                modifier = 0;
            }

            switch (race)
            {
                case 0:
                    d = 4.75;
                    a = 0;
                    g = 0;
                    break;
                case 1:
                    d = 178.75;
                    a = 10;
                    g = 8;
                    break;
                case 2:
                    d = 50.75;
                    a = -2;
                    g = 4;
                    break;
                case 3:
                    d = 50.75;
                    a = 0;
                    g = 4;
                    break;
                case 4:
                    d = 385.75;
                    a = 10;
                    g = 6;
                    z = 1;
                    break;
                case 5:
                    d = 23.75;
                    a = -2;
                    g = 2;
                    z = 1;
                    break;
                case 6:
                    d = 705.75;
                    a = 5;
                    g = 14;
                    break;
                case 8:
                    return (int)Math.Floor(4 + (double)additionalHp);
                default:
                    d = 0;
                    a = 0;
                    g = 0;
                    break;
            }

            x = level;
            if ((modifier + a) != 0)
            {
                x += (int)Math.Floor((level - 1) / (decimal)(10.0 / (modifier + a)) + z);
            }

            mp = Math.Floor((5.25 + g) * x + d) + ((Math.Floor((double)(x - 6) / 4) + 1) * 2) * ((Mod(x - 2, 4) + 1) + Math.Floor(((double)x - 6) / 4) * 2);

            return (int)Math.Floor(mp + additionalHp);
        }

        public static short GetAttack(bool isMin, int race, byte attackType, short weaponLevel, byte wInfo, byte level, int modifier, int additional, bool isWild = true, short petLevel = 0)
        {
            int calcLevel;
            int weaponMod;
            int a;
            int b;
            int c;

            if (isWild)
            {
                modifier = 0;
                calcLevel = level;
                weaponMod = weaponLevel;
            }
            else
            {
                calcLevel = petLevel;
                weaponMod = petLevel + level - weaponLevel;
            }

            switch (attackType)
            {
                case 0:
                    switch (race)
                    {
                        case 0:
                            a = 35;
                            b = 0;
                            break;
                        case 1:
                            a = 43;
                            b = 10;
                            break;
                        case 2:
                            a = 33;
                            b = 5;
                            break;
                        case 3:
                            a = 33;
                            b = 0;
                            break;
                        case 4:
                            a = 38;
                            b = 2;
                            break;
                        case 5:
                            a = 30;
                            b = -2;
                            break;
                        case 6:
                            a = 26;
                            b = 0;
                            break;
                        case 8:
                            a = 23;
                            b = 0;
                            break;
                        default:
                            a = 0;
                            b = 0;
                            break;
                    }

                    break;
                case 1:
                    switch (race)
                    {
                        case 0:
                            a = 30;
                            b = 0;
                            break;
                        case 1:
                            a = 38;
                            b = 10;
                            break;
                        case 2:
                            a = 33;
                            b = 0;
                            break;
                        case 3:
                            a = 33;
                            b = 0;
                            break;
                        case 4:
                            a = 38;
                            b = 2;
                            break;
                        case 5:
                            a = 30;
                            b = -2;
                            break;
                        case 6:
                            a = 33;
                            b = 0;
                            break;
                        case 8:
                            a = 23;
                            b = 0;
                            break;
                        default:
                            a = 0;
                            b = 0;
                            break;
                    }

                    break;
                case 2:
                    switch (race)
                    {
                        case 0:
                            a = 25;
                            b = 0;
                            break;
                        case 1:
                            a = 41;
                            b = 10;
                            break;
                        case 2:
                            a = 33;
                            b = -2;
                            break;
                        case 3:
                            a = 33;
                            b = 0;
                            break;
                        case 4:
                            a = 38;
                            b = 10;
                            break;
                        case 5:
                            a = 30;
                            b = -2;
                            break;
                        case 6:
                            a = 53;
                            b = 5;
                            break;
                        case 8:
                            a = 23;
                            b = 0;
                            break;
                        default:
                            a = 0;
                            b = 0;
                            break;
                    }

                    break;
                default:
                    a = 0;
                    b = 0;
                    break;
            }

            if (wInfo > 1)
            {
                c = (int)Math.Floor((decimal)((calcLevel + 2.0) / (wInfo - 1))) + 1;
            }
            else
            {
                c = 0;
            }

            if (isMin)
            {
                return (short)Math.Floor(calcLevel + (a - 7.2) + 3.2 * weaponMod + Math.Floor((calcLevel - 1) * ((modifier + b) / 10.0)) + additional + c);
            }

            return (short)Math.Floor(calcLevel + a + 4.8 * weaponMod + Math.Floor((calcLevel - 1) * ((modifier + b) / 10.0)) + additional - c);
        }

        public static short GetHitrate(int race, byte attackType, short weaponLevel, short level, int modifier, int additional, bool isWild = true, short petLevel = 0)
        {
            int calcLevel;
            int weaponMod;
            int a;
            int b;

            if (isWild)
            {
                modifier = 0;
                calcLevel = level;
                weaponMod = weaponLevel;
            }
            else
            {
                calcLevel = petLevel;
                weaponMod = petLevel + level - weaponLevel;
            }

            switch (attackType)
            {
                case 0:
                    switch (race)
                    {
                        case 0:
                            a = 22;
                            b = 0;
                            break;
                        case 1:
                            a = 30;
                            b = 10;
                            break;
                        case 2:
                            a = 25;
                            b = 0;
                            break;
                        case 3:
                            a = 25;
                            b = 0;
                            break;
                        case 4:
                            a = 30;
                            b = 2;
                            break;
                        case 5:
                            a = 22;
                            b = -2;
                            break;
                        case 6:
                            a = 25;
                            b = 0;
                            break;
                        case 8:
                            a = 15;
                            b = 0;
                            break;
                        default:
                            a = 0;
                            b = 0;
                            break;
                    }

                    return (short)Math.Floor(calcLevel + 4 * weaponMod + a + Math.Floor((calcLevel - 1) * ((modifier + b) / 10.0)) + additional);

                case 1:
                    switch (race)
                    {
                        case 0:
                            a = 28;
                            b = 0;
                            break;
                        case 1:
                            a = 44;
                            b = 10;
                            break;
                        case 2:
                            a = 34;
                            b = 0;
                            break;
                        case 3:
                            a = 34;
                            b = 0;
                            break;
                        case 4:
                            a = 44;
                            b = 2;
                            break;
                        case 5:
                            a = 28;
                            b = -2;
                            break;
                        case 6:
                            a = 34;
                            b = 0;
                            break;
                        case 8:
                            a = 15;
                            b = 0;
                            break;
                        default:
                            a = 0;
                            b = 0;
                            break;
                    }

                    return (short)Math.Floor(2 * calcLevel + 4 * weaponMod + a + Math.Floor((calcLevel - 1) * ((modifier + b) / 10.0)) * 2 + additional);
                case 2:
                    return (short)(70 + additional);
                default:
                    return 0;
            }
        }

        public static int GetDodge(int race, short armorLevel, short level, int modifier, int additional, bool isWild = true, short petLevel = 0)
        {
            int calcLevel;
            int armorMod;
            int a;
            int b;

            if (isWild)
            {
                modifier = 0;
                calcLevel = level;
                armorMod = armorLevel;
            }
            else
            {
                calcLevel = petLevel;
                armorMod = petLevel + level - armorLevel;
            }

            switch (race)
            {
                case 0:
                    a = 26;
                    b = 0;
                    break;
                case 1:
                    a = 34;
                    b = 10;
                    break;
                case 2:
                    a = 29;
                    b = 0;
                    break;
                case 3:
                    a = 29;
                    b = 0;
                    break;
                case 4:
                    a = 34;
                    b = 2;
                    break;
                case 5:
                    a = 26;
                    b = -2;
                    break;
                case 6:
                    a = 29;
                    b = 0;
                    break;
                case 8:
                    a = 19;
                    b = 0;
                    break;
                default:
                    a = 0;
                    b = 0;
                    break;
            }

            return (int)Math.Floor(calcLevel + 4 * armorMod + a + Math.Floor((calcLevel - 1) * ((modifier + b) / 10.0)) + additional);
        }

        public static int GetDefense(int race, byte attackType, short armorLevel, short level, int modifier, int additional, bool isWild = true, short petLevel = 0)
        {
            int calcLevel;
            int armorMod;
            double[] raceInfo;

            if (isWild)
            {
                modifier = 0;
                calcLevel = level;
                armorMod = armorLevel;
            }
            else
            {
                calcLevel = petLevel;
                armorMod = petLevel + level - armorLevel;
            }

            raceInfo = race switch
            {
                0 => DefenseRace0,
                1 => DefenseRace1,
                2 => DefenseRace2,
                3 => DefenseRace3,
                4 => DefenseRace4,
                5 => DefenseRace5,
                6 => DefenseRace6,
                8 => DefenseRace8,
                _ => DefaultDefense
            };

            return attackType switch
            {
                0 => (int)Math.Floor(2 * armorMod + raceInfo[0] + Math.Floor((armorMod + 5) * 0.08) + (calcLevel - 1) * ((modifier * 10 + (raceInfo[3] - 5 * modifier)) / 100.0) +
                    additional),
                1 => (int)Math.Floor(2 * armorMod + raceInfo[1] + Math.Floor((armorMod + 5) * 0.36) + (calcLevel - 1) * ((modifier * 10 + (raceInfo[4] - 5 * modifier)) / 100.0) +
                    additional),
                2 => (int)Math.Floor(2 * armorMod + raceInfo[2] + Math.Floor((armorMod + 5) * 0.04) + (calcLevel - 1) * ((modifier * 10 + (raceInfo[5] - 5 * modifier)) / 100.0) +
                    additional),
            };
        }

        private static double Mod(int a, int modulo)
        {
            if (a >= 0)
            {
                return a % modulo;
            }

            return Math.Abs((a - 2) % modulo);
        }
    }
}
