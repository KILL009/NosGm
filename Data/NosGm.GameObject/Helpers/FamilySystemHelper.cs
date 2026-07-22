using System.Collections.Generic;
using System.Linq;

namespace NosGm.GameObject.Helpers
{
    public class FamilySystemHelper
    {
        #region Members

        private static readonly Dictionary<short, short[]> FamilySystemItems = new Dictionary<short, short[]>
        {
            //VNUM      {TYPE, LEVEL MINIMUM, MAX AMMOUNT)
            { 9600, new short[] {0, 6, 0, 0 }}, 
            { 9601, new short[] {0, 4, 0, 1}},
            { 9602, new short[] {0, 4, 0, 2}},
            { 9603, new short[] {0, 4, 0, 3}},
            { 9604, new short[] {1, 2, 10, 4 }},
            { 9605, new short[] {1, 2, 5, 5}},
            { 9606, new short[] {1, 2, 1, 6}},
            { 9607, new short[] {1, 1, 5, 7}},
            { 9608, new short[] {1, 1, 5, 8}}, // CUBY DAILY - IDENTIFY ALL AND MORE EASY
            { 9609, new short[] {1, 1, 5, 9}},
            { 9610, new short[] {1, 1, 5, 10}},
            { 9611, new short[] {1, 1, 5, 11}},
            { 9612, new short[] {1, 2, 5, 12}},
            { 9613, new short[] {1, 2, 5, 13}},
            { 9614, new short[] {1, 3, 5, 14}},
            { 9615, new short[] {1, 3, 5, 15}},
            { 9616, new short[] {1, 3, 5, 16}},
            { 9617, new short[] {1, 1, 10, 17}},
            { 9618, new short[] {2, 1, 0, 18}},
            { 9619, new short[] {2, 2, 0, 19 }},
            { 9620, new short[] {2, 3, 0, 20 }},
            { 9621, new short[] {2, 4, 0, 21 }},
            { 9622, new short[] {2, 5, 0, 22}},
            { 9623, new short[] {2, 6, 0, 23 }},
            { 9624, new short[] {2, 7, 0, 24 }},
            { 9625, new short[] {2, 8, 0, 25 }},
            { 9626, new short[] {2, 9, 0, 26 }},
            { 9627, new short[] {2, 10, 0, 27 }},
            { 9628, new short[] {2, 11, 0, 28 }},
            { 9629, new short[] {2, 12, 0, 29 }},
            { 9630, new short[] {2, 13, 0, 30 }},
            { 9631, new short[] {2, 14, 0, 31 }},
            { 9632, new short[] {2, 15, 0, 32 }},
            { 9633, new short[] {2, 16, 0, 33 }},
            { 9634, new short[] {2, 17, 0, 34 }},
            { 9635, new short[] {2, 18, 0, 35 }},
            { 9636, new short[] {2, 19, 0, 36 }},
            { 9637, new short[] {2, 0, 20, 37 }},
            { 9638, new short[] {2, 0, 100, 38 }},
            { 9639, new short[] {2, 0, 300, 39 }},
            { 9640, new short[] {2, 0, 1000, 40 }},
            { 9641, new short[] {2, 0, 3000, 41}},
            { 9642, new short[] {2, 0, 20, 42}},
            { 9643, new short[] {2, 0, 100, 43}},
            { 9644, new short[] {2, 0, 300, 44}},
            { 9645, new short[] {2, 0, 1000, 45}},
            { 9646, new short[] {2, 0, 3000, 46}},
            { 9647, new short[] {2, 0, 5, 47}},
            { 9648, new short[] {2, 0, 50, 48}},
            { 9649, new short[] {2, 0, 150, 49}},
            { 9650, new short[] {2, 0, 500, 50}},
            { 9651, new short[] {2, 0, 1500, 51}},
            { 9652, new short[] {2, 0, 10, 52}},
            { 9653, new short[] {2, 0, 100, 53}},
            { 9654, new short[] {2, 0, 1000, 54 }},
            { 9655, new short[] {2, 0, 1, 55}},
            { 9656, new short[] {2, 0, 10, 56}},
            { 9657, new short[] {2, 0, 30, 57}},
            { 9658, new short[] {2, 0, 100, 58}},
            { 9659, new short[] {2, 0, 300, 59}},
            { 9660, new short[] {2, 0, 1, 60}},
            { 9661, new short[] {2, 0, 10, 61}},
            { 9662, new short[] {2, 0, 30, 62}},
            { 9663, new short[] {2, 0, 100, 63}},
            { 9664, new short[] {2, 0, 300, 64}},
            { 9665, new short[] {2, 0, 1, 65}},
            { 9666, new short[] {2, 0, 10, 66}},
            { 9667, new short[] {2, 0, 30, 67}},
            { 9668, new short[] {2, 0, 100, 68}},
            { 9669, new short[] {2, 0, 300, 69}},
            { 9670, new short[] {2, 0, 1, 70}},
            { 9671, new short[] {2, 0, 10, 71}},
            { 9672, new short[] {2, 0, 30, 72}},
            { 9673, new short[] {2, 0, 100, 73}},
            { 9674, new short[] {2, 0, 300, 74}},

            //USABLE EXTENSIONS
            { 9675, new short[] {4, 0, 0, 0}},
            { 9676, new short[] {4, 0, 0, 0}},
            { 9677, new short[] {4, 0, 0, 0}},
            { 9678, new short[] {4, 0, 0, 0}},

            //Family Extensions (to shop)  (TYPE, Lvl minimum, Subtype(for duplicates))
            { 9679, new short[] {5, 3, 11, }},
            { 9680, new short[] {5, 6, 11, }},
            { 9681, new short[] {5, 10, 11, }},
            { 9682, new short[] {5, 13, 11, }},
            { 9683, new short[] {5, 16, 11, }},
            { 9684, new short[] {5, 20, 11, }},
            { 9685, new short[] {5, 4, 12, }},
            { 9686, new short[] {5, 11, 12, }},
            { 9687, new short[] {5, 17, 12, }},
            { 9688, new short[] {5, 8, 13, }},
            { 9689, new short[] {5, 14, 13, }},
            { 9690, new short[] {5, 18, 13, }},
            { 9691, new short[] {5, 12, 14, }},
            { 9692, new short[] {5, 15, 14, }},
            { 9693, new short[] {5, 19, 14, }},
            { 9694, new short[] {5, 2, 15, }},
            { 9695, new short[] {5, 7, 15, }},
            { 9696, new short[] {5, 5, 16, }},
            { 9697, new short[] {5, 9, 16, }},


            { 9698, new short[] {3, 0, 17, }},
            { 9699, new short[] {3, 0, 17, }},
            { 9700, new short[] {3, 0, 17, }},
            { 9701, new short[] {3, 0, 17, }},
            { 9702, new short[] {3, 0, 17, }},
            { 9703, new short[] {3, 0, 17, }},
            { 9704, new short[] {3, 0, 18, }},
            { 9705, new short[] {3, 0, 18, }},
            { 9706, new short[] {3, 0, 18, }},
            { 9707, new short[] {3, 0, 19, }},
            { 9708, new short[] {3, 0, 19, }},
            { 9709, new short[] {3, 0, 19, }},
            { 9710, new short[] {3, 0, 20, }},
            { 9711, new short[] {3, 0, 20, }},
            { 9712, new short[] {3, 0, 20, }},
            { 9713, new short[] {3, 0, 21, }},
            { 9714, new short[] {3, 0, 21, }},
            { 9715, new short[] {3, 0, 22, }},
            { 9716, new short[] {3, 0, 22, }},
            //{ 9717, new short[] { }},
            //{ 9718, new short[] { }},
            //{ 9719, new short[] { }},
            //{ 9720, new short[] { }},
            //{ 9721, new short[] { }},
            //{ 9722, new short[] { }},
            //{ 9723, new short[] { }},
            //{ 9724, new short[] { }},
            //{ 9725, new short[] { }},
            //{ 9726, new short[] { }},
            //{ 9727, new short[] { }},
            //{ 9728, new short[] { }},
            //{ 9729, new short[] { }},
            //{ 9730, new short[] { }},
            //{ 9731, new short[] { }},

            //RESISTANCE BOOSTS
            { 9732, new short[] {3, 0, 23, }},
            { 9733, new short[] {3, 0, 23, }},
            { 9734, new short[] {3, 0, 23, }},
            { 9735, new short[] {3, 0, 23, }},
            { 9736, new short[] {3, 0, 23, }},
            { 9737, new short[] {3, 0, 24, }},
            { 9738, new short[] {3, 0, 24, }},
            { 9739, new short[] {3, 0, 24, }},
            { 9740, new short[] {3, 0, 24, }},
            { 9741, new short[] {3, 0, 24, }},
            { 9742, new short[] {3, 0, 25, }},
            { 9743, new short[] {3, 0, 25, }},
            { 9744, new short[] {3, 0, 25, }},
            { 9745, new short[] {3, 0, 25, }},
            { 9746, new short[] {3, 0, 25, }},
            { 9747, new short[] {3, 0, 26, }},
            { 9748, new short[] {3, 0, 26, }},
            { 9749, new short[] {3, 0, 26, }},
            { 9750, new short[] {3, 0, 26, }},
            { 9751, new short[] {3, 0, 26, }},
            { 9752, new short[] {1, 5, 1, 75}},
            { 9753, new short[] {1, 5, 1, 76}},
            { 9754, new short[] {1, 3, 10, 77}},
            { 9755, new short[] {1, 2, 10, 78}},
            { 9756, new short[] {1, 3, 5, 79}},


            //{ 9757, new short[] { }},
            //{ 9758, new short[] { }},
            //{ 9759, new short[] { }},
            //{ 9760, new short[] { }},
            //{ 9761, new short[] { }},
            //{ 9762, new short[] { }},
            //{ 9763, new short[] { }},
            //{ 9764, new short[] { }},
            { 9765, new short[] {1, 4, 5, 80}}, //Spirit King Daily
            { 9766, new short[] {1, 4, 5, 81}}, //Beast King Daily
            { 9767, new short[] {1, 4, 5, 82}}, //Belial Daily
            { 9768, new short[] {1, 4, 5, 83}}, //Paimon Daily
            { 9773, new short[] {1, 4, 5, 84}}, // Alzanor's Daily
            { 9774, new short[] {1, 4, 5, 85}}, // Valehir's Daily
            { 9775, new short[] {1, 4, 5, 86}}, // Asgoba's Daily
            //{ 9769, new short[] { }},
            //{ 9770, new short[] { }},
            //{ 9771, new short[] { }},
        };

        private static readonly Dictionary<short, int[]> MissionRewards = new Dictionary<short, int[]>
        {
            //DAILY MISSIONS
            {9604, new int[] {/*XPREWARD*/1000, /*EXTENSION REWARD*/ }},
            {9605, new int[] {3000, }},
            {9606, new int[] {5000, }},
            {9607, new int[] {1000, }},
            {9608, new int[] {1000, }},
            {9609, new int[] {1500, }},
            {9610, new int[] {1500, }},
            {9611, new int[] {2000, }},
            {9612, new int[] {2000, }},
            {9613, new int[] {2000, }},
            {9614, new int[] {2500, }},
            {9615, new int[] {2500, }},
            {9616, new int[] {3000, }},
            {9617, new int[] {3000, }},
            ////

            {9637, new int[] {10000, }},
            {9638, new int[] {30000, }},
            {9639, new int[] {50000, }},
            {9640, new int[] {100000, }},
            {9641, new int[] {200000, }},
            {9642, new int[] {5000, }},
            {9643, new int[] {25000, }},
            {9644, new int[] {50000, }},
            {9645, new int[] {100000, }},
            {9646, new int[] {150000, }},
            {9647, new int[] {5000, }},
            {9648, new int[] {25000, }},
            {9649, new int[] {50000, }},
            {9650, new int[] {100000, }},
            {9651, new int[] {150000, }},
            {9652, new int[] {50000, }},
            {9653, new int[] {100000, }},
            {9654, new int[] {500000, }},
            {9655, new int[] {10000, 9732}},
            {9656, new int[] {50000, 9733}},
            {9657, new int[] {100000, 9734}},
            {9658, new int[] {200000, 9735}},
            {9659, new int[] {300000, 9736}},
            {9660, new int[] {10000, 9737}},
            {9661, new int[] {50000, 9738}},
            {9662, new int[] {100000, 9739}},
            {9663, new int[] {200000, 9740}},
            {9664, new int[] {300000, 9741}},
            {9665, new int[] {10000, 9742}},
            {9666, new int[] {50000, 9743}},
            {9667, new int[] {100000, 9744}},
            {9668, new int[] {200000, 9745}},
            {9669, new int[] {300000, 9746}},
            {9670, new int[] {10000, 9747}},
            {9671, new int[] {50000, 9748}},
            {9672, new int[] {100000, 9749}},
            {9673, new int[] {200000, 9750}},
            {9674, new int[] {300000, 9751}},
            {9773, new int[] {10000, }}, // Add EXP like offi Alzanor's
            {9774, new int[] {10000, }},// Add EXP like offi Valehir's Daily
            {9775, new int[] {10000, }},// Add EXP like offi Asgoba's Daily
        };

        #endregion

        #region Methods

        public static int ExtensionReward(short key)
        {
            if (MissionRewards.TryGetValue(key, out int[] value) && value[1] != null)
                return value[1];
            else
                return 0;
        }

        public static short[]? GetMissValues(short key)
        {
            if (FamilySystemItems.TryGetValue(key, out short[] value))
                return value;
            else
                return null;
        }

        public static bool IsBase(short key)
        {
            switch (key)
            {
                case 9618:
                case 9637:
                case 9642:
                case 9647:
                case 9652:
                case 9655:
                case 9660:
                case 9665:
                case 9670:
                case 9679:
                case 9685:
                case 9688:
                case 9691:
                case 9694:
                case 9696:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsDaily(short key)
        {
            if (FamilySystemItems.TryGetValue(key, out short[] value))
            {
                return value[0] == 1;
            }
            return false;
        }

        public static int XpReward(short key)
        {
            if (MissionRewards.TryGetValue(key, out int[] value) && value[0] != null)
                return value[0];
            else
                return 0;
        }

        /// <summary>
        /// Returns data for 1st tab in $"fmi " packet (DailyMissions + MonthlyMissions)
        /// </summary>
        /// <returns></returns>
        internal static List<KeyValuePair<short, short[]>> GetFmiPrepSortedData1()
        {
            List<KeyValuePair<short, short[]>> list = new List<KeyValuePair<short, short[]>>();

            foreach (var value in FamilySystemItems.Where(s => s.Value[0].Equals(0) || s.Value[0].Equals(1)).OrderBy(s => s.Key))
            {
                list.Add(value);
            }

            return list;
        }

        /// <summary>
        /// Returns data for 2nd tab in $"fmi " packet (Normal missions)
        /// </summary>
        /// <returns></returns>
        internal static List<KeyValuePair<short, short[]>> GetFmiPrepSortedData2()
        {
            List<KeyValuePair<short, short[]>> list = new List<KeyValuePair<short, short[]>>();

            foreach (var value in FamilySystemItems.Where(s => s.Value[0].Equals(2)).OrderBy(s => s.Key))
            {
                list.Add(value);
            }

            return list;
        }

        internal static List<KeyValuePair<short, short[]>> GetFmpPrepSortedData()
        {
            List<KeyValuePair<short, short[]>> list = new List<KeyValuePair<short, short[]>>();

            foreach (var value in FamilySystemItems.Where(s => s.Value[0].Equals(3) || s.Value[0].Equals(4) || s.Value[0].Equals(0) || s.Value[0].Equals(5)).OrderByDescending(s => s.Key))
            {
                list.Add(value);
            }

            return list;
        }

        #endregion
    }
}
