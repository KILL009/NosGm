using NosGm.Domain;
using System;

namespace NosGm.Data
{
    [Serializable]
    public class FamilyLogDTO
    {
        #region Properties

        public long FamilyId { get; set; }

        public string FamilyLogData { get; set; }

        public long FamilyLogId { get; set; }

        public FamilyLogType FamilyLogType { get; set; }

        public DateTime Timestamp { get; set; }

        #endregion
    }
}