using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Data.Enums;
using NosGm.Domain;
using NosGm.Mapper.Mappers;
using NosGm.LoggerService;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.DAL.DAO
{
    public class AccountDAO : IAccountDAO
    {
        #region Methods

        public bool ContainsAccounts()
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Account.AsNoTracking().Any();
                }
            }
            catch (Exception e)
            {
                LoggerService.LogServer.Logger.LogAsync($"Message: {e.Message} | Source: {e.Source} | Data: {e.Data}", LogType.ERROR);
                throw;
            }
        }

        public void Insert(List<AccountDTO> account)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var Account in account)
                    {
                        var entity = new Account();
                        AccountMapper.ToAccount(Account, entity);
                        context.Account.Add(entity);
                    }

                    context.Configuration.AutoDetectChangesEnabled = true;
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                LoggerService.LogServer.Logger.LogAsync($"Message: {e.Message} | Source: {e.Source} | Data: {e.Data}", LogType.ERROR);
            }
        }

        public DeleteResult Delete(long accountId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var account = context.Account.FirstOrDefault(c => c.AccountId.Equals(accountId));

                    if (account != null)
                    {
                        context.Account.Remove(account);
                        context.SaveChanges();
                    }

                    return DeleteResult.Deleted;
                }
            }
            catch (Exception e)
            {
                LoggerService.LogServer.Logger.LogAsync($"Message: {e.Message} | Source: {e.Source} | Data: {e.Data}", LogType.ERROR);
                return DeleteResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(ref AccountDTO account)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var accountId = account.AccountId;
                    var entity = context.Account.FirstOrDefault(c => c.AccountId.Equals(accountId));

                    if (entity == null)
                    {
                        account = insert(account, context);
                        return SaveResult.Inserted;
                    }

                    account = update(entity, account, context);
                    return SaveResult.Updated;
                }
            }
            catch (Exception e)
            {
                LoggerService.LogServer.Logger.LogAsync($"Message: {e.Message} | Source: {e.Source} | Data: {e.Data}", LogType.ERROR);
                return SaveResult.Error;
            }
        }

        public AccountDTO LoadById(long accountId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var account = context.Account.FirstOrDefault(a => a.AccountId.Equals(accountId));
                    if (account != null)
                    {
                        var accountDTO = new AccountDTO();
                        if (AccountMapper.ToAccountDTO(account, accountDTO))
                        {
                            return accountDTO;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LoggerService.LogServer.Logger.LogAsync($"Message: {e.Message} | Source: {e.Source} | Data: {e.Data}", LogType.ERROR);
            }

            return null;
        }

        public AccountDTO LoadByName(string name)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var account = context.Account.FirstOrDefault(a => a.Name.Equals(name));
                    if (account != null)
                    {
                        var accountDTO = new AccountDTO();
                        if (AccountMapper.ToAccountDTO(account, accountDTO))
                        {
                            return accountDTO;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LoggerService.LogServer.Logger.LogAsync($"Message: {e.Message} | Source: {e.Source} | Data: {e.Data}", LogType.ERROR);
            }

            return null;
        }

        public bool TryUpgradePassword(long accountId, string expectedPassword, string upgradedPassword)
        {
            if (accountId <= 0 || expectedPassword == null || string.IsNullOrWhiteSpace(upgradedPassword) ||
                upgradedPassword.Length > 255)
            {
                return false;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    Account entity = context.Account.FirstOrDefault(a => a.AccountId.Equals(accountId));
                    if (entity == null ||
                        !string.Equals(entity.Password, expectedPassword, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    entity.Password = upgradedPassword;
                    return context.SaveChanges() == 1;
                }
            }
            catch (Exception e)
            {
                LoggerService.LogServer.Logger.LogAsync(
                    $"Unable to upgrade password hash for AccountId={accountId}. Message: {e.Message} | Source: {e.Source}",
                    LogType.ERROR);
                return false;
            }
        }

        public bool TryUpdateLanguage(long accountId, string language)
        {
            if (accountId <= 0 || string.IsNullOrWhiteSpace(language) || language.Length > 8)
            if (accountId <= 0 || string.IsNullOrWhiteSpace(language) || language.Length > 10)
            {
                return false;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    Account entity = context.Account.FirstOrDefault(a => a.AccountId.Equals(accountId));
                    if (entity == null)
                    {
                        return false;
                    }

                    if (string.Equals(entity.Language, language, StringComparison.OrdinalIgnoreCase))
                    if (string.Equals(entity.Language, language, StringComparison.Ordinal))
                    {
                        return true;
                    }

                    entity.Language = language;
                    return context.SaveChanges() == 1;
                }
            }
            catch (Exception e)
            {
                LoggerService.LogServer.Logger.LogAsync(
                    $"Unable to update account language for AccountId={accountId}. Message: {e.Message} | Source: {e.Source}",
                    LogType.ERROR);
                return false;
            }
        }

        public async Task WriteGeneralLog(long accountId, string ipAddress, long? characterId, GeneralLogType logType,
            string logData)
        {
            var dto = new GeneralLogDTO
            {
                AccountId = accountId,
                IpAddress = ipAddress,
                Timestamp = DateTime.Now,
                LogType = logType.ToString(),
                LogData = logData,
                CharacterId = characterId
            };

            if (GeneralLogBatchWriter.TryEnqueue(dto))
            {
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            bool success = false;
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    var log = new GeneralLog
                    {
                        AccountId = dto.AccountId,
                        IpAddress = dto.IpAddress,
                        Timestamp = dto.Timestamp,
                        LogType = dto.LogType,
                        LogData = dto.LogData,
                        CharacterId = dto.CharacterId
                    };

                    context.GeneralLog.Add(log);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    success = true;
                }
            }
            catch (Exception e)
            {
                await LoggerService.LogServer.Logger.LogAsync($"Message: {e.Message} | Source: {e.Source} | Data: {e.Data}", LogType.ERROR)
                    .ConfigureAwait(false);
            }
            finally
            {
                stopwatch.Stop();
                LogPipelineMonitor.RecordGeneralLogWrite(1, stopwatch.ElapsedTicks, success);
            }
        }

        private static AccountDTO insert(AccountDTO account, NosGmContext context)
        {
            var entity = new Account();
            AccountMapper.ToAccount(account, entity);
            context.Account.Add(entity);
            context.SaveChanges();
            AccountMapper.ToAccountDTO(entity, account);
            return account;
        }

        private static AccountDTO update(Account entity, AccountDTO account, NosGmContext context)
        {
            if (entity != null)
            {
                AccountMapper.ToAccount(account, entity);
                context.Entry(entity).State = EntityState.Modified;
                context.SaveChanges();
            }

            if (AccountMapper.ToAccountDTO(entity, account))
            {
                return account;
            }

            return null;
        }

        #endregion
    }
}
