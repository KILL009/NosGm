using Frostvein.DAL.DAO;
using Frostvein.Data;
using System;

namespace Frostvein.DAL
{
    public sealed class BazaarListingService
    {
        private static readonly Lazy<BazaarListingService> LazyInstance =
            new Lazy<BazaarListingService>(() => new BazaarListingService(new BazaarListingLiveStateDAO()));

        private readonly BazaarListingLiveStateDAO _dao;

        internal BazaarListingService(BazaarListingLiveStateDAO dao)
        {
            _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        }

        public static BazaarListingService Instance => LazyInstance.Value;

        public BazaarListingResult Commit(BazaarListingDTO request) => _dao.Commit(request);
    }
}
