using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IMapDAO
    {
        #region Methods

        MapDTO Insert(MapDTO map);

        void Insert(List<MapDTO> maps);

        IEnumerable<MapDTO> LoadAll();

        CacheStatisticsSnapshot GetCacheStatistics();

        MapDTO LoadById(short mapId);

        #endregion
    }
}