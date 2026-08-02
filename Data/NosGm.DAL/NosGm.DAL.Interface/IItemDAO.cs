using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IItemDAO
    {
        #region Methods

        IEnumerable<ItemDTO> FindByName(string name);

        ItemDTO Insert(ItemDTO item);

        void Insert(IEnumerable<ItemDTO> items);

        IEnumerable<ItemDTO> LoadAll();

        CacheStatisticsSnapshot GetCacheStatistics();

        ItemDTO LoadById(short vNum);

        #endregion
    }
}