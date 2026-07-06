namespace Frostvein.DAL.EF
{
    public class CharacterTitle
    {
        public long CharacterTitleId { get; set; }

        public virtual Character Character { get; set; }

        public long CharacterId { get; set; }

        public long TitleVnum { get; set; }

        public byte Stat { get; set; }
    }
}