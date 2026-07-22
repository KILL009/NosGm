using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Networking;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.GameObject
{
    public class Account : AccountDTO
    {
        #region Instantiation

        public Account(AccountDTO input)
        {
            AccountId = input.AccountId;
            Authority = input.Authority;
            Email = input.Email;
            Name = input.Name;
            Password = input.Password;
            ReferrerId = input.ReferrerId;
            RegistrationIP = input.RegistrationIP;
            VerificationToken = input.VerificationToken;
            Language = input.Language;
        }

        #endregion

        #region Properties

        public bool IsLimited => Authority == AuthorityType.GM;

        public List<PenaltyLogDTO> PenaltyLogs
        {
            get
            {
                var logs = new PenaltyLogDTO[ServerManager.Instance.PenaltyLogs.Count + 10];
                ServerManager.Instance.PenaltyLogs.CopyTo(logs);
                return logs.Where(s => s != null && s.AccountId == AccountId).ToList();
            }
        }

        #endregion
    }
}