using Frostvein.Domain;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Frostvein.DAL.EF
{
    public class BattlePassQuest
    {
        public BattlePassQuest()
        {
            BattlePassQuestProgress = new HashSet<BattlePassQuestProgress>();
        }

        [Key]
        public long BpQuestId { get; set; }

        public virtual ICollection<BattlePassQuestProgress> BattlePassQuestProgress { get; set; }

        public BpQuestType BpQuestType { get; set; }

        public BpTimeType BpTimeType { get; set; }

        public int Amount { get; set; }

        public int Points { get; set; }

        public bool IsPremium { get; set; }
    }
}
