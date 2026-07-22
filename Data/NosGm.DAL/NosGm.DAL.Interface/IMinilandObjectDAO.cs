using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
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