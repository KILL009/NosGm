using NosGm.DAL.DAO;
using NosGm.Data;
using System;

namespace NosGm.DAL
{
    public sealed class TradeCommitService
    {
        private static readonly Lazy<TradeCommitService> LazyInstance =
            new Lazy<TradeCommitService>(() => new TradeCommitService(new TradeCommitDAO()));

        private readonly TradeCommitDAO _dao;

        internal TradeCommitService(TradeCommitDAO dao)
        {
            _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        }

        public static TradeCommitService Instance => LazyInstance.Value;

        public TradeCommitResult Commit(TradeCommitDTO request) => _dao.Commit(request);
    }
}
