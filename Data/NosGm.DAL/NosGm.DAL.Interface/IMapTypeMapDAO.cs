using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IMapTypeMapDAO
    {
        #region Methods

        void Insert(List<MapTypeMapDTO> mapTypeMaps);

        IEnumerable<MapTypeMapDTO> LoadAll();

        MapTypeMapDTO LoadByMapAndMapType(short mapId, short maptypeId);

        IEnumerable<MapTypeMapDTO> LoadByMapId(short mapId);

        IEnumerable<MapTypeMapDTO> LoadByMapTypeId(short maptypeId);

        #endregion
    }
}