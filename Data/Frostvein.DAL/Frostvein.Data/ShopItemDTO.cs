using System;

namespace Frostvein.Data
{
    [Serializable]
    public class ShopItemDTO
    {
        #region Properties

        public byte Color { get; set; }

        public short ItemVNum { get; set; }

        public sbyte Rare { get; set; }

        public int ShopId { get; set; }

        public int ShopItemId { get; set; }

        public byte Slot { get; set; }

        public byte Type { get; set; }

        public byte Upgrade { get; set; }

        #endregion
    }
}