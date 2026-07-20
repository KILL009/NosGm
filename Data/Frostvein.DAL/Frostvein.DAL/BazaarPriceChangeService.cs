using Frostvein.DAL.DAO;
using Frostvein.Data;
using System;

namespace Frostvein.DAL
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
