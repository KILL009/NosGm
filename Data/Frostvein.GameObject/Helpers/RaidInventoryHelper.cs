using System.Collections.Generic;

namespace Frostvein.GameObject.Helpers
{
    public static class RaidInventoryHelper
    {
        private static readonly HashSet<short> AllowedItemVNums =
            new HashSet<short>
            {
                302,
                1094,
                1127,
                1128,
                1129,
                1130,
                1131,
                1150,
                1195,
                1226,
                1234,
                1371,
                1892,
                1916,
                4083,
                5109,
                5140,
                5500,
                5512,
                5730,
                5734
            };

        public static bool IsAllowed(short itemVNum)
        {
            return AllowedItemVNums.Contains(itemVNum);
        }

        public static bool IsAllowed(ItemInstance item)
        {
            return item != null &&
                   item.Amount > 0 &&
                   IsAllowed(item.ItemVNum);
        }
    }
}