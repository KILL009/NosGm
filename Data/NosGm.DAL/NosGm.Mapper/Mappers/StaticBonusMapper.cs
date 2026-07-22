using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class StaticBonusMapper
    {
        #region Methods

        public static bool ToStaticBonus(StaticBonusDTO input, StaticBonus output)
        {
            if (input == null) return false;

            output.CharacterId = input.CharacterId;
            output.DateEnd = input.DateEnd;
            output.StaticBonusId = input.StaticBonusId;
            output.StaticBonusType = input.StaticBonusType;

            return true;
        }

        public static bool ToStaticBonusDTO(StaticBonus input, StaticBonusDTO output)
        {
            if (input == null) return false;

            output.CharacterId = input.CharacterId;
            output.DateEnd = input.DateEnd;
            output.StaticBonusId = input.StaticBonusId;
            output.StaticBonusType = input.StaticBonusType;

            return true;
        }

        #endregion
    }
}