namespace Frostvein.Data
{
    public class BattlePassPrizeDTO
    {
        public int BpPrizeId { get; set; }

        public byte Level { get; set; }

        public short ItemVNum { get; set; }

        public short Amount { get; set; }

        public short ItemVNumPremium { get; set; }

        public short AmountPremium { get; set; }

        public bool IsSpecial { get; set; }
    }
}