using System;

namespace Frostvein.Data
{
    [Serializable]
    public class ShopDTO
    {
        #region Properties

        public int MapNpcId { get; set; }

        public byte MenuType { get; set; }

        public string Name { get; set; }

        public int ShopId { get; set; }

        public byte ShopType { get; set; }

        #endregion
    }
}