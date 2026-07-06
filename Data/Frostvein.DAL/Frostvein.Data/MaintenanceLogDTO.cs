using System;

namespace Frostvein.Data
{
    [Serializable]
    public class MaintenanceLogDTO
    {
        #region Properties

        public DateTime DateEnd { get; set; }

        public DateTime DateStart { get; set; }

        public long LogId { get; set; }

        public string Reason { get; set; }

        #endregion
    }
}