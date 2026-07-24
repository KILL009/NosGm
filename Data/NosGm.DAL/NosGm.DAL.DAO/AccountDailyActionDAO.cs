using NosGm.Core;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Domain;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace NosGm.DAL.DAO
{
    public class AccountDailyActionDAO : IAccountDailyActionDAO
    {
        public bool IsAvailable()
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<int>(
                        "SELECT CASE WHEN OBJECT_ID(N'dbo.AccountDailyAction', N'U') IS NULL THEN 0 ELSE 1 END")
                        .Single() == 1;
                }
            }
            catch (Exception exception)
            {
                Logger.Error("Unable to check AccountDailyAction availability.", exception);
                return false;
            }
        }

        public DailyActionClaimResult TryClaim(long accountId, long? characterId, string actionKey,
            DateTime actionDate)
        {
            DailyActionClaimResult result;
            if (accountId <= 0 || string.IsNullOrWhiteSpace(actionKey) || actionKey.Length > 64)
            {
                result = DailyActionClaimResult.Error;
                LogPipelineMonitor.RecordDailyAction(result);
                return result;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    int affected = context.Database.ExecuteSqlCommand(
                        @"INSERT INTO dbo.AccountDailyAction
                          (AccountId, ActionKey, ActionDate, CharacterId, CompletedAtUtc)
                          VALUES (@p0, @p1, @p2, @p3, @p4)",
                        accountId,
                        actionKey.Trim(),
                        actionDate.Date,
                        (object)characterId ?? DBNull.Value,
                        DateTime.UtcNow);

                    result = affected == 1
                        ? DailyActionClaimResult.Claimed
                        : DailyActionClaimResult.Error;
                }
            }
            catch (SqlException exception) when (exception.Number == 2601 || exception.Number == 2627)
            {
                result = DailyActionClaimResult.AlreadyClaimed;
            }
            catch (SqlException exception) when (exception.Number == 208)
            {
                Logger.Error(
                    "AccountDailyAction is unavailable. Apply Database/Migrations/20260724_AccountDailyAction.sql.",
                    exception);
                result = DailyActionClaimResult.StorageUnavailable;
            }
            catch (Exception exception)
            {
                Logger.Error("Unable to claim account daily action.", exception);
                result = DailyActionClaimResult.Error;
            }

            LogPipelineMonitor.RecordDailyAction(result);
            return result;
        }

        public bool ReleaseClaim(long accountId, string actionKey, DateTime actionDate)
        {
            if (accountId <= 0 || string.IsNullOrWhiteSpace(actionKey))
            {
                return false;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.ExecuteSqlCommand(
                        @"DELETE FROM dbo.AccountDailyAction
                          WHERE AccountId = @p0 AND ActionKey = @p1 AND ActionDate = @p2",
                        accountId,
                        actionKey.Trim(),
                        actionDate.Date) == 1;
                }
            }
            catch (Exception exception)
            {
                Logger.Error("Unable to release account daily action claim.", exception);
                return false;
            }
        }
    }
}
