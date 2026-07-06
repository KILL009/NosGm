using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class PenaltyLogMapper
    {
        #region Methods

        public static bool ToPenaltyLog(PenaltyLogDTO input, PenaltyLog output)
        {
            if (input == null) return false;

            output.AccountId = input.AccountId;
            output.IP = input.IP;
            output.AdminName = input.AdminName;
            output.DateEnd = input.DateEnd;
            output.DateStart = input.DateStart;
            output.Penalty = input.Penalty;
            output.PenaltyLogId = input.PenaltyLogId;
            output.Reason = input.Reason;

            return true;
        }

        public static bool ToPenaltyLogDTO(PenaltyLog input, PenaltyLogDTO output)
        {
            if (input == null) return false;

            output.AccountId = input.AccountId;
            output.IP = input.IP;
            output.AdminName = input.AdminName;
            output.DateEnd = input.DateEnd;
            output.DateStart = input.DateStart;
            output.Penalty = input.Penalty;
            output.PenaltyLogId = input.PenaltyLogId;
            output.Reason = input.Reason;

            return true;
        }

        #endregion
    }
}