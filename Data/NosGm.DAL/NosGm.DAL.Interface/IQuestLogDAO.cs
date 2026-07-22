using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
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