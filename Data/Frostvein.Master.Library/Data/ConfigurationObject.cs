using System;

namespace Frostvein.Master.Library.Data
{
    [Serializable]
    public class ConfigurationObject
    {
        public long MaxGold { get; set; } = 2000000000L;

        public DateTime TimeExpBuff { get; set; } = DateTime.Now.AddHours(-2);

        public DateTime TimeGoldBuff { get; set; } = DateTime.Now.AddHours(-2);
    }
}
