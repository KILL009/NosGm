using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
{
    public interface IMateDAO
    {
        #region Methods

        DeleteResult Delete(long id);

        SaveResult InsertOrUpdate(ref MateDTO mate);

        Task<SaveResult> InsertOrUpdateAsync(MateDTO mate);

        IEnumerable<MateDTO> LoadByCharacterId(long characterId);

        #endregion
    }
}