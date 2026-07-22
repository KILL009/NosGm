using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IQuestObjectiveDAO
    {
        #region Methods

        QuestObjectiveDTO Insert(QuestObjectiveDTO questObjective);

        void Insert(List<QuestObjectiveDTO> questObjectives);

        List<QuestObjectiveDTO> LoadAll();

        IEnumerable<QuestObjectiveDTO> LoadByQuestId(long questId);

        #endregion
    }
}