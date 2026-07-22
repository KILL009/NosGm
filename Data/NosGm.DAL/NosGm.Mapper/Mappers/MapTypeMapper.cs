using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class MapTypeMapper
    {
        #region Methods

        public static bool ToMapType(MapTypeDTO input, MapType output)
        {
            if (input == null) return false;

            output.MapTypeId = input.MapTypeId;
            output.MapTypeName = input.MapTypeName;
            output.PotionDelay = input.PotionDelay;
            output.RespawnMapTypeId = input.RespawnMapTypeId;
            output.ReturnMapTypeId = input.ReturnMapTypeId;

            return true;
        }

        public static bool ToMapTypeDTO(MapType input, MapTypeDTO output)
        {
            if (input == null) return false;

            output.MapTypeId = input.MapTypeId;
            output.MapTypeName = input.MapTypeName;
            output.PotionDelay = input.PotionDelay;
            output.RespawnMapTypeId = input.RespawnMapTypeId;
            output.ReturnMapTypeId = input.ReturnMapTypeId;

            return true;
        }

        #endregion
    }
}