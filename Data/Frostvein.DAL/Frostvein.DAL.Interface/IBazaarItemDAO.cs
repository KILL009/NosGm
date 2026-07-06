using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IBazaarItemDAO
    {
        #region Methods

        DeleteResult Delete(long bazaarItemId);

        SaveResult InsertOrUpdate(ref BazaarItemDTO bazaarItem);

        IEnumerable<BazaarItemDTO> LoadAll();

        BazaarItemDTO LoadById(long bazaarItemId);

        void RemoveOutDated();

        IEnumerable<BazaarItemDTO> LoadByCharacterId(long characterId);

        #endregion
    }
}