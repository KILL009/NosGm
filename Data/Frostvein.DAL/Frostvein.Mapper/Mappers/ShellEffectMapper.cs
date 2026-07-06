using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class ShellEffectMapper
    {
        #region Methods

        public static bool ToShellEffect(ShellEffectDTO input, ShellEffect output)
        {
            if (input == null)
            {
                return false;
            }

            output.Effect = input.Effect;
            output.EffectLevel = input.EffectLevel;
            output.EquipmentSerialId = input.EquipmentSerialId;
            output.ShellEffectId = input.ShellEffectId;
            output.Value = input.Value;
            output.IsRune = input.IsRune;
            output.Type = input.Type;
            output.Upgrade = input.Upgrade;

            return true;
        }

        public static bool ToShellEffectDTO(ShellEffect input, ShellEffectDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.Effect = input.Effect;
            output.EffectLevel = input.EffectLevel;
            output.EquipmentSerialId = input.EquipmentSerialId;
            output.ShellEffectId = input.ShellEffectId;
            output.Value = input.Value;
            output.IsRune = input.IsRune;
            output.Type = input.Type;
            output.Upgrade = input.Upgrade;

            return true;
        }

        #endregion
    }
}