using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class BattlePassQuestProgressMapper
    {
        public static bool ToBpQuestProgress(BattlePassQuestProgressDTO input, BattlePassQuestProgress output)
        {
            if (input == null)
            {
                return false;
            }

            output.BpQuestProgressId = input.BpQuestProgressId;
            output.AccountId = input.AccountId;
            output.BpQuestId = input.BpQuestId;
            output.Amount = input.Amount;
            output.Completed = input.Completed;
            return true;
        }

        public static bool ToBpQuestProgressDTO(BattlePassQuestProgress input, BattlePassQuestProgressDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.BpQuestProgressId = input.BpQuestProgressId;
            output.AccountId = input.AccountId;
            output.BpQuestId = input.BpQuestId;
            output.Amount = input.Amount;
            output.Completed = input.Completed;
            return true;
        }
    }
}