using System.ComponentModel.DataAnnotations;

namespace Frostvein.DAL.EF
{
    public class BattlePassAccountLog
    {
        [Key]
        public long BpAccountLogId { get; set; }

        public long AccountId { get; set; }

        public byte Level { get; set; }

        public bool IsPremium { get; set; }
    }
}
