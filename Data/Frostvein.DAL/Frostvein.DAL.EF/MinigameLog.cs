namespace Frostvein.DAL.EF
{
    public class MinigameLog
    {
        #region Properties

        public virtual Character Character { get; set; }

        public long CharacterId { get; set; }

        public long EndTime { get; set; }

        public byte Minigame { get; set; }

        public long MinigameLogId { get; set; }

        public int Score { get; set; }

        public long StartTime { get; set; }

        #endregion
    }
}