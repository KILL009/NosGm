using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
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