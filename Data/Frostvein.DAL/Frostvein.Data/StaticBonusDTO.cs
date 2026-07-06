using Frostvein.Domain;
using System;

namespace Frostvein.Data
{
    [Serializable]
    public class StaticBonusDTO
    {
        #region Properties

        public long CharacterId { get; set; }

        public DateTime DateEnd { get; set; }

        public long StaticBonusId { get; set; }

        public StaticBonusType StaticBonusType { get; set; }

        #endregion
    }
}