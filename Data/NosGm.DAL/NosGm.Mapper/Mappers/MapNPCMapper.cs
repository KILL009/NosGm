using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class MapNpcMapper
    {
        #region Methods

        public static bool ToMapNpc(MapNpcDTO input, MapNpc output)
        {
            if (input == null) return false;

            output.Dialog = input.Dialog;
            output.Effect = input.Effect;
            output.EffectDelay = input.EffectDelay;
            output.IsDisabled = input.IsDisabled;
            output.IsMoving = input.IsMoving;
            output.IsSitting = input.IsSitting;
            output.MapId = input.MapId;
            output.MapNpcId = input.MapNpcId;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.Name = input.Name;
            output.NpcVNum = input.NpcVNum;
            output.Position = input.Position;
            output.Say = input.Say;
            output.Delay = input.Delay;
            output.Information = input.Information;
            return true;
        }

        public static bool ToMapNpcDTO(MapNpc input, MapNpcDTO output)
        {
            if (input == null) return false;

            output.Dialog = input.Dialog;
            output.Effect = input.Effect;
            output.EffectDelay = input.EffectDelay;
            output.IsDisabled = input.IsDisabled;
            output.IsMoving = input.IsMoving;
            output.IsSitting = input.IsSitting;
            output.MapId = input.MapId;
            output.MapNpcId = input.MapNpcId;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.Name = input.Name;
            output.NpcVNum = input.NpcVNum;
            output.Position = input.Position;
            output.Say = input.Say;
            output.Delay = input.Delay;
            output.Information = input.Information;
            return true;
        }

        #endregion
    }
}