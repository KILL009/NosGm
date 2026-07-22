namespace NosGm.Data
{
    public class MinigameLogDTO
    {
        #region Properties

        public long CharacterId { get; set; }

        public long EndTime { get; set; }

        public byte Minigame { get; set; }

        public long MinigameLogId { get; set; }

        public int Score { get; set; }

        public long StartTime { get; set; }

        #endregion
    }
}