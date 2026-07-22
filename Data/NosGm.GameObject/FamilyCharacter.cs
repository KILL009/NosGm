using NosGm.DAL;
using NosGm.Data;

namespace NosGm.GameObject
{
    public class FamilyCharacter : FamilyCharacterDTO
    {
        #region Members

        private CharacterDTO _character;

        #endregion

        #region Properties

        public CharacterDTO Character => _character ?? (_character = DAOFactory.CharacterDAO.LoadById(CharacterId));

        #endregion

        #region Instantiation

        public FamilyCharacter()
        {
        }

        public FamilyCharacter(FamilyCharacterDTO input)
        {
            Authority = input.Authority;
            CharacterId = input.CharacterId;
            DailyMessage = input.DailyMessage;
            Experience = input.Experience;
            FamilyCharacterId = input.FamilyCharacterId;
            FamilyId = input.FamilyId;
            Rank = input.Rank;
        }

        #endregion
    }
}