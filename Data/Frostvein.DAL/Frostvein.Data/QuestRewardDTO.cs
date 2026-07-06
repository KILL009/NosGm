using System;

namespace Frostvein.Data
{
    [Serializable]
    public class QuestRewardDTO
    {
        #region Properties

        public int Amount { get; set; }

        public int Data { get; set; }

        public byte Design { get; set; }

        public long QuestId { get; set; }

        public long QuestRewardId { get; set; }

        public byte Rarity { get; set; }

        public byte RewardType { get; set; }

        public byte Upgrade { get; set; }

        #endregion
    }
}