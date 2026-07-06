using System.Collections.Generic;

namespace Frostvein.GameObject
{
    public class MapShop
    {
        #region Instantiation

        public MapShop()
        {
            Items = new List<PersonalShopItem>();
            Sell = 0;
        }

        #endregion

        #region Properties

        public List<PersonalShopItem> Items { get; set; }

        public string Name { get; set; }

        public long OwnerId { get; set; }

        public long Sell { get; set; }

        #endregion
    }
}