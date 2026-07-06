using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class CharacterTitleMapper
    {
        #region Methods

        public static bool ToTitle(CharacterTitleDTO input, CharacterTitle output)
        {
            if (input == null)
            {
                return false;
            }

            output.CharacterTitleId = input.CharacterTitleId;
            output.CharacterId = input.CharacterId;
            output.Stat = input.Stat;
            output.TitleVnum = input.TitleVnum;

            return true;
        }

        public static bool ToTitleDTO(CharacterTitle input, CharacterTitleDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.CharacterTitleId = input.CharacterTitleId;
            output.CharacterId = input.CharacterId;
            output.Stat = input.Stat;
            output.TitleVnum = input.TitleVnum;

            return true;
        }

        #endregion
    }
}