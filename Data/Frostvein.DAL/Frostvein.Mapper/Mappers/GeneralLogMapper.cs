using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class GeneralLogMapper
    {
        #region Methods

        public static bool ToGeneralLog(GeneralLogDTO input, GeneralLog output)
        {
            if (input == null) return false;

            output.AccountId = input.AccountId;
            output.CharacterId = input.CharacterId;
            output.IpAddress = input.IpAddress;
            output.LogData = input.LogData;
            output.LogId = input.LogId;
            output.LogType = input.LogType;
            output.Timestamp = input.Timestamp;

            return true;
        }

        public static bool ToGeneralLogDTO(GeneralLog input, GeneralLogDTO output)
        {
            if (input == null) return false;

            output.AccountId = input.AccountId;
            output.CharacterId = input.CharacterId;
            output.IpAddress = input.IpAddress;
            output.LogData = input.LogData;
            output.LogId = input.LogId;
            output.LogType = input.LogType;
            output.Timestamp = input.Timestamp;

            return true;
        }

        #endregion
    }
}