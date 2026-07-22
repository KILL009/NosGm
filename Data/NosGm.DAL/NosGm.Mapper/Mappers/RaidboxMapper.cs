using NosGm.DAL.EF;
using NosGm.DAL.Interface;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class RaidboxMapper
    {
        #region Methods

        public static bool ToRaidbox(RaidboxDTO input, Raidbox output)
        {
            if (input == null) return false;

            output.IsRareRandom = input.IsRareRandom;
            output.ItemGeneratedAmount = input.ItemGeneratedAmount;
            output.ItemGeneratedVNum = input.ItemGeneratedVNum;
            output.ItemGeneratedDesign = input.ItemGeneratedDesign;
            output.MaximumOriginalItemRare = input.MaximumOriginalItemRare;
            output.MinimumOriginalItemRare = input.MinimumOriginalItemRare;
            output.OriginalItemDesign = input.OriginalItemDesign;
            output.OriginalItemVNum = input.OriginalItemVNum;
            output.Probability = input.Probability;
            output.RaidboxId = input.RaidboxId;
            return true;
        }

        public static bool ToRaidboxDTO(Raidbox input, RaidboxDTO output)
        {
            if (input == null) return false;

            output.IsRareRandom = input.IsRareRandom;
            output.ItemGeneratedAmount = input.ItemGeneratedAmount;
            output.ItemGeneratedVNum = input.ItemGeneratedVNum;
            output.ItemGeneratedDesign = input.ItemGeneratedDesign;
            output.MaximumOriginalItemRare = input.MaximumOriginalItemRare;
            output.MinimumOriginalItemRare = input.MinimumOriginalItemRare;
            output.OriginalItemDesign = input.OriginalItemDesign;
            output.OriginalItemVNum = input.OriginalItemVNum;
            output.Probability = input.Probability;
            output.RaidboxId = input.RaidboxId;

            return true;
        }

        #endregion
    }
}