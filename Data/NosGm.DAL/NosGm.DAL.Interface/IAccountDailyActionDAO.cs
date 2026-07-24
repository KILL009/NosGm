using NosGm.Domain;
using System;

namespace NosGm.DAL.Interface
{
    public interface IAccountDailyActionDAO
    {
        bool IsAvailable();

        DailyActionClaimResult TryClaim(long accountId, long? characterId, string actionKey, DateTime actionDate);

        bool ReleaseClaim(long accountId, string actionKey, DateTime actionDate);
    }
}
