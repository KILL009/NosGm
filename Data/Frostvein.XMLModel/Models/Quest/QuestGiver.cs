using Frostvein.Domain;
using System;

namespace Frostvein.XMLModel.Models.Quest
{
    [Serializable]
    public class QuestGiver
    {
        #region Properties

        public byte MaximumLevel { get; set; }

        public byte MinimumLevel { get; set; }

        public long QuestGiverId { get; set; }

        public QuestGiverType Type { get; set; }

        #endregion
    }
}