using NosGm.Domain;
using System;

namespace NosGm.Master.Library.Data
{
    [Serializable]
    public class MapInstance
    {
        #region Properties

        public short MapId { get; set; }

        public Guid MapInstanceId { get; set; }

        public MapInstanceType Type { get; set; }

        public short XLength { get; set; }

        public short YLength { get; set; }

        #endregion
    }
}