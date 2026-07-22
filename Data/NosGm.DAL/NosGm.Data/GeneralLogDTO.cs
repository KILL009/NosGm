using System;

namespace NosGm.Data
{
    [Serializable]
    public class GeneralLogDTO
    {
        #region Properties

        public long? AccountId { get; set; }

        public long? CharacterId { get; set; }

        public string IpAddress { get; set; }

        public string LogData { get; set; }

        public long LogId { get; set; }

        public string LogType { get; set; }

        public DateTime Timestamp { get; set; }

        #endregion
    }
}