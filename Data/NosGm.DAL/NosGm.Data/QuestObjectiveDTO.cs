using System;

namespace NosGm.Data
{
    [Serializable]
    public class QuestObjectiveDTO
    {
        #region Properties

        public int? Data { get; set; }

        public int? DropRate { get; set; }

        public int? Objective { get; set; }

        public byte ObjectiveIndex { get; set; }

        public int QuestId { get; set; }

        public int QuestObjectiveId { get; set; }

        public int? SpecialData { get; set; }

        #endregion
    }
}