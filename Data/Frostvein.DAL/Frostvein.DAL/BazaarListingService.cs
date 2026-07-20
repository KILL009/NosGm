using Frostvein.DAL.DAO;
using Frostvein.Data;
using System;

namespace Frostvein.DAL
{
    public sealed class BazaarListingService
    {
        private static readonly Lazy<BazaarListingService> LazyInstance =
            new Lazy<BazaarListingService>(() => new BazaarListingService(new BazaarListingDAO()));

        private readonly BazaarListingDAO _dao;

        internal BazaarListingService(BazaarListingDAO dao)
        {
            _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        }

        public static BazaarListingService Instance => LazyInstance.Value;

        public BazaarListingResult Commit(BazaarListingDTO request) => _dao.Commit(request);
    }
}
