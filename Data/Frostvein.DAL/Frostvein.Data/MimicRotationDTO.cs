using Frostvein.Domain;
using System.ComponentModel.DataAnnotations;

namespace Frostvein.Data
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
