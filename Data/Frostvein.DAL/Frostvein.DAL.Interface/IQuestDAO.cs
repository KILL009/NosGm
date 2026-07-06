using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
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