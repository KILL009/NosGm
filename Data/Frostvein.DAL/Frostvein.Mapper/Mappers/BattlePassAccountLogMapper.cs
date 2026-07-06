using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class BattlePassAccountLogMapper
    {
        public static bool ToBpAccountLog(BattlePassAccountLogDTO input, BattlePassAccountLog output)
        {
            if (input == null)
            {
                return false;
            }

            output.BpAccountLogId = input.BpAccountLogId;
            output.AccountId = input.AccountId;
            output.Level = input.Level;
            output.IsPremium = input.IsPremium;
            return true;
        }

        public static bool ToBpAccountLogDTO(BattlePassAccountLog input, BattlePassAccountLogDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.BpAccountLogId = input.BpAccountLogId;
            output.AccountId = input.AccountId;
            output.Level = input.Level;
            output.IsPremium = input.IsPremium;
            return true;
        }
    }
}