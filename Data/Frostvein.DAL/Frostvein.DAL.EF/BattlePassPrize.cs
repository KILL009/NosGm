using System.ComponentModel.DataAnnotations;

namespace Frostvein.DAL.EF
{
    public class BattlePassPrize
    {
        [Key]
        public int BpPrizeId { get; set; }

        public byte Level { get; set; }

        public short ItemVNum { get; set; }

        public short Amount { get; set; }

        public short ItemVNumPremium { get; set; }

        public short AmountPremium { get; set; }

        public bool IsSpecial { get; set; }
    }
}
