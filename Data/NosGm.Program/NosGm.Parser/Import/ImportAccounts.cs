using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using System;
using System.Collections.Generic;

namespace NosGm.Parser.Import
{
    public class ImportAccounts : IImport
    {
        public void Import()
        {
            var accounts = new List<AccountDTO>();
            if (!DAOFactory.AccountDAO.ContainsAccounts())
            {
                if (!PasswordHashService.TryHashPassword("HelloImZuya", out string passwordHash))
                {
                    throw new InvalidOperationException("Unable to create the initial account password hash.");
                }

                accounts.Add(new AccountDTO
                {
                    AccountId = 1,
                    Authority = AuthorityType.ADMIN,
                    Name = "Zuya",
                    Password = passwordHash
                });
            }

            ;

            DAOFactory.AccountDAO.Insert(accounts);
            Logger.Log.Info($"{accounts.Count} Accounts parsed");
        }
    }
}