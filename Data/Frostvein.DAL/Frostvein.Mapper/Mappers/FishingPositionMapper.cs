using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public class FishingPositionMapper
    {
        public static bool ToFishingPosition(FishingPositionDto input, FishingPosition output)
        {
            if (input == null)
            {
                return false;
            }

            output.Direction = input.Direction;
            output.Id = input.Id;
            output.MapId = input.MapId;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.MinLevel = input.MinLevel;

            return true;
        }

        public static bool ToFishingPositionDto(FishingPosition input, FishingPositionDto output)
        {
            if (input == null)
            {
                return false;
            }

            output.Direction = input.Direction;
            output.Id = input.Id;
            output.MapId = input.MapId;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.MinLevel = input.MinLevel;

            return true;
        }
    }
}
