using System.Collections.Generic;

namespace NosGm.GameObject
{
    public static class MappingHelper
    {
        #region Instantiation

        // intialize hardcode in waiting for better solution
        static MappingHelper() => GuriItemEffects = new Dictionary<int, int>
        {
            [859] = 1343,
            [860] = 1344,
            [861] = 1344,
            [875] = 1558,
            [876] = 1559,
            [877] = 1560,
            [878] = 1560,
            [879] = 1561,
            [880] = 1561
        };

        #endregion

        // effect items aka. fireworks

        #region Properties

        public static Dictionary<int, int> GuriItemEffects { get; }

        #endregion
    }
}