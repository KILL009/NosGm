using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosGm.DAL.EF
{
    public class Quest
    {
        #region Properties

        public int? DialogNpcId { get; set; }

        public int? DialogNpcVNum { get; set; }

        public int? EndDialogId { get; set; }

        public int InfoId { get; set; }

        public bool IsDaily { get; set; }

        public bool CanBeDoneOnlyOnce { get; set; }

        public int LevelMax { get; set; }

        public int LevelMin { get; set; }

        public long? NextQuestId { get; set; }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long QuestId { get; set; }

        public int QuestType { get; set; }

        public int? StartDialogId { get; set; }

        public short? TargetMap { get; set; }

        public short? TargetX { get; set; }

        public short? TargetY { get; set; }

        #endregion
    }
}