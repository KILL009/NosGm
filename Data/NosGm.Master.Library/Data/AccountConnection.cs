using System;

namespace NosGm.Master.Library.Data
{
    public class AccountConnection
    {
        #region Instantiation

        public AccountConnection(long accountId, int sessionId, string ipAddress)
        {
            AccountId = accountId;
            SessionId = sessionId;
            IpAddress = ipAddress;
            LastPulse = DateTime.Now;
        }

        #endregion

        #region Properties

        public long AccountId { get; }

        public bool CanLoginCrossServer { get; set; }

        public long CharacterId { get; set; }

        public WorldServer ConnectedWorld { get; set; }

        public string IpAddress { get; }

        public DateTime LastPulse { get; set; }

        public WorldServer OriginWorld { get; set; }

        public int SessionId { get; }

        #endregion
    }
}