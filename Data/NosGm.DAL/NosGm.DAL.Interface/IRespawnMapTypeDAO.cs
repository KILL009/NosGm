using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IRespawnMapTypeDAO
    {
        #region Methods

        void Insert(List<RespawnMapTypeDTO> respawnMapTypes);

        SaveResult InsertOrUpdate(ref RespawnMapTypeDTO respawnMapType);

        RespawnMapTypeDTO LoadById(long respawnMapTypeId);

        RespawnMapTypeDTO LoadByMapId(short mapId);

        IEnumerable<RespawnMapTypeDTO> LoadAll();

        #endregion
    }
}