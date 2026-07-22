using System.ComponentModel.DataAnnotations;

namespace NosGm.DAL.EF
{
    public class BattlePassQuestProgress
    {
        [Key]
        public long BpQuestProgressId { get; set; }

        public long AccountId { get; set; }

        public virtual BattlePassQuest BattlePassQuest { get; set; }

        public long BpQuestId { get; set; }

        public int Amount { get; set; }

        public bool Completed { get; set; }
    }
}
