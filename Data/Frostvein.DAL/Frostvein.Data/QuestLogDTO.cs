using System;

namespace Frostvein.Data
{
    [Serializable]
    public class QuestLogDTO
    {
        #region Properties

        public long CharacterId { get; set; }

        public long Id { get; set; }

        public string IpAddress { get; set; }

        public DateTime? LastDaily { get; set; }

        public long QuestId { get; set; }

        #endregion
    }
}