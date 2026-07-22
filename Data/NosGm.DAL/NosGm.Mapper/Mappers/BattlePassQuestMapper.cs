using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class BattlePassQuestMapper
    {
        public static bool ToBpQuest(BattlePassQuestDTO input, BattlePassQuest output)
        {
            if (input == null)
            {
                return false;
            }

            output.BpQuestId = input.BpQuestId;
            output.BpQuestType = input.BpQuestType;
            output.BpTimeType = input.BpTimeType;
            output.Amount = input.Amount;
            output.Points = input.Points;
            output.IsPremium = input.IsPremium;
            return true;
        }

        public static bool ToBpQuestDTO(BattlePassQuest input, BattlePassQuestDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.BpQuestId = input.BpQuestId;
            output.BpQuestType = input.BpQuestType;
            output.BpTimeType = input.BpTimeType;
            output.Amount = input.Amount;
            output.Points = input.Points;
            output.IsPremium = input.IsPremium;
            return true;
        }
    }
}