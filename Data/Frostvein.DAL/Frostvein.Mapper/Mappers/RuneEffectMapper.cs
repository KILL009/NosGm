using Frostvein.DAL.EF;
using Frostvein.DAL.Interface.PropertiesMapping;
using Frostvein.Data;
using Frostvein.Mapper.Props;

namespace Frostvein.Mapper.Mappers
{
    public class RuneEffectMapper : ModuleMapper<RuneEffectDTO, RuneEffect>, IModuleMapper<RuneEffectDTO, RuneEffect>
    {
        public static bool ToEntityStatic(RuneEffectDTO input, RuneEffect output)
        {
            return new RuneEffectMapper().ToEntity(input, output);
        }

        public static bool ToDTOStatic(RuneEffect input, RuneEffectDTO output)
        {
            return new RuneEffectMapper().ToDTO(input, output);
        }

        public override bool ToEntity(RuneEffectDTO input, RuneEffect output)
        {
            if (input == null)
            {
                return false;
            }

            output.RuneEffectId = input.RuneEffectId;
            output.EquipmentSerialId = input.EquipmentSerialId;
            output.Type = input.Type;
            output.SubType = input.SubType;
            output.FirstData = input.FirstData;
            output.SecondData = input.SecondData;
            output.ThirdData = input.ThirdData;
            output.IsPower = input.IsPower;

            return true;
        }

        public override bool ToDTO(RuneEffect input, RuneEffectDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.RuneEffectId = input.RuneEffectId;
            output.EquipmentSerialId = input.EquipmentSerialId;
            output.Type = input.Type;
            output.SubType = input.SubType;
            output.FirstData = input.FirstData;
            output.SecondData = input.SecondData;
            output.ThirdData = input.ThirdData;
            output.IsPower = input.IsPower;

            return true;
        }
    }
}
