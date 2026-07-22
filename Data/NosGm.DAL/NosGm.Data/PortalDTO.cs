using System;

namespace NosGm.Data
{
    [Serializable]
    public class PortalDTO
    {
        #region Properties

        public short DestinationMapId { get; set; }

        public short DestinationX { get; set; }

        public short DestinationY { get; set; }

        public bool IsDisabled { get; set; }

        public int PortalId { get; set; }

        public short SourceMapId { get; set; }

        public short SourceX { get; set; }

        public short SourceY { get; set; }

        public short Type { get; set; }

        public short LevelRequired { get; set; }

        public short HeroLevelRequired { get; set; }

        public short RequiredItem { get; set; }

        public string NomeOggetto { get; set; }

        public byte? RequiredClass { get; set; }

        #endregion
    }
}