using Frostvein.DAL.EF;
using Frostvein.DAL.Interface.PropertiesMapping;
using Frostvein.Data;
using Frostvein.Mapper.Props;

namespace Frostvein.Mapper.Mappers
{
    public class FairyEnchantmentMapper : ModuleMapper<FairyEnchantmentDTO, FairyEnchantment>, IModuleMapper<FairyEnchantmentDTO, FairyEnchantment>
    {
        public static bool ToEntityStatic(FairyEnchantmentDTO input, FairyEnchantment output)
        {
            return new FairyEnchantmentMapper().ToEntity(input, output);
        }

        public static bool ToDTOStatic(FairyEnchantment input, FairyEnchantmentDTO output)
        {
            return new FairyEnchantmentMapper().ToDTO(input, output);
        }

        public override bool ToEntity(FairyEnchantmentDTO input, FairyEnchantment output)
        {
            if (input == null)
            {
                return false;
            }

            output.FairyEnchantmentId = input.FairyEnchantmentId;
            output.EquipmentSerialId = input.EquipmentSerialId;
            output.Type = input.Type;
            output.SubType = input.SubType;
            output.FirstData = input.FirstData;
            output.SecondData = input.SecondData;
            output.ThirdData = input.ThirdData;

            return true;
        }

        public override bool ToDTO(FairyEnchantment input, FairyEnchantmentDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.FairyEnchantmentId = input.FairyEnchantmentId;
            output.EquipmentSerialId = input.EquipmentSerialId;
            output.Type = input.Type;
            output.SubType = input.SubType;
            output.FirstData = input.FirstData;
            output.SecondData = input.SecondData;
            output.ThirdData = input.ThirdData;

            return true;
        }
    }
}
