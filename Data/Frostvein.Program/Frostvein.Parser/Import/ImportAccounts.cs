using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using System.Collections.Generic;

namespace Frostvein.Parser.Import
{
    public class ImportAccounts : IImport
    {
        public void Import()
        {
            var accounts = new List<AccountDTO>();
            if (!DAOFactory.AccountDAO.ContainsAccounts())
            {
                accounts.Add(new AccountDTO
                {
                    AccountId = 1,
                    Authority = AuthorityType.ADMIN,
                    Name = "Zuya",
                    Password = CryptographyBase.Sha512("HelloImZuya")
                });
            }

            ;

            DAOFactory.AccountDAO.Insert(accounts);
            Logger.Log.Info($"{accounts.Count} Accounts parsed");
        }
    }
}