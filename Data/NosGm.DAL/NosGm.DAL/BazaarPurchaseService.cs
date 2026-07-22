using NosGm.DAL.DAO;
using NosGm.Data;
using System;

namespace NosGm.DAL
{
    public sealed class BazaarPurchaseService
    {
        private static readonly Lazy<BazaarPurchaseService> LazyInstance =
            new Lazy<BazaarPurchaseService>(() => new BazaarPurchaseService(new BazaarPurchaseDAO()));

        private readonly BazaarPurchaseDAO _dao;

        internal BazaarPurchaseService(BazaarPurchaseDAO dao)
        {
            _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        }

        public static BazaarPurchaseService Instance => LazyInstance.Value;

        public BazaarPurchaseResult Commit(BazaarPurchaseDTO request) => _dao.Commit(request);
    }
}
