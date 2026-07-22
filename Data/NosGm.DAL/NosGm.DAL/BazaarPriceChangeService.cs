using NosGm.DAL.DAO;
using NosGm.Data;
using System;

namespace NosGm.DAL
{
    public sealed class BazaarPriceChangeService
    {
        private static readonly Lazy<BazaarPriceChangeService> LazyInstance =
            new Lazy<BazaarPriceChangeService>(() => new BazaarPriceChangeService(new BazaarPriceChangeDAO()));

        private readonly BazaarPriceChangeDAO _dao;

        internal BazaarPriceChangeService(BazaarPriceChangeDAO dao)
        {
            _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        }

        public static BazaarPriceChangeService Instance => LazyInstance.Value;

        public BazaarPriceChangeResult Commit(BazaarPriceChangeDTO request) => _dao.Commit(request);
    }
}
