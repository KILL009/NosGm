namespace Frostvein.Data
{
    public class BattlePassAccountLogDTO
    {
        public long BpAccountLogId { get; set; }

        public long AccountId { get; set; }

        public byte Level { get; set; }

        public bool IsPremium { get; set; }
    }
}
