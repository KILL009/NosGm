using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.DAL.Interface
{
    public interface IMinilandObjectDAO
    {
        #region Methods

        DeleteResult DeleteById(long id);

        SaveResult InsertOrUpdate(ref MinilandObjectDTO obj);

        Task<SaveResult> InsertOrUpdateAsync(MinilandObjectDTO obj);

        IEnumerable<MinilandObjectDTO> LoadByCharacterId(long characterId);

        #endregion
    }
}