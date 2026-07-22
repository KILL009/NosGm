using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class CharacterSkillMapper
    {
        #region Methods

        public static bool ToCharacterSkill(CharacterSkillDTO input, CharacterSkill output)
        {
            if (input == null) return false;

            output.CharacterId = input.CharacterId;
            output.Id = input.Id;
            output.SkillVNum = input.SkillVNum;
            output.IsTattoo = input.IsTattoo;
            output.TattooLevel = input.TattooLevel;
            output.IsPartnerSkill = input.IsPartnerSkill;
            return true;
        }

        public static bool ToCharacterSkillDTO(CharacterSkill input, CharacterSkillDTO output)
        {
            if (input == null) return false;

            output.CharacterId = input.CharacterId;
            output.Id = input.Id;
            output.SkillVNum = input.SkillVNum;
            output.IsTattoo = input.IsTattoo;
            output.TattooLevel = input.TattooLevel;
            output.IsPartnerSkill = input.IsPartnerSkill;
            return true;
        }

        #endregion
    }
}