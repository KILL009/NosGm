namespace Frostvein.Data
{
    public class BattlePassQuestProgressDTO
    {
        public long BpQuestProgressId { get; set; }

        public long AccountId { get; set; }

        public long BpQuestId { get; set; }

        public int Amount { get; set; }

        public bool Completed { get; set; }
    }
}