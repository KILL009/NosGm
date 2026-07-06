using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.DAL.Interface
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