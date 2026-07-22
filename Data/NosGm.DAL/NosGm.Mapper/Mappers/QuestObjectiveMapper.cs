using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class QuestObjectiveMapper
    {
        #region Methods

        public static bool ToQuestObjective(QuestObjectiveDTO input, QuestObjective output)
        {
            if (input == null) return false;

            output.QuestObjectiveId = input.QuestObjectiveId;
            output.QuestId = input.QuestId;
            output.Data = input.Data;
            output.Objective = input.Objective;
            output.SpecialData = input.SpecialData;
            output.DropRate = input.DropRate;
            output.ObjectiveIndex = input.ObjectiveIndex;

            return true;
        }

        public static bool ToQuestObjectiveDTO(QuestObjective input, QuestObjectiveDTO output)
        {
            if (input == null) return false;

            output.QuestObjectiveId = input.QuestObjectiveId;
            output.QuestId = input.QuestId;
            output.Data = input.Data;
            output.Objective = input.Objective;
            output.SpecialData = input.SpecialData;
            output.DropRate = input.DropRate;
            output.ObjectiveIndex = input.ObjectiveIndex;

            return true;
        }

        #endregion
    }
}