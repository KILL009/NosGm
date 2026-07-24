using System;
using System.ComponentModel.DataAnnotations;

namespace NosGm.DAL.EF
{
    public class GeneralLog
    {
        #region Properties

        public virtual Account Account { get; set; }

        public long? AccountId { get; set; }

        public virtual Character Character { get; set; }

        public long? CharacterId { get; set; }

        [MaxLength(255)] public string IpAddress { get; set; }

        [MaxLength(255)] public string LogData { get; set; }

        [Key] public long LogId { get; set; }

        [MaxLength(64)] public string LogType { get; set; }

        public DateTime Timestamp { get; set; }

        #endregion
    }
}
