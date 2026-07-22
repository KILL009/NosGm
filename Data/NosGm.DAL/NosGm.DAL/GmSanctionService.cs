using NosGm.DAL.DAO;
using NosGm.DAL.Interface;
using NosGm.Data;
using System;
using System.Collections.Generic;

namespace NosGm.DAL
{
    public sealed class GmSanctionService
    {
        private static readonly Lazy<GmSanctionService> LazyInstance =
            new Lazy<GmSanctionService>(() => new GmSanctionService(new GmSanctionDAO()));

        private readonly IGmSanctionDAO _dao;

        internal GmSanctionService(IGmSanctionDAO dao)
        {
            _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        }

        public static GmSanctionService Instance => LazyInstance.Value;

        public bool IsAvailable() => _dao.IsAvailable();

        public GmSanctionResultDTO Execute(GmSanctionRequestDTO request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.CaseId <= 0) return Failed("A valid GM case is required.");
            if (request.SubjectAccountId <= 0) return Failed("A valid target account is required.");
            if (request.ActorAccountId <= 0) return Failed("A valid staff account is required.");
            if (string.IsNullOrWhiteSpace(request.Reason)) return Failed("A reason is required.");

            request.Reason = Limit(request.Reason, 255);
            request.SubjectName = Limit(request.SubjectName, 64);
            request.ActorName = Limit(request.ActorName, 64);
            request.IpAddress = Limit(request.IpAddress, 64);
            if (request.OperationId == Guid.Empty) request.OperationId = Guid.NewGuid();
            if (request.OccurredAtUtc == default(DateTime)) request.OccurredAtUtc = DateTime.UtcNow;
            if (request.PenaltyStart == default(DateTime)) request.PenaltyStart = DateTime.Now;

            return _dao.Execute(request);
        }

        public IEnumerable<GmSanctionActionDTO> GetByCase(long caseId, int take = 20) =>
            _dao.LoadByCase(caseId, take);

        private static GmSanctionResultDTO Failed(string error) => new GmSanctionResultDTO
        {
            Success = false,
            Error = error
        };

        private static string Limit(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string trimmed = value.Trim();
            return trimmed.Length <= maximumLength ? trimmed : trimmed.Substring(0, maximumLength);
        }
    }
}
