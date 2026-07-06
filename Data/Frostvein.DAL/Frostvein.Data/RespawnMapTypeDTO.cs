using System;

namespace Frostvein.Data
{
    [Serializable]
    public class RespawnMapTypeDTO
    {
        #region Properties

        public short DefaultMapId { get; set; }

        public short DefaultX { get; set; }

        public short DefaultY { get; set; }

        public string Name { get; set; }

        public long RespawnMapTypeId { get; set; }

        #endregion
    }
}