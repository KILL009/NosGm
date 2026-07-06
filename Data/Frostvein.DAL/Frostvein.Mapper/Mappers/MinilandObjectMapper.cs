using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class MinilandObjectMapper
    {
        #region Methods

        public static bool ToMinilandObject(MinilandObjectDTO input, MinilandObject output)
        {
            if (input == null) return false;

            output.CharacterId = input.CharacterId;
            output.ItemInstanceId = input.ItemInstanceId;
            output.Level1BoxAmount = input.Level1BoxAmount;
            output.Level2BoxAmount = input.Level2BoxAmount;
            output.Level3BoxAmount = input.Level3BoxAmount;
            output.Level4BoxAmount = input.Level4BoxAmount;
            output.Level5BoxAmount = input.Level5BoxAmount;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.MinilandObjectId = input.MinilandObjectId;

            return true;
        }

        public static bool ToMinilandObjectDTO(MinilandObject input, MinilandObjectDTO output)
        {
            if (input == null) return false;

            output.CharacterId = input.CharacterId;
            output.ItemInstanceId = input.ItemInstanceId;
            output.Level1BoxAmount = input.Level1BoxAmount;
            output.Level2BoxAmount = input.Level2BoxAmount;
            output.Level3BoxAmount = input.Level3BoxAmount;
            output.Level4BoxAmount = input.Level4BoxAmount;
            output.Level5BoxAmount = input.Level5BoxAmount;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.MinilandObjectId = input.MinilandObjectId;

            return true;
        }

        #endregion
    }
}