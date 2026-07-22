using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IShopDAO
    {
        #region Methods

        DeleteResult DeleteByNpcId(int mapNpcId);

        ShopDTO Insert(ShopDTO shop);

        void Insert(List<ShopDTO> shops);

        IEnumerable<ShopDTO> LoadAll();

        ShopDTO LoadById(int shopId);

        ShopDTO LoadByNpc(int mapNpcId);

        SaveResult Update(ref ShopDTO shop);

        #endregion
    }
}