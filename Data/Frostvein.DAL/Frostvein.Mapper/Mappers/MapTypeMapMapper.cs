using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class MapTypeMapMapper
    {
        #region Methods

        public static bool ToMapTypeMap(MapTypeMapDTO input, MapTypeMap output)
        {
            if (input == null) return false;

            output.MapId = input.MapId;
            output.MapTypeId = input.MapTypeId;

            return true;
        }

        public static bool ToMapTypeMapDTO(MapTypeMap input, MapTypeMapDTO output)
        {
            if (input == null) return false;

            output.MapId = input.MapId;
            output.MapTypeId = input.MapTypeId;

            return true;
        }

        #endregion
    }
}