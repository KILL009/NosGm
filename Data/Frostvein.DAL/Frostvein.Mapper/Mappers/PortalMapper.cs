using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class PortalMapper
    {
        #region Methods

        public static bool ToPortal(PortalDTO input, Portal output)
        {
            if (input == null) return false;

            output.DestinationMapId = input.DestinationMapId;
            output.DestinationX = input.DestinationX;
            output.DestinationY = input.DestinationY;
            output.IsDisabled = input.IsDisabled;
            output.PortalId = input.PortalId;
            output.SourceMapId = input.SourceMapId;
            output.SourceX = input.SourceX;
            output.SourceY = input.SourceY;
            output.Type = input.Type;
            output.RequiredClass = input.RequiredClass;
            output.NomeOggetto = input.NomeOggetto;
            output.RequiredItem = input.RequiredItem;
            output.LevelRequired = input.LevelRequired;
            output.HeroLevelRequired = input.HeroLevelRequired;

            return true;
        }

        public static bool ToPortalDTO(Portal input, PortalDTO output)
        {
            if (input == null) return false;

            output.DestinationMapId = input.DestinationMapId;
            output.DestinationX = input.DestinationX;
            output.DestinationY = input.DestinationY;
            output.IsDisabled = input.IsDisabled;
            output.PortalId = input.PortalId;
            output.RequiredItem = input.RequiredItem;
            output.LevelRequired = input.LevelRequired;
            output.HeroLevelRequired = input.HeroLevelRequired;
            output.NomeOggetto = input.NomeOggetto;
            output.SourceMapId = input.SourceMapId;
            output.SourceX = input.SourceX;
            output.SourceY = input.SourceY;
            output.Type = input.Type;
            output.RequiredClass = input.RequiredClass;
            output.NomeOggetto = input.NomeOggetto;
            output.RequiredItem = input.RequiredItem;
            output.LevelRequired = input.LevelRequired;
            output.HeroLevelRequired = input.HeroLevelRequired;

            return true;
        }

        #endregion
    }
}