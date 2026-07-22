using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
{
    public interface IRespawnDAO
    {
        #region Methods

        SaveResult InsertOrUpdate(ref RespawnDTO respawn);

        Task<SaveResult> InsertOrUpdateAsync(RespawnDTO respawn);

        IEnumerable<RespawnDTO> LoadByCharacter(long characterId);

        RespawnDTO LoadById(long respawnId);

        #endregion
    }
}