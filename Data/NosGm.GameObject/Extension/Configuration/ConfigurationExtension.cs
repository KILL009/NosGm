namespace NosGm.GameObject.Extension
{
    public static class ConfigurationExtension
    {
        public static readonly byte[] RareRate = new byte[] { 80, 70, 60, 50, 30, 15, 5, 1 };

        public static readonly byte[] BuyCraftRareRate = new byte[] { 100, 100, 63, 48, 35, 24, 14, 6 };

        public static readonly byte[] RarifyRate = new byte[] { 80, 70, 60, 40, 30, 15, 10, 5, 3, 2, 1 };

        public static readonly byte[] SpUpFailRate = new byte[] { 20, 25, 30, 40, 50, 60, 65, 70, 75, 80, 90, 93, 95, 97, 98 };

        public static readonly byte[] SpDestroyRate = new byte[] { 0, 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 70 };

        public static readonly byte[] ItemUpgradeFixRate = new byte[] { 0, 0, 10, 15, 20, 20, 20, 5, 5, 4 };

        public static readonly byte[] ItemUpgradeFailRate = new byte[] { 0, 0, 0, 5, 20, 40, 60, 80, 90, 95 };

        public static readonly byte[] R8ItemUpgradeFixRate = new byte[] { 50, 40, 70, 65, 80, 90, 95, 97, 98, 99 };

        public static readonly byte[] R8ItemUpgradeFailRate = new byte[] { 50, 40, 60, 50, 60, 70, 75, 77, 83, 89 };
    }
}