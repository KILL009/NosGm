using System;

namespace Frostvein.Data
{
    [Serializable]
    public class StaticBuffDTO
    {
        #region Properties

        public short CardId { get; set; }

        public long CharacterId { get; set; }

        public int RemainingTime { get; set; }

        public long StaticBuffId { get; set; }

        #endregion
    }
}