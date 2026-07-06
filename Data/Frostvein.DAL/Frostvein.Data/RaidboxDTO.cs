using System;

namespace Frostvein.Data
{
    [Serializable]
    public class RaidboxDTO
    {
        #region Properties

        public bool IsRareRandom { get; set; }

        public short ItemGeneratedAmount { get; set; }

        public short ItemGeneratedDesign { get; set; }

        public short ItemGeneratedVNum { get; set; }

        public byte MaximumOriginalItemRare { get; set; }

        public byte MinimumOriginalItemRare { get; set; }

        public short OriginalItemDesign { get; set; }

        public short OriginalItemVNum { get; set; }

        public short Probability { get; set; }

        public short RaidboxId { get; set; }

        #endregion
    }
}