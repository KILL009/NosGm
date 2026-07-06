using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class CellonOptionMapper
    {
        #region Methods

        public static bool ToCellonOption(CellonOptionDTO input, CellonOption output)
        {
            if (input == null) return false;

            output.CellonOptionId = input.CellonOptionId;
            output.EquipmentSerialId = input.EquipmentSerialId;
            output.Level = input.Level;
            output.Type = input.Type;
            output.Value = input.Value;

            return true;
        }

        public static bool ToCellonOptionDTO(CellonOption input, CellonOptionDTO output)
        {
            if (input == null) return false;

            output.CellonOptionId = input.CellonOptionId;
            output.EquipmentSerialId = input.EquipmentSerialId;
            output.Level = input.Level;
            output.Type = input.Type;
            output.Value = input.Value;

            return true;
        }

        #endregion
    }
}