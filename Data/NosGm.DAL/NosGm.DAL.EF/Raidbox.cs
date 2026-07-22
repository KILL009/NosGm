using System.ComponentModel.DataAnnotations;

namespace NosGm.DAL.EF
{
    public class Raidbox
    {
        #region Properties

        public bool IsRareRandom { get; set; }

        public virtual Item ItemGenerated { get; set; }

        public short ItemGeneratedAmount { get; set; }

        public short ItemGeneratedDesign { get; set; }

        public short ItemGeneratedVNum { get; set; }

        public byte MaximumOriginalItemRare { get; set; }

        public byte MinimumOriginalItemRare { get; set; }

        public virtual Item OriginalItem { get; set; }

        public short OriginalItemDesign { get; set; }

        public short OriginalItemVNum { get; set; }

        public short Probability { get; set; }

        [Key] public short RaidboxId { get; set; }

        #endregion
    }
}