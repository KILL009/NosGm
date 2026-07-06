using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class CharacterTimespaceLogMapper
    {
        #region Methods

        public static bool ToTimespaceLog(CharacterTimespaceLogDTO input, CharacterTimespaceLog output)
        {
            if (input == null)
            {
                return false;
            }

            output.LogId = input.CharacterId;
            output.CharacterId = input.CharacterId;
            output.ScriptedInstanceId = input.ScriptedInstanceId;
            output.Score = input.Score;
            output.Date = input.Date;
            output.IsFailed = input.IsFailed;

            return true;
        }

        public static bool ToTimespaceLogDTO(CharacterTimespaceLog input, CharacterTimespaceLogDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.LogId = input.CharacterId;
            output.CharacterId = input.CharacterId;
            output.ScriptedInstanceId = input.ScriptedInstanceId;
            output.Score = input.Score;
            output.Date = input.Date;
            output.IsFailed = input.IsFailed;

            return true;
        }

        #endregion
    }
}