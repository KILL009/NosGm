using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IQuestDAO
    {
        #region Methods

        DeleteResult DeleteById(long id);

        void Insert(List<QuestDTO> questList);

        QuestDTO InsertOrUpdate(QuestDTO quest);

        IEnumerable<QuestDTO> LoadAll();

        QuestDTO LoadById(long id);

        #endregion
    }
}