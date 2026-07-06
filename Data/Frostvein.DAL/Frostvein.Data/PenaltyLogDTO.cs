using Frostvein.Domain;
using System;

namespace Frostvein.Data
{
    [Serializable]
    public class PenaltyLogDTO
    {
        #region Properties

        public long AccountId { get; set; }

        public string AdminName { get; set; }

        public DateTime DateEnd { get; set; }

        public DateTime DateStart { get; set; }

        public string IP { get; set; }

        public PenaltyType Penalty { get; set; }

        public int PenaltyLogId { get; set; }

        public string Reason { get; set; }

        #endregion
    }
}