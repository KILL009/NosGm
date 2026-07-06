using System;

namespace Frostvein.Data
{
    [Serializable]
    public class MapTypeDTO
    {
        #region Properties

        public short MapTypeId { get; set; }

        public string MapTypeName { get; set; }

        public short PotionDelay { get; set; }

        public long? RespawnMapTypeId { get; set; }

        public long? ReturnMapTypeId { get; set; }

        #endregion
    }
}