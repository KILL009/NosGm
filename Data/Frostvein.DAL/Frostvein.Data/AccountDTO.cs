using Frostvein.Domain;
using System;

namespace Frostvein.Data
{
    [Serializable]
    public class AccountDTO
    {
        #region Properties

        public long AccountId { get; set; }

        public AuthorityType Authority { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

        public string Password { get; set; }

        public long ReferrerId { get; set; }

        public string RegistrationIP { get; set; }

        public string VerificationToken { get; set; }

        public string Language { get; set; }

        #endregion
    }
}