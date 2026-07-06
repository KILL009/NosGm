using System;

namespace Frostvein.Data
{
    [Serializable]
    public class MapDTO : IMapDTO
    {
        #region Properties

        public byte[] Data { get; set; }

        public short GridMapId { get; set; }

        public short MapId { get; set; }

        public int Music { get; set; }

        public string Name { get; set; }

        public bool ShopAllowed { get; set; }

        public byte XpRate { get; set; }

        #endregion
    }
}