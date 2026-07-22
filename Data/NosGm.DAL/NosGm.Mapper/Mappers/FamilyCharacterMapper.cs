using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class FamilyCharacterMapper
    {
        #region Methods

        public static bool ToFamilyCharacter(FamilyCharacterDTO input, FamilyCharacter output)
        {
            if (input == null) return false;

            output.Authority = input.Authority;
            output.CharacterId = input.CharacterId;
            output.DailyMessage = input.DailyMessage;
            output.Experience = input.Experience;
            output.FamilyCharacterId = input.FamilyCharacterId;
            output.FamilyId = input.FamilyId;
            output.Rank = input.Rank;

            return true;
        }

        public static bool ToFamilyCharacterDTO(FamilyCharacter input, FamilyCharacterDTO output)
        {
            if (input == null) return false;

            output.Authority = input.Authority;
            output.CharacterId = input.CharacterId;
            output.DailyMessage = input.DailyMessage;
            output.Experience = input.Experience;
            output.FamilyCharacterId = input.FamilyCharacterId;
            output.FamilyId = input.FamilyId;
            output.Rank = input.Rank;

            return true;
        }

        #endregion
    }
}