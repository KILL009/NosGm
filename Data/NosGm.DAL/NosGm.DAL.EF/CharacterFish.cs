using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosGm.DAL.EF
{
    [Table("CharacterFish")]
    public class CharacterFish
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Id { get; set; }

        public long CharacterId { get; set; }

        public short FishId { get; set; }

        public int FishCount { get; set; }

        public int MaxLength { get; set; }
    }
}
