using Frostvein.DAL.DAO;
using Frostvein.Data;
using System;

namespace Frostvein.DAL
{
    public sealed class BazaarRecollectService
    {
        private static readonly Lazy<BazaarRecollectService> LazyInstance =
            new Lazy<BazaarRecollectService>(() => new BazaarRecollectService(new BazaarRecollectDAO()));

        private readonly BazaarRecollectDAO _dao;

        internal BazaarRecollectService(BazaarRecollectDAO dao)
        {
            _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        }

        public static BazaarRecollectService Instance => LazyInstance.Value;

        public BazaarRecollectResult Commit(BazaarRecollectDTO request) => _dao.Commit(request);
    }
}
