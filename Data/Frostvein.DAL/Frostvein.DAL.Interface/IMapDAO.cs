using Frostvein.Data;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IMapDAO
    {
        #region Methods

        MapDTO Insert(MapDTO map);

        void Insert(List<MapDTO> maps);

        IEnumerable<MapDTO> LoadAll();

        MapDTO LoadById(short mapId);

        #endregion
    }
}