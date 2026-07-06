namespace Frostvein.GameObject.Helpers
{
    public class RewardsHelper
    {
        #region Members

        private static RewardsHelper _instance;

        #endregion

        #region Properties

        public static RewardsHelper Instance => _instance ?? (_instance = new RewardsHelper());

        #endregion

        #region Methods

        public int ArenaXpReward(byte characterLevel)

        {
            if (characterLevel <= 39)
            {
                // 25%
                return (int)(CharacterHelper.XPData[characterLevel] / 4);

            }

            if (characterLevel <= 55)
            {
                // 20%
                return (int)(CharacterHelper.XPData[characterLevel] / 5);
            }


            if (characterLevel <= 75)
            {
                // > 12%
                return (int)(CharacterHelper.XPData[characterLevel] / 7);
            }


            if (characterLevel <= 79)
            {
                // > 10%
                return (int)(CharacterHelper.XPData[characterLevel] / 10);
            }


            if (characterLevel <= 85)
            {
                // > 4%
                return (int)(CharacterHelper.XPData[characterLevel] / 25);
            }

            if (characterLevel <= 90)
            {
                // > 2%
                return (int)(CharacterHelper.XPData[characterLevel] / 35);
            }

            if (characterLevel <= 93)
            {
                // idk
                return (int)(CharacterHelper.XPData[characterLevel] / 50);
            }

            if (characterLevel <= 99)
            {
                // idk
                return (int)(CharacterHelper.XPData[characterLevel] / 500);
            }

            return 0;
        }


        #endregion
    }
}