using NosGm.Domain;

namespace NosGm.Data
{
    public class BattlePassQuestDTO
    {
        public long BpQuestId { get; set; }

        public BpQuestType BpQuestType { get; set; }

        public BpTimeType BpTimeType { get; set; }

        public int Amount { get; set; }

        public int Points { get; set; }

        public bool IsPremium { get; set; }
    }
}