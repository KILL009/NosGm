using NosGm.DAL.EF;
using NosGm.Data;

namespace NosGm.Mapper.Mappers
{
    public static class AccountMapper
    {
        #region Methods

        public static bool ToAccount(AccountDTO input, Account output)
        {
            if (input == null) return false;

            output.AccountId = input.AccountId;
            output.Authority = input.Authority;
            output.Email = input.Email;
            output.Name = input.Name;
            output.Password = input.Password;
            output.RegistrationIP = input.RegistrationIP;
            output.VerificationToken = input.VerificationToken;
            output.Language = input.Language;
            return true;
    }

        public static bool ToAccountDTO(Account input, AccountDTO output)
        {
            if (input == null) return false;

            output.AccountId = input.AccountId;
            output.Authority = input.Authority;
            output.Email = input.Email;
            output.Name = input.Name;
            output.Password = input.Password;
            output.RegistrationIP = input.RegistrationIP;
            output.VerificationToken = input.VerificationToken;
            output.Language = input.Language;
            return true;
        }

        #endregion
    }
}