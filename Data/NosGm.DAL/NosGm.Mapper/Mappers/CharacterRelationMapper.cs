using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class CharacterRelationMapper
    {
        #region Methods

        public static bool ToCharacterRelation(CharacterRelationDTO input, CharacterRelation output)
        {
            if (input == null) return false;

            output.CharacterId = input.CharacterId;
            output.CharacterRelationId = input.CharacterRelationId;
            output.RelatedCharacterId = input.RelatedCharacterId;
            output.RelationType = input.RelationType;

            return true;
        }

        public static bool ToCharacterRelationDTO(CharacterRelation input, CharacterRelationDTO output)
        {
            if (input == null) return false;

            output.CharacterId = input.CharacterId;
            output.CharacterRelationId = input.CharacterRelationId;
            output.RelatedCharacterId = input.RelatedCharacterId;
            output.RelationType = input.RelationType;

            return true;
        }

        #endregion
    }
}