using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public class FishingInformationsMapper
    {
        public static bool ToFishingInformations(FishingInformationsDto input, FishingInformations output)
        {
            if (input == null)
            {
                return false;
            }

            output.Id = input.Id;
            output.FishVNum = input.FishVNum;
            output.MapId1 = input.MapId1;
            output.MapId2 = input.MapId2;
            output.MapId3 = input.MapId3;
            output.MaxFishLength = input.MaxFishLength;
            output.MinFishLength = input.MinFishLength;
            output.Probability = input.Probability;
            output.IsFish = input.IsFish;

            return true;
        }

        public static bool ToFishingInformationsDto(FishingInformations input, FishingInformationsDto output)
        {
            if (input == null)
            {
                return false;
            }

            output.Id = input.Id;
            output.FishVNum = input.FishVNum;
            output.MapId1 = input.MapId1;
            output.MapId2 = input.MapId2;
            output.MapId3 = input.MapId3;
            output.MaxFishLength = input.MaxFishLength;
            output.MinFishLength = input.MinFishLength;
            output.Probability = input.Probability;
            output.IsFish = input.IsFish;

            return true;
        }
    }
}
