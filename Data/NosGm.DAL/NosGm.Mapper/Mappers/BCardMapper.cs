using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class BCardMapper
    {
        #region Methods

        public static bool ToBCard(BCardDTO input, BCard output)
        {
            if (input == null) return false;

            output.BCardId = input.BCardId;
            output.CardId = input.CardId;
            output.CastType = input.CastType;
            output.FirstData = input.FirstData;
            output.IsLevelDivided = input.IsLevelDivided;
            output.IsLevelScaled = input.IsLevelScaled;
            output.ItemVNum = input.ItemVNum;
            output.NpcMonsterVNum = input.NpcMonsterVNum;
            output.SecondData = input.SecondData;
            output.SkillVNum = input.SkillVNum;
            output.SubType = input.SubType;
            output.ThirdData = input.ThirdData;
            output.Type = input.Type;

            return true;
        }

        public static bool ToBCardDTO(BCard input, BCardDTO output)
        {
            if (input == null) return false;

            output.BCardId = input.BCardId;
            output.CardId = input.CardId;
            output.CastType = input.CastType;
            output.FirstData = input.FirstData;
            output.IsLevelDivided = input.IsLevelDivided;
            output.IsLevelScaled = input.IsLevelScaled;
            output.ItemVNum = input.ItemVNum;
            output.NpcMonsterVNum = input.NpcMonsterVNum;
            output.SecondData = input.SecondData;
            output.SkillVNum = input.SkillVNum;
            output.SubType = input.SubType;
            output.ThirdData = input.ThirdData;
            output.Type = input.Type;

            return true;
        }

        #endregion
    }
}