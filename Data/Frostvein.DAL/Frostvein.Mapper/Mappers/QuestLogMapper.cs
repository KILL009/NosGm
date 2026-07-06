using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class QuestLogMapper
    {
        #region Methods

        public static bool ToQuestLog(QuestLogDTO input, QuestLog output)
        {
            if (input == null) return false;

            output.CharacterId = input.CharacterId;
            output.QuestId = input.QuestId;
            output.IpAddress = input.IpAddress;
            output.LastDaily = input.LastDaily;

            return true;
        }

        public static bool ToQuestLogDTO(QuestLog input, QuestLogDTO output)
        {
            if (input == null) return false;

            output.CharacterId = input.CharacterId;
            output.QuestId = input.QuestId;
            output.IpAddress = input.IpAddress;
            output.LastDaily = input.LastDaily;

            return true;
        }

        #endregion
    }
}