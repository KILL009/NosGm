using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IQuestLogDAO
    {
        #region Methods

        SaveResult InsertOrUpdate(ref QuestLogDTO questLog);

        IEnumerable<QuestLogDTO> LoadByCharacterId(long id);

        QuestLogDTO LoadById(long id);

        #endregion
    }
}