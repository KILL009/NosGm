using System;

namespace NosGm.Data
{
    [Serializable]
    public class RespawnDTO
    {
        #region Properties

        public long CharacterId { get; set; }

        public short MapId { get; set; }

        public long RespawnId { get; set; }

        public long RespawnMapTypeId { get; set; }

        public short X { get; set; }

        public short Y { get; set; }

        #endregion
    }
}