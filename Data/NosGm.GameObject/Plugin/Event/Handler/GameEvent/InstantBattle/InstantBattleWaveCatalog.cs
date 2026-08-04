using System;
using System.Collections.Generic;
using NosGm.GameObject.Event;

namespace NosGm.GameObject.Plugin.Event
{
    internal static class InstantBattleWaveCatalog
    {
        public static List<Tuple<short, int, short, short>> GetDrops(
            Map map,
            short levelBracket,
            int wave)
        {
            var drops = new List<Tuple<short, int, short, short>>();

            switch (levelBracket)
            {
                case 1:
                    switch (wave)
                    {
                        case 0:
                            AddDrops(drops, map, 1046, 15, 500);
                            AddDrops(drops, map, 2027, 8, 5);
                            AddDrops(drops, map, 2018, 5, 5);
                            AddDrops(drops, map, 180, 5, 1);
                            break;
                        case 1:
                            AddDrops(drops, map, 1046, 15, 1000);
                            AddDrops(drops, map, 1002, 8, 3);
                            AddDrops(drops, map, 1005, 16, 3);
                            AddDrops(drops, map, 181, 5, 1);
                            break;
                        case 2:
                            AddDrops(drops, map, 1046, 15, 1500);
                            AddDrops(drops, map, 1002, 10, 5);
                            AddDrops(drops, map, 1005, 10, 5);
                            break;
                        case 3:
                            AddDrops(drops, map, 1046, 15, 2000);
                            AddDrops(drops, map, 1003, 10, 5);
                            AddDrops(drops, map, 1006, 10, 5);
                            break;
                    }
                    break;

                case 40:
                case 50:
                    switch (wave)
                    {
                        case 0:
                            AddDrops(drops, map, 1046, 15, 1500);
                            AddDrops(drops, map, 1008, 5, 3);
                            AddDrops(drops, map, 180, 5, 1);
                            break;
                        case 1:
                            AddDrops(drops, map, 1046, 15, 2000);
                            AddDrops(drops, map, 1008, 8, 3);
                            AddDrops(drops, map, 181, 5, 1);
                            break;
                        case 2:
                            AddDrops(drops, map, 1046, 15, 2500);
                            AddDrops(drops, map, 1009, 10, 3);
                            AddDrops(drops, map, 1246, 5, 1);
                            AddDrops(drops, map, 1247, 5, 1);
                            break;
                        case 3:
                            AddDrops(drops, map, 1046, 15, 3000);
                            AddDrops(drops, map, 1009, 10, 3);
                            AddDrops(drops, map, 1248, 5, 1);
                            break;
                    }
                    break;

                case 60:
                    switch (wave)
                    {
                        case 0:
                            AddDrops(drops, map, 1046, 15, 3000);
                            AddDrops(drops, map, 1010, 8, 4);
                            AddDrops(drops, map, 1246, 5, 1);
                            break;
                        case 1:
                            AddDrops(drops, map, 1046, 15, 4000);
                            AddDrops(drops, map, 1010, 10, 3);
                            AddDrops(drops, map, 1247, 5, 1);
                            break;
                        case 2:
                            AddDrops(drops, map, 1046, 15, 5000);
                            AddDrops(drops, map, 1010, 10, 13);
                            AddDrops(drops, map, 1246, 8, 1);
                            AddDrops(drops, map, 1247, 8, 1);
                            break;
                        case 3:
                            AddDrops(drops, map, 1046, 15, 7000);
                            AddDrops(drops, map, 1011, 13, 5);
                            AddDrops(drops, map, 1029, 5, 1);
                            AddDrops(drops, map, 1248, 13, 1);
                            break;
                    }
                    break;

                case 70:
                    switch (wave)
                    {
                        case 0:
                            AddDrops(drops, map, 1046, 15, 3000);
                            AddDrops(drops, map, 1010, 8, 3);
                            AddDrops(drops, map, 1246, 5, 1);
                            break;
                        case 1:
                            AddDrops(drops, map, 1046, 15, 4000);
                            AddDrops(drops, map, 1010, 15, 4);
                            AddDrops(drops, map, 1247, 10, 1);
                            break;
                        case 2:
                            AddDrops(drops, map, 1046, 15, 5000);
                            AddDrops(drops, map, 1010, 13, 5);
                            AddDrops(drops, map, 1246, 13, 1);
                            AddDrops(drops, map, 1247, 13, 1);
                            break;
                        case 3:
                            AddDrops(drops, map, 1046, 15, 7000);
                            AddDrops(drops, map, 1011, 13, 5);
                            AddDrops(drops, map, 1248, 13, 1);
                            AddDrops(drops, map, 1029, 5, 1);
                            break;
                    }
                    break;

                case 80:
                    switch (wave)
                    {
                        case 0:
                            AddDrops(drops, map, 1046, 15, 10000);
                            AddDrops(drops, map, 1011, 15, 5);
                            AddDrops(drops, map, 1246, 15, 1);
                            break;
                        case 1:
                            AddDrops(drops, map, 1046, 15, 12000);
                            AddDrops(drops, map, 1011, 15, 5);
                            AddDrops(drops, map, 1247, 15, 1);
                            break;
                        case 2:
                            AddDrops(drops, map, 1046, 15, 15000);
                            AddDrops(drops, map, 1011, 20, 5);
                            AddDrops(drops, map, 1246, 15, 1);
                            AddDrops(drops, map, 1247, 15, 1);
                            break;
                        case 3:
                            AddDrops(drops, map, 1046, 30, 20000);
                            AddDrops(drops, map, 1011, 30, 5);
                            AddDrops(drops, map, 1030, 30, 1);
                            AddDrops(drops, map, 2282, 12, 3);
                            break;
                    }
                    break;
            }

            return drops;
        }

        public static List<MonsterToSummon> GetMonsters(
            Map map,
            short levelBracket,
            int wave)
        {
            var monsters = new List<MonsterToSummon>();

            switch (levelBracket)
            {
                case 1:
                    switch (wave)
                    {
                        case 0:
                            AddMonsters(monsters, map, 1, 16, true);
                            AddMonsters(monsters, map, 58, 15, true);
                            AddMonsters(monsters, map, 105, 16, true);
                            AddMonsters(monsters, map, 107, 15, true);
                            AddMonsters(monsters, map, 108, 8, true);
                            AddMonsters(monsters, map, 111, 15, true);
                            AddMonsters(monsters, map, 136, 15, true);
                            break;
                        case 1:
                            AddMonsters(monsters, map, 194, 15, true);
                            AddMonsters(monsters, map, 114, 15, true);
                            AddMonsters(monsters, map, 99, 15, true);
                            AddMonsters(monsters, map, 39, 15, true);
                            AddMonsters(monsters, map, 2, 16, true);
                            break;
                        case 2:
                            AddMonsters(monsters, map, 140, 15, true);
                            AddMonsters(monsters, map, 100, 15, true);
                            AddMonsters(monsters, map, 81, 15, true);
                            AddMonsters(monsters, map, 12, 15, true);
                            AddMonsters(monsters, map, 4, 16, true);
                            break;
                        case 3:
                            AddMonsters(monsters, map, 115, 15, true);
                            AddMonsters(monsters, map, 112, 15, true);
                            AddMonsters(monsters, map, 110, 15, true);
                            AddMonsters(monsters, map, 14, 15, true);
                            AddMonsters(monsters, map, 5, 16, true);
                            break;
                        case 4:
                            AddMonsters(monsters, map, 979, 1, true);
                            AddMonsters(monsters, map, 167, 15, true);
                            AddMonsters(monsters, map, 137, 10, true);
                            AddMonsters(monsters, map, 22, 15, false);
                            AddMonsters(monsters, map, 17, 8, true);
                            AddMonsters(monsters, map, 16, 16, true);
                            break;
                    }
                    break;

                // The legacy catalog called this bracket 30 even though the join
                // table routes levels 40-49 as bracket 40. Keeping the public key at
                // 40 restores the missing waves for those players.
                case 40:
                    switch (wave)
                    {
                        case 0:
                            AddMonsters(monsters, map, 120, 15, true);
                            AddMonsters(monsters, map, 151, 15, true);
                            AddMonsters(monsters, map, 149, 15, true);
                            AddMonsters(monsters, map, 139, 15, true);
                            AddMonsters(monsters, map, 73, 16, true);
                            break;
                        case 1:
                            AddMonsters(monsters, map, 152, 15, true);
                            AddMonsters(monsters, map, 147, 15, true);
                            AddMonsters(monsters, map, 104, 15, true);
                            AddMonsters(monsters, map, 62, 15, true);
                            AddMonsters(monsters, map, 8, 16, true);
                            break;
                        case 2:
                            AddMonsters(monsters, map, 153, 15, true);
                            AddMonsters(monsters, map, 132, 15, true);
                            AddMonsters(monsters, map, 86, 15, true);
                            AddMonsters(monsters, map, 76, 15, true);
                            AddMonsters(monsters, map, 68, 16, true);
                            break;
                        case 3:
                            AddMonsters(monsters, map, 134, 15, true);
                            AddMonsters(monsters, map, 91, 15, true);
                            AddMonsters(monsters, map, 133, 15, true);
                            AddMonsters(monsters, map, 70, 15, true);
                            AddMonsters(monsters, map, 89, 16, true);
                            break;
                        case 4:
                            AddMonsters(monsters, map, 154, 15, true);
                            AddMonsters(monsters, map, 200, 15, true);
                            AddMonsters(monsters, map, 77, 8, true);
                            AddMonsters(monsters, map, 217, 15, true);
                            AddMonsters(monsters, map, 724, 1, true);
                            break;
                    }
                    break;

                case 50:
                    switch (wave)
                    {
                        case 0:
                            AddMonsters(monsters, map, 134, 15, true);
                            AddMonsters(monsters, map, 91, 15, true);
                            AddMonsters(monsters, map, 89, 15, true);
                            AddMonsters(monsters, map, 77, 15, true);
                            AddMonsters(monsters, map, 71, 16, true);
                            break;
                        case 1:
                            AddMonsters(monsters, map, 217, 15, true);
                            AddMonsters(monsters, map, 200, 15, true);
                            AddMonsters(monsters, map, 154, 15, true);
                            AddMonsters(monsters, map, 92, 15, true);
                            AddMonsters(monsters, map, 79, 16, true);
                            break;
                        case 2:
                            AddMonsters(monsters, map, 235, 15, true);
                            AddMonsters(monsters, map, 226, 15, true);
                            AddMonsters(monsters, map, 214, 15, true);
                            AddMonsters(monsters, map, 204, 15, true);
                            AddMonsters(monsters, map, 201, 15, true);
                            break;
                        case 3:
                            AddMonsters(monsters, map, 249, 15, true);
                            AddMonsters(monsters, map, 236, 15, true);
                            AddMonsters(monsters, map, 227, 15, true);
                            AddMonsters(monsters, map, 218, 15, true);
                            AddMonsters(monsters, map, 202, 15, true);
                            break;
                        case 4:
                            AddMonsters(monsters, map, 583, 1, true);
                            AddMonsters(monsters, map, 400, 13, true);
                            AddMonsters(monsters, map, 255, 8, true);
                            AddMonsters(monsters, map, 253, 13, true);
                            AddMonsters(monsters, map, 251, 10, true);
                            AddMonsters(monsters, map, 205, 14, true);
                            break;
                    }
                    break;

                case 60:
                    switch (wave)
                    {
                        case 0:
                            AddMonsters(monsters, map, 242, 12, true);
                            AddMonsters(monsters, map, 234, 12, true);
                            AddMonsters(monsters, map, 215, 12, true);
                            AddMonsters(monsters, map, 207, 12, true);
                            AddMonsters(monsters, map, 202, 13, true);
                            break;
                        case 1:
                            AddMonsters(monsters, map, 402, 12, true);
                            AddMonsters(monsters, map, 253, 12, true);
                            AddMonsters(monsters, map, 237, 12, true);
                            AddMonsters(monsters, map, 216, 12, true);
                            AddMonsters(monsters, map, 205, 13, true);
                            break;
                        case 2:
                            AddMonsters(monsters, map, 402, 12, true);
                            AddMonsters(monsters, map, 243, 12, true);
                            AddMonsters(monsters, map, 228, 12, true);
                            AddMonsters(monsters, map, 255, 12, true);
                            AddMonsters(monsters, map, 205, 13, true);
                            break;
                        case 3:
                            AddMonsters(monsters, map, 268, 12, true);
                            AddMonsters(monsters, map, 255, 12, true);
                            AddMonsters(monsters, map, 254, 12, true);
                            AddMonsters(monsters, map, 174, 12, true);
                            AddMonsters(monsters, map, 172, 13, true);
                            break;
                        case 4:
                            AddMonsters(monsters, map, 725, 1, true);
                            AddMonsters(monsters, map, 407, 12, true);
                            AddMonsters(monsters, map, 272, 12, true);
                            AddMonsters(monsters, map, 261, 12, true);
                            AddMonsters(monsters, map, 256, 12, true);
                            AddMonsters(monsters, map, 275, 13, true);
                            break;
                    }
                    break;

                case 70:
                    switch (wave)
                    {
                        case 0:
                            AddMonsters(monsters, map, 402, 15, true);
                            AddMonsters(monsters, map, 253, 15, true);
                            AddMonsters(monsters, map, 237, 15, true);
                            AddMonsters(monsters, map, 216, 15, true);
                            AddMonsters(monsters, map, 205, 15, true);
                            break;
                        case 1:
                            AddMonsters(monsters, map, 402, 15, true);
                            AddMonsters(monsters, map, 243, 15, true);
                            AddMonsters(monsters, map, 228, 15, true);
                            AddMonsters(monsters, map, 225, 15, true);
                            AddMonsters(monsters, map, 205, 15, true);
                            break;
                        case 2:
                            AddMonsters(monsters, map, 255, 15, true);
                            AddMonsters(monsters, map, 254, 15, true);
                            AddMonsters(monsters, map, 251, 15, true);
                            AddMonsters(monsters, map, 174, 15, true);
                            AddMonsters(monsters, map, 172, 15, true);
                            break;
                        case 3:
                            AddMonsters(monsters, map, 407, 15, true);
                            AddMonsters(monsters, map, 272, 15, true);
                            AddMonsters(monsters, map, 261, 15, true);
                            AddMonsters(monsters, map, 257, 15, true);
                            AddMonsters(monsters, map, 256, 15, true);
                            break;
                        case 4:
                            AddMonsters(monsters, map, 748, 1, true);
                            AddMonsters(monsters, map, 444, 13, true);
                            AddMonsters(monsters, map, 439, 13, true);
                            AddMonsters(monsters, map, 275, 13, true);
                            AddMonsters(monsters, map, 274, 13, true);
                            AddMonsters(monsters, map, 273, 13, true);
                            AddMonsters(monsters, map, 163, 13, true);
                            break;
                    }
                    break;

                case 80:
                    switch (wave)
                    {
                        case 0:
                            AddMonsters(monsters, map, 1007, 15, true);
                            AddMonsters(monsters, map, 1003, 15, false);
                            AddMonsters(monsters, map, 1002, 15, true);
                            AddMonsters(monsters, map, 1001, 15, true);
                            AddMonsters(monsters, map, 1000, 16, true);
                            break;
                        case 1:
                            AddMonsters(monsters, map, 1199, 15, true);
                            AddMonsters(monsters, map, 1198, 15, true);
                            AddMonsters(monsters, map, 1197, 15, true);
                            AddMonsters(monsters, map, 1196, 15, true);
                            AddMonsters(monsters, map, 1123, 16, true);
                            break;
                        case 2:
                            AddMonsters(monsters, map, 1305, 15, true);
                            AddMonsters(monsters, map, 1304, 15, true);
                            AddMonsters(monsters, map, 1303, 15, true);
                            AddMonsters(monsters, map, 1302, 15, true);
                            AddMonsters(monsters, map, 1194, 16, true);
                            break;
                        case 3:
                            AddMonsters(monsters, map, 1902, 15, true);
                            AddMonsters(monsters, map, 1901, 15, true);
                            AddMonsters(monsters, map, 1900, 15, true);
                            AddMonsters(monsters, map, 1045, 15, true);
                            AddMonsters(monsters, map, 1043, 15, true);
                            AddMonsters(monsters, map, 1042, 16, true);
                            break;
                        case 4:
                            AddMonsters(monsters, map, 637, 1, true);
                            AddMonsters(monsters, map, 1903, 13, true);
                            AddMonsters(monsters, map, 1053, 13, true);
                            AddMonsters(monsters, map, 1051, 13, true);
                            AddMonsters(monsters, map, 1049, 13, true);
                            AddMonsters(monsters, map, 1048, 13, true);
                            AddMonsters(monsters, map, 1047, 13, true);
                            break;
                    }
                    break;
            }

            return monsters;
        }

        private static void AddDrops(
            ICollection<Tuple<short, int, short, short>> destination,
            Map map,
            short itemVNum,
            int count,
            int amount)
        {
            for (int index = 0; index < count; index++)
            {
                MapCell cell = map.GetRandomPosition();
                if (cell != null)
                {
                    destination.Add(
                        new Tuple<short, int, short, short>(
                            itemVNum,
                            amount,
                            cell.X,
                            cell.Y));
                }
            }
        }

        private static void AddMonsters(
            ICollection<MonsterToSummon> destination,
            Map map,
            short monsterVNum,
            int count,
            bool moving)
        {
            destination.AddRange(
                map.GenerateMonsters(
                    monsterVNum,
                    (short)count,
                    moving,
                    new List<EventContainer>()));
        }

        private static void AddRange<T>(this ICollection<T> destination, IEnumerable<T> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (T item in source)
            {
                destination.Add(item);
            }
        }
    }
}
