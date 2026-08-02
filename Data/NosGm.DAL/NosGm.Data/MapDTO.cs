using System;

namespace NosGm.Data
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

        public MapDTO Clone()
        {
            var clone = (MapDTO)this.MemberwiseClone();
            if (this.Data != null)
            {
                clone.Data = new byte[this.Data.Length];
                Array.Copy(this.Data, clone.Data, this.Data.Length);
            }
            return clone;
        }
    }
}