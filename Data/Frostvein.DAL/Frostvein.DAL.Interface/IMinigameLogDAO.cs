using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IMinigameLogDAO
    {
        #region Methods

        SaveResult InsertOrUpdate(ref MinigameLogDTO minigameLog);

        IEnumerable<MinigameLogDTO> LoadByCharacterId(long characterId);

        MinigameLogDTO LoadById(long minigameLogId);

        #endregion
    }
}