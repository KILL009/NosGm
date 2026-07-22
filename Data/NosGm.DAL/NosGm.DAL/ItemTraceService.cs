using NosGm.DAL.DAO;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NosGm.DAL
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

        public IEnumerable<GmCommandAuditDTO> GetDenied(int take = 30) =>
            _auditDao.LoadByOutcome(GmCommandAuditOutcome.Denied, take);

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

    /// <summary>
    /// Cached authorization service. Missing or disabled profiles preserve legacy
    /// AuthorityType checks; enabled profiles only narrow access and never elevate it.
    /// </summary>
    public sealed class StaffPermissionService
    {
        private static readonly Lazy<StaffPermissionService> LazyInstance =
            new Lazy<StaffPermissionService>(() => new StaffPermissionService(new StaffPermissionDAO()));

        private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(30);
        private readonly IStaffPermissionDAO _permissionDao;
        private readonly ConcurrentDictionary<long, CacheEntry> _cache =
            new ConcurrentDictionary<long, CacheEntry>();

        internal StaffPermissionService(IStaffPermissionDAO permissionDao)
        {
            _permissionDao = permissionDao ?? throw new ArgumentNullException(nameof(permissionDao));
        }

        public static StaffPermissionService Instance => LazyInstance.Value;

        public bool IsAvailable() => _permissionDao.IsAvailable();

        public StaffAuthorizationResult Authorize(
            long accountId,
            AuthorityType authority,
            string commandHeader,
            AuthorityType requiredAuthority)
        {
            var requiredPermission = StaffPermissionCatalog.Resolve(commandHeader, requiredAuthority);
            bool legacyAllowed = authority >= requiredAuthority;

            if (!legacyAllowed)
            {
                return Deny(false, requiredPermission, StaffPermission.None,
                    $"Legacy authority {authority} is below {requiredAuthority}.");
            }

            if (authority >= AuthorityType.DEV ||
                (StaffPermissionCatalog.IsManagementCommand(commandHeader) && authority >= AuthorityType.ADMIN))
            {
                return Allow(false, requiredPermission, StaffPermission.All);
            }

            StaffPermissionProfileDTO profile = GetProfile(accountId);
            if (profile == null || !profile.IsEnabled)
            {
                return Allow(false, requiredPermission, StaffPermission.None);
            }

            StaffPermission granted = profile.Permissions & StaffPermission.All;
            bool allowed = requiredPermission == StaffPermission.None ||
                           (granted & requiredPermission) == requiredPermission;
            return allowed
                ? Allow(true, requiredPermission, granted)
                : Deny(true, requiredPermission, granted,
                    $"Granular profile requires {requiredPermission}; granted: {StaffPermissionCatalog.Format(granted)}.");
        }

        public StaffPermissionProfileDTO GetProfile(long accountId, bool forceRefresh = false)
        {
            if (!forceRefresh && _cache.TryGetValue(accountId, out CacheEntry cached) &&
                DateTime.UtcNow - cached.LoadedAtUtc <= CacheLifetime)
            {
                return cached.Profile;
            }

            StaffPermissionProfileDTO profile = _permissionDao.LoadByAccountId(accountId);
            _cache[accountId] = new CacheEntry(profile, DateTime.UtcNow);
            return profile;
        }

        public StaffPermissionProfileDTO SetEnabled(
            long accountId,
            bool isEnabled,
            long? actorAccountId,
            long? actorCharacterId,
            string reason)
        {
            StaffPermissionProfileDTO current = GetProfile(accountId, true);
            long mask = current?.PermissionMask ?? 0L;
            return Save(accountId, mask, isEnabled, actorAccountId, actorCharacterId, reason);
        }

        public StaffPermissionProfileDTO Grant(
            long accountId,
            StaffPermission permission,
            long? actorAccountId,
            long? actorCharacterId,
            string reason)
        {
            StaffPermissionProfileDTO current = GetProfile(accountId, true);
            StaffPermission existing = current?.Permissions ?? StaffPermission.None;
            long mask = (long)((existing | permission) & StaffPermission.All);
            return Save(accountId, mask, true, actorAccountId, actorCharacterId, reason);
        }

        public StaffPermissionProfileDTO Revoke(
            long accountId,
            StaffPermission permission,
            long? actorAccountId,
            long? actorCharacterId,
            string reason)
        {
            StaffPermissionProfileDTO current = GetProfile(accountId, true);
            StaffPermission existing = current?.Permissions ?? StaffPermission.None;
            long mask = permission == StaffPermission.All
                ? 0L
                : (long)(existing & ~permission & StaffPermission.All);
            bool enabled = current?.IsEnabled ?? true;
            return Save(accountId, mask, enabled, actorAccountId, actorCharacterId, reason);
        }

        public void Invalidate(long accountId) => _cache.TryRemove(accountId, out _);

        private StaffPermissionProfileDTO Save(
            long accountId,
            long mask,
            bool enabled,
            long? actorAccountId,
            long? actorCharacterId,
            string reason)
        {
            StaffPermissionProfileDTO saved = _permissionDao.Save(
                accountId,
                mask & (long)StaffPermission.All,
                enabled,
                actorAccountId,
                actorCharacterId,
                reason);

            if (saved != null)
            {
                _cache[accountId] = new CacheEntry(saved, DateTime.UtcNow);
            }
            else
            {
                Invalidate(accountId);
            }
            return saved;
        }

        private static StaffAuthorizationResult Allow(
            bool profileEnabled,
            StaffPermission required,
            StaffPermission granted) => new StaffAuthorizationResult
        {
            Allowed = true,
            ProfileEnabled = profileEnabled,
            RequiredPermission = required,
            GrantedPermissions = granted
        };

        private static StaffAuthorizationResult Deny(
            bool profileEnabled,
            StaffPermission required,
            StaffPermission granted,
            string reason) => new StaffAuthorizationResult
        {
            Allowed = false,
            ProfileEnabled = profileEnabled,
            RequiredPermission = required,
            GrantedPermissions = granted,
            Reason = reason
        };

        private sealed class CacheEntry
        {
            public CacheEntry(StaffPermissionProfileDTO profile, DateTime loadedAtUtc)
            {
                Profile = profile;
                LoadedAtUtc = loadedAtUtc;
            }

            public StaffPermissionProfileDTO Profile { get; }

            public DateTime LoadedAtUtc { get; }
        }
    }
}
