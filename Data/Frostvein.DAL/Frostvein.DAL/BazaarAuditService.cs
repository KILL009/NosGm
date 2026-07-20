using Frostvein.DAL.DAO;
using Frostvein.Data;
using System;
using System.Collections.Generic;

namespace Frostvein.DAL
{
    public sealed class BazaarAuditService
    {
        private static readonly Lazy<BazaarAuditService> LazyInstance =
            new Lazy<BazaarAuditService>(() => new BazaarAuditService(new BazaarAuditDAO()));

        private readonly BazaarAuditDAO _dao;

        internal BazaarAuditService(BazaarAuditDAO dao)
        {
            _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        }

        public static BazaarAuditService Instance => LazyInstance.Value;

        public bool IsAvailable() => _dao.IsAvailable();

        public BazaarAuditStatusDTO GetStatus() => _dao.LoadStatus();

        public BazaarAuditListingDTO GetListing(long bazaarItemId) => _dao.LoadListing(bazaarItemId);

        public IEnumerable<BazaarAuditEventDTO> GetRecent(int take = 20) => _dao.LoadRecent(take);

        public IEnumerable<BazaarAuditEventDTO> GetByListing(long bazaarItemId, int take = 30) =>
            _dao.LoadByListing(bazaarItemId, take);

        public IEnumerable<BazaarAuditEventDTO> GetByCharacter(long characterId, int take = 30) =>
            _dao.LoadByCharacter(characterId, take);

        public IEnumerable<BazaarAuditEventDTO> GetByItem(Guid itemInstanceId, int take = 30) =>
            _dao.LoadByItem(itemInstanceId, take);

        public IEnumerable<BazaarAuditAnomalyDTO> GetAnomalies(int take = 30) => _dao.LoadAnomalies(take);
    }
}
