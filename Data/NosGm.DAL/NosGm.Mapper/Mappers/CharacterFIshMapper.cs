using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public class CharacterFIshMapper
    {
        public static bool ToCharacterFish(CharacterFishDto input, CharacterFish output)
        {
            if (input == null)
            {
                return false;
            }

            output.CharacterId = input.CharacterId;
            output.FishCount = input.FishCount;
            output.FishId = input.FishId;
            output.Id = input.Id;
            output.MaxLength = input.MaxLength;

            return true;
        }

        public static bool ToCharacterFishDto(CharacterFish input, CharacterFishDto output)
        {
            if (input == null)
            {
                return false;
            }

            output.CharacterId = input.CharacterId;
            output.FishCount = input.FishCount;
            output.FishId = input.FishId;
            output.Id = input.Id;
            output.MaxLength = input.MaxLength;

            return true;
        }
    }
}
