using NosGm.Domain;
using System.ComponentModel.DataAnnotations;

namespace NosGm.Data
{
    public class MimicRotationDTO
    {
        [Key]
        public long Id { get; set; }

        public int ItemVnum { get; set; }

        public short ItemAmount { get; set; }

        public double Percentage { get; set; }

        public MimicRotationType RotationType { get; set; }

        public bool IsSuperReward { get; set; }
    }
}
