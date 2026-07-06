using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class RespawnMapper
    {
        #region Methods

        public static bool ToRespawn(RespawnDTO input, Respawn output)
        {
            if (input == null)
            {
                return false;
            }

            output.CharacterId = input.CharacterId;
            output.MapId = input.MapId;
            output.RespawnId = input.RespawnId;
            output.RespawnMapTypeId = input.RespawnMapTypeId;
            output.X = input.X;
            output.Y = input.Y;

            return true;
        }

        public static bool ToRespawnDTO(Respawn input, RespawnDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.CharacterId = input.CharacterId;
            output.MapId = input.MapId;
            output.RespawnId = input.RespawnId;
            output.RespawnMapTypeId = input.RespawnMapTypeId;
            output.X = input.X;
            output.Y = input.Y;

            return true;
        }

        #endregion
    }
}