using Frostvein.Domain;
using System;

namespace Frostvein.Data
{
    [Serializable]
    public class TeleporterDTO
    {
        #region Properties

        public short Index { get; set; }

        public short MapId { get; set; }

        public int MapNpcId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public short TeleporterId { get; set; }

        public TeleporterType Type { get; set; }

        #endregion
    }
}