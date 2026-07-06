using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IShopItemDAO
    {
        #region Methods

        DeleteResult DeleteById(int itemId);

        ShopItemDTO Insert(ShopItemDTO item);

        void Insert(List<ShopItemDTO> items);

        IEnumerable<ShopItemDTO> LoadAll();

        ShopItemDTO LoadById(int itemId);

        IEnumerable<ShopItemDTO> LoadByShopId(int shopId);

        #endregion
    }
}