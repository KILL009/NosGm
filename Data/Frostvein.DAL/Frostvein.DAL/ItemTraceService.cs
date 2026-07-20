using Frostvein.DAL.DAO;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Frostvein.DAL
{
    /// <summary>
    /// Central entry point for recording item mutations. Callers should create one
    /// OperationId per business operation and increment Sequence for every affected item.
    /// </summary>
    public sealed class ItemTraceService
    {
        private static readonly Lazy<ItemTraceService> LazyInstance =
            new Lazy<ItemTraceService>(() => new ItemTraceService(new ItemTraceDAO()));

        private readonly IItemTraceDAO _traceDao;

        internal ItemTraceService(IItemTraceDAO traceDao)
        {
            _traceDao = traceDao ?? throw new ArgumentNullException(nameof(traceDao));
        }

        public static ItemTraceService Instance => LazyInstance.Value;

        public Guid BeginOperation() => Guid.NewGuid();

        public ItemTraceDTO Record(
            Guid operationId,
            int sequence,
            ItemTraceAction action,
            ItemTraceSource source,
            ItemInstanceDTO before,
            ItemInstanceDTO after,
            long? actorAccountId = null,
            long? actorCharacterId = null,
            string actorName = null,
            string reason = null,
            object metadata = null,
            bool isSuspicious = false)
        {
            if (operationId == Guid.Empty)
            {
                throw new ArgumentException("OperationId must be created once and reused for the whole operation.", nameof(operationId));
            }

            if (sequence < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            var item = after ?? before;
            if (item == null)
            {
                throw new ArgumentException("At least one item snapshot is required.");
            }

            if (before != null && after != null && before.Id != after.Id)
            {
                throw new ArgumentException("Before and after snapshots must describe the same item instance.");
            }

            var serial = item.EquipmentSerialId == Guid.Empty
                ? (Guid?)null
                : item.EquipmentSerialId;

            var trace = new ItemTraceDTO
            {
                OperationId = operationId,
                Sequence = sequence,
                OccurredAtUtc = DateTime.UtcNow,
                Action = action,
                Source = source,
                ItemInstanceId = item.Id,
                EquipmentSerialId = serial,
                ItemVNum = item.ItemVNum,
                AmountBefore = before?.Amount,
                AmountAfter = after?.Amount,
                OwnerCharacterIdBefore = before?.CharacterId,
                OwnerCharacterIdAfter = after?.CharacterId,
                InventoryTypeBefore = before?.Type,
                InventoryTypeAfter = after?.Type,
                SlotBefore = before?.Slot,
                SlotAfter = after?.Slot,
                ActorAccountId = actorAccountId,
                ActorCharacterId = actorCharacterId,
                ActorName = actorName,
                Reason = reason,
                Metadata = SerializeMetadata(metadata),
                IsSuspicious = isSuspicious
            };

            return _traceDao.InsertIfMissing(trace);
        }

        public IEnumerable<ItemTraceDTO> GetHistory(Guid itemInstanceId, int take = 100) =>
            _traceDao.LoadByItemInstanceId(itemInstanceId, take);

        public IEnumerable<ItemTraceDTO> GetSerialHistory(Guid equipmentSerialId, int take = 100) =>
            _traceDao.LoadByEquipmentSerialId(equipmentSerialId, take);

        public IEnumerable<ItemTraceDTO> GetOperation(Guid operationId) =>
            _traceDao.LoadByOperationId(operationId);

        public IEnumerable<ItemTraceDTO> GetSuspicious(int take = 100) =>
            _traceDao.LoadSuspicious(take);

        public IEnumerable<DuplicateEquipmentSerialItemDTO> GetCurrentItemsBySerial(
            Guid equipmentSerialId,
            int take = 100) =>
            _traceDao.LoadCurrentItemsByEquipmentSerialId(equipmentSerialId, take);

        public IEnumerable<DuplicateEquipmentSerialItemDTO> GetDuplicateEquipmentSerialItems(int takeGroups = 20) =>
            _traceDao.LoadDuplicateEquipmentSerialItems(takeGroups);

        private static string SerializeMetadata(object metadata)
        {
            if (metadata == null) return null;
            if (metadata is string text) return text;
            return JsonConvert.SerializeObject(metadata, Formatting.None);
        }
    }

    /// <summary>
    /// Central service used by the command execution bridge and the read-only GM
    /// investigation command. Command text is sanitized before persistence.
    /// </summary>
    public sealed class GmCommandAuditService
    {
        private static readonly Lazy<GmCommandAuditService> LazyInstance =
            new Lazy<GmCommandAuditService>(() => new GmCommandAuditService(new GmCommandAuditDAO()));

        private readonly IGmCommandAuditDAO _auditDao;

        internal GmCommandAuditService(IGmCommandAuditDAO auditDao)
        {
            _auditDao = auditDao ?? throw new ArgumentNullException(nameof(auditDao));
        }

        public static GmCommandAuditService Instance => LazyInstance.Value;

        public bool IsAvailable() => _auditDao.IsAvailable();

        public GmCommandAuditDTO Record(
            long? accountId,
            long? characterId,
            string characterName,
            AuthorityType authority,
            string commandHeader,
            string commandText,
            AuthorityType requiredAuthority,
            GmCommandAuditOutcome outcome,
            string ipAddress,
            int channelId,
            short? mapId,
            int? sessionId,
            Exception exception = null)
        {
            string normalizedHeader = NormalizeHeader(commandHeader);
            var audit = new GmCommandAuditDTO
            {
                CorrelationId = Guid.NewGuid(),
                OccurredAtUtc = DateTime.UtcNow,
                AccountId = accountId,
                CharacterId = characterId,
                CharacterName = Limit(characterName, 64),
                Authority = authority,
                CommandHeader = normalizedHeader,
                CommandText = SanitizeCommand(normalizedHeader, commandText),
                RequiredAuthority = requiredAuthority,
                Outcome = outcome,
                IpAddress = Limit(ipAddress, 64),
                ChannelId = channelId,
                MapId = mapId,
                SessionId = sessionId,
                Failure = exception == null
                    ? null
                    : Limit($"{exception.GetType().Name}: {exception.Message}", 2000)
            };

            return _auditDao.Insert(audit);
        }

        public IEnumerable<GmCommandAuditDTO> GetRecent(int take = 30) =>
            _auditDao.LoadRecent(take);

        public IEnumerable<GmCommandAuditDTO> GetByAccountId(long accountId, int take = 30) =>
            _auditDao.LoadByAccountId(accountId, take);

        public IEnumerable<GmCommandAuditDTO> GetByCharacterId(long characterId, int take = 30) =>
            _auditDao.LoadByCharacterId(characterId, take);

        public IEnumerable<GmCommandAuditDTO> GetByCommand(string commandHeader, int take = 30) =>
            _auditDao.LoadByCommand(commandHeader, take);

        public IEnumerable<GmCommandAuditDTO> GetFailed(int take = 30) =>
            _auditDao.LoadByOutcome(GmCommandAuditOutcome.Failed, take);

        private static string SanitizeCommand(string header, string commandText)
        {
            string normalized = NormalizeWhitespace(commandText);
            if (IsSensitiveHeader(header))
            {
                return $"{header} <redacted>";
            }

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return header;
            }

            return Limit(normalized, 1000);
        }

        private static bool IsSensitiveHeader(string header)
        {
            string value = (header ?? string.Empty).ToLowerInvariant();
            return value.Contains("password") || value.Contains("passwd") ||
                   value.Contains("secret") || value.Contains("token") ||
                   value.Contains("auth") || value.Contains("key") ||
                   string.Equals(value, "$sudo", StringComparison.Ordinal);
        }

        private static string NormalizeHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "<unknown>";
            string normalized = value.Trim();
            if (!normalized.StartsWith("$", StringComparison.Ordinal)) normalized = "$" + normalized;
            return Limit(normalized, 64);
        }

        private static string NormalizeWhitespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        }

        private static string Limit(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string trimmed = value.Trim();
            return trimmed.Length <= maximumLength ? trimmed : trimmed.Substring(0, maximumLength);
        }
    }
}
