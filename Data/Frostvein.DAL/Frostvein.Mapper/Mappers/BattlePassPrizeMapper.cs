using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class BattlePassPrizeMapper
    {
        public static bool ToBpQuestPrize(BattlePassPrizeDTO input, BattlePassPrize output)
        {
            if (input == null)
            {
                return false;
            }

            output.BpPrizeId = input.BpPrizeId;
            output.Level = input.Level;
            output.ItemVNum = input.ItemVNum;
            output.Amount = input.Amount;
            output.ItemVNumPremium = input.ItemVNumPremium;
            output.AmountPremium = input.AmountPremium;
            output.IsSpecial = input.IsSpecial;
            return true;
        }

        public static bool ToBpQuestDTOPrize(BattlePassPrize input, BattlePassPrizeDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.BpPrizeId = input.BpPrizeId;
            output.Level = input.Level;
            output.ItemVNum = input.ItemVNum;
            output.Amount = input.Amount;
            output.ItemVNumPremium = input.ItemVNumPremium;
            output.AmountPremium = input.AmountPremium;
            output.IsSpecial = input.IsSpecial;
            return true;
        }
    }
}