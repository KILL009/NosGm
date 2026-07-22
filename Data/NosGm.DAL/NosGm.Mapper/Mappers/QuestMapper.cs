using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class QuestMapper
    {
        #region Methods

        public static bool ToQuest(QuestDTO input, Quest output)
        {
            if (input == null) return false;

            output.QuestId = input.QuestId;
            output.QuestType = input.QuestType;
            output.LevelMin = input.LevelMin;
            output.LevelMax = input.LevelMax;
            output.StartDialogId = input.StartDialogId;
            output.EndDialogId = input.EndDialogId;
            output.DialogNpcVNum = input.DialogNpcVNum;
            output.DialogNpcId = input.DialogNpcId;
            output.TargetMap = input.TargetMap;
            output.TargetX = input.TargetX;
            output.TargetY = input.TargetY;
            output.InfoId = input.InfoId;
            output.NextQuestId = input.NextQuestId;
            output.IsDaily = input.IsDaily;
            output.CanBeDoneOnlyOnce = input.CanBeDoneOnlyOnce;

            return true;
        }

        public static bool ToQuestDTO(Quest input, QuestDTO output)
        {
            if (input == null) return false;

            output.QuestId = input.QuestId;
            output.QuestType = input.QuestType;
            output.LevelMin = input.LevelMin;
            output.LevelMax = input.LevelMax;
            output.StartDialogId = input.StartDialogId;
            output.EndDialogId = input.EndDialogId;
            output.DialogNpcVNum = input.DialogNpcVNum;
            output.DialogNpcId = input.DialogNpcId;
            output.TargetMap = input.TargetMap;
            output.TargetX = input.TargetX;
            output.TargetY = input.TargetY;
            output.InfoId = input.InfoId;
            output.NextQuestId = input.NextQuestId;
            output.IsDaily = input.IsDaily;

            return true;
        }

        #endregion
    }
}