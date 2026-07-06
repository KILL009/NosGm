using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class QuicklistEntryMapper
    {
        #region Methods

        public static bool ToQuicklistEntry(QuicklistEntryDTO input, QuicklistEntry output)
        {
            if (input == null) return false;

            output.CharacterId = input.CharacterId;
            output.Id = input.Id;
            output.Morph = input.Morph;
            output.Pos = input.Pos;
            output.Q1 = input.Q1;
            output.Q2 = input.Q2;
            output.Slot = input.Slot;
            output.Type = input.Type;

            return true;
        }

        public static bool ToQuicklistEntryDTO(QuicklistEntry input, QuicklistEntryDTO output)
        {
            if (input == null) return false;

            output.CharacterId = input.CharacterId;
            output.Id = input.Id;
            output.Morph = input.Morph;
            output.Pos = input.Pos;
            output.Q1 = input.Q1;
            output.Q2 = input.Q2;
            output.Slot = input.Slot;
            output.Type = input.Type;

            return true;
        }

        #endregion
    }
}