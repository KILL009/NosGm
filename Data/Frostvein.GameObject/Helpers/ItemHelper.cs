namespace Frostvein.GameObject.Helpers
{
    public class ItemHelper
    {
        #region Properties

        public static PassiveSkillHelper Instance => _instance ?? (_instance = new PassiveSkillHelper());

        #endregion

        #region Members

        public static readonly byte[] UpFairyRate = new byte[] { 100, 100, 100, 100, 100, 100, 100, 100, 100 };
        public static readonly byte[] DestroyFairyRate = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public static readonly byte[] FailedFairyRate = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };


        public static readonly byte[] BuyCraftRareRate = new byte[] { 100, 100, 63, 48, 35, 24, 14, 6 };

        public static readonly byte[] ItemUpgradeFailRate = new byte[] { 0, 0, 0, 5, 20, 40, 60, 70, 80, 85 };
        public static readonly byte[] ItemUpgradeFixRate = new byte[] { 0, 0, 10, 15, 20, 20, 20, 20, 15, 14 };
        public static readonly byte[] ItemUpgradeSuccess = new byte[] { 100, 100, 90, 80, 60, 40, 20, 10, 5, 1 };

        public static readonly byte[] R8ItemUpgradeFailRate = new byte[] { 50, 50, 45, 45, 50, 60, 70, 70, 80, 80 };
        public static readonly byte[] R8ItemUpgradeFixRate = new byte[] { 0, 0, 10, 15, 20, 20, 20, 25, 17, 19 };
        public static readonly byte[] R8UpgradeSuccess = new byte[] { 50, 50, 45, 40, 30, 20, 10, 5, 3, 1 };

        public static readonly byte[] RareRate = new byte[] { 100, 80, 70, 50, 30, 15, 5, 1 };
        public static readonly byte[] RarifyRate = new byte[] { 50, 70, 60, 40, 30, 20, 15, 10, 7, 4, 2 };        

        public static readonly byte[] SpDestroyRate = new byte[] { 0, 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 70 };
        public static readonly byte[] SpUpFailRate = new byte[] { 20, 25, 30, 40, 50, 60, 65, 70, 75, 80, 90, 93, 95, 97, 99 };
        public static readonly byte[] SpUpSuccess = new byte[] { 80, 75, 70, 60, 50, 40, 35, 30, 25, 20, 15, 10, 7, 3, 1 };
        public static readonly short[] SpPity = new short[] { 3, 5, 5, 5, 6, 7, 8, 10, 12, 15, 30, 45, 60, 100, 300 };
        public static readonly short[] ItemPity = new short[] { 3, 3, 3, 3, 4, 7, 15, 60, 300, 600 };
        private static PassiveSkillHelper _instance;

        public static byte FusionPspNextUpgrade(byte upgradeValue, int itemToUpgrade)
        {
            byte nextUpgrade = (byte)(upgradeValue + itemToUpgrade);

            if (nextUpgrade > 20 && itemToUpgrade < 19)
            {
                nextUpgrade = 20;
            }
            else if (nextUpgrade > 40 && itemToUpgrade < 39)
            {
                nextUpgrade = 40;
            }
            else if (nextUpgrade > 60 && itemToUpgrade < 59)
            {
                nextUpgrade = 60;
            }
            else if (nextUpgrade > 80 && itemToUpgrade < 79)
            {
                nextUpgrade = 80;
            }
            else if (nextUpgrade > 99 && itemToUpgrade <= 98)
            {
                nextUpgrade = 99;
            }
            else if (nextUpgrade > 100)
            {
                nextUpgrade = 100;
            }

            return nextUpgrade;
        }

        #endregion
    }
}