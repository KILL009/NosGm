using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.DAL.EF
{
    [Table("FishInformations")]
    public class FishingInformations
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public short FishVNum { get; set; }

        public short Probability { get; set; }

        public short MapId1 { get; set; }

        public short MapId2 { get; set; }

        public short MapId3 { get; set; }

        public double MinFishLength { get; set; }

        public double MaxFishLength { get; set; }

        public bool IsFish { get; set; }
    }
}
