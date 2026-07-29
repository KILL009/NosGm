using NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Server.State;

public sealed class ClusterCommunicationState
{
    private sealed class AccountRegistration
    {
        public required long AccountId { get; init; }

        public required int SessionId { get; init; }

        public required string IpAddress { get; init; }

        public Guid? ConnectedWorldId { get; set; }

        public long CharacterId { get; set; }

        public DateTimeOffset LastPulse { get; set; }
    }

    private sealed class WorldRegistration
    {
        public required Guid WorldId { get; init; }

        public required string EndpointIp { get; init; }

        public required int EndpointPort { get; init; }

        public required int AccountLimit { get; init; }

        public required string WorldGroup { get; init; }

        public required int ChannelId { get; init; }
    }

    public sealed class RegisterWorldResult
    {
        public CommunicationResultCode Result { get; init; }

        public int ChannelId { get; init; }
    }

    public sealed class WorldSnapshot
    {
        public Guid WorldId { get; init; }

        public string EndpointIp { get; init; }

        public int EndpointPort { get; init; }

        public int AccountLimit { get; init; }

        public int ConnectedAccounts { get; init; }

        public int ChannelId { get; init; }

        public string WorldGroup { get; init; }
    }

    private readonly Dictionary<long, AccountRegistration> _accounts = new();
    private readonly CommunicationRuntimeOptions _options;
    private readonly object _syncRoot = new();
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<Guid, WorldRegistration> _worlds = new();

    public ClusterCommunicationState(
        CommunicationRuntimeOptions options,
        TimeProvider timeProvider)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public int AccountCount
    {
        get
        {
            lock (_syncRoot)
            {
                RemoveExpiredAccounts(_timeProvider.GetUtcNow());
                return _accounts.Count;
            }
        }
    }

    public int WorldCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _worlds.Count;
            }
        }
    }

    public CommunicationResultCode RegisterAccountLogin(
        long accountId,
        int sessionId,
        string ipAddress)
    {
        lock (_syncRoot)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            RemoveExpiredAccounts(now);

            if (_accounts.TryGetValue(accountId, out AccountRegistration existing))
            {
                if (existing.SessionId == sessionId &&
                    string.Equals(
                        existing.IpAddress,
                        ipAddress,
                        StringComparison.Ordinal))
                {
                    existing.LastPulse = now;
                    return CommunicationResultCode.Success;
                }

                _accounts.Remove(accountId);
            }

            if (_accounts.Count >= _options.MaximumAccounts)
            {
                return CommunicationResultCode.CapacityExceeded;
            }

            _accounts.Add(
                accountId,
                new AccountRegistration
                {
                    AccountId = accountId,
                    SessionId = sessionId,
                    IpAddress = ipAddress,
                    LastPulse = now
                });
            return CommunicationResultCode.Success;
        }
    }

    public bool IsAccountSessionRegistered(long accountId, int sessionId)
    {
        lock (_syncRoot)
        {
            RemoveExpiredAccounts(_timeProvider.GetUtcNow());
            return TryGetExactAccount(accountId, sessionId, out _);
        }
    }

    public bool IsLoginPermitted(long accountId, int sessionId)
    {
        lock (_syncRoot)
        {
            RemoveExpiredAccounts(_timeProvider.GetUtcNow());
            return TryGetExactAccount(
                       accountId,
                       sessionId,
                       out AccountRegistration account) &&
                   !account.ConnectedWorldId.HasValue;
        }
    }

    public bool IsAccountConnected(long accountId)
    {
        lock (_syncRoot)
        {
            RemoveExpiredAccounts(_timeProvider.GetUtcNow());
            return _accounts.TryGetValue(accountId, out AccountRegistration account) &&
                   account.ConnectedWorldId.HasValue;
        }
    }

    public CommunicationResultCode ConnectAccount(
        Guid worldId,
        long accountId,
        int sessionId)
    {
        lock (_syncRoot)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            RemoveExpiredAccounts(now);
            if (!_worlds.ContainsKey(worldId) ||
                !TryGetExactAccount(
                    accountId,
                    sessionId,
                    out AccountRegistration account))
            {
                return CommunicationResultCode.NotFound;
            }

            if (account.ConnectedWorldId.HasValue &&
                account.ConnectedWorldId.Value != worldId)
            {
                return CommunicationResultCode.Conflict;
            }

            account.ConnectedWorldId = worldId;
            account.LastPulse = now;
            return CommunicationResultCode.Success;
        }
    }

    public CommunicationResultCode DisconnectAccount(
        long accountId,
        int sessionId,
        bool preserveSessionRegistration)
    {
        lock (_syncRoot)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            RemoveExpiredAccounts(now);
            if (!_accounts.TryGetValue(
                    accountId,
                    out AccountRegistration account))
            {
                return CommunicationResultCode.NotFound;
            }

            if (sessionId > 0 && account.SessionId != sessionId)
            {
                return CommunicationResultCode.Conflict;
            }

            if (preserveSessionRegistration)
            {
                account.CharacterId = 0;
                account.ConnectedWorldId = null;
                account.LastPulse = now;
                return CommunicationResultCode.Success;
            }

            _accounts.Remove(accountId);
            return CommunicationResultCode.Success;
        }
    }

    public CommunicationResultCode PulseAccount(long accountId, int sessionId)
    {
        lock (_syncRoot)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            RemoveExpiredAccounts(now);
            if (!TryGetExactAccount(
                    accountId,
                    sessionId,
                    out AccountRegistration account))
            {
                return CommunicationResultCode.NotFound;
            }

            account.LastPulse = now;
            return CommunicationResultCode.Success;
        }
    }

    public CommunicationResultCode ConnectCharacter(
        Guid worldId,
        long accountId,
        int sessionId,
        long characterId)
    {
        lock (_syncRoot)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            RemoveExpiredAccounts(now);
            if (!TryGetExactAccount(
                    accountId,
                    sessionId,
                    out AccountRegistration account) ||
                account.ConnectedWorldId != worldId ||
                !_worlds.ContainsKey(worldId))
            {
                return CommunicationResultCode.NotFound;
            }

            if (_accounts.Values.Any(candidate =>
                    candidate.AccountId != accountId &&
                    candidate.CharacterId == characterId))
            {
                return CommunicationResultCode.Conflict;
            }

            account.CharacterId = characterId;
            account.LastPulse = now;
            return CommunicationResultCode.Success;
        }
    }

    public CommunicationResultCode DisconnectCharacter(
        Guid worldId,
        long accountId,
        int sessionId,
        long characterId)
    {
        lock (_syncRoot)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            RemoveExpiredAccounts(now);
            if (!TryGetExactAccount(
                    accountId,
                    sessionId,
                    out AccountRegistration account) ||
                account.ConnectedWorldId != worldId ||
                account.CharacterId != characterId)
            {
                return CommunicationResultCode.NotFound;
            }

            account.CharacterId = 0;
            account.ConnectedWorldId = null;
            account.LastPulse = now;
            return CommunicationResultCode.Success;
        }
    }

    public RegisterWorldResult RegisterWorldServer(
        Guid worldId,
        string endpointIp,
        int endpointPort,
        int accountLimit,
        string worldGroup)
    {
        lock (_syncRoot)
        {
            if (_worlds.TryGetValue(worldId, out WorldRegistration existing))
            {
                bool sameRegistration =
                    string.Equals(
                        existing.EndpointIp,
                        endpointIp,
                        StringComparison.Ordinal) &&
                    existing.EndpointPort == endpointPort &&
                    existing.AccountLimit == accountLimit &&
                    string.Equals(
                        existing.WorldGroup,
                        worldGroup,
                        StringComparison.Ordinal);
                return new RegisterWorldResult
                {
                    Result = sameRegistration
                        ? CommunicationResultCode.Success
                        : CommunicationResultCode.Conflict,
                    ChannelId = sameRegistration ? existing.ChannelId : 0
                };
            }

            if (_worlds.Count >= _options.MaximumWorlds)
            {
                return new RegisterWorldResult
                {
                    Result = CommunicationResultCode.CapacityExceeded
                };
            }

            int channelId = endpointPort == _options.GlacernonPort
                ? CommunicationRuntimeOptions.GlacernonChannelId
                : FindAvailableChannel(worldGroup);
            if (channelId <= 0)
            {
                return new RegisterWorldResult
                {
                    Result = CommunicationResultCode.CapacityExceeded
                };
            }

            _worlds.Add(
                worldId,
                new WorldRegistration
                {
                    WorldId = worldId,
                    EndpointIp = endpointIp,
                    EndpointPort = endpointPort,
                    AccountLimit = accountLimit,
                    WorldGroup = worldGroup,
                    ChannelId = channelId
                });
            return new RegisterWorldResult
            {
                Result = CommunicationResultCode.Success,
                ChannelId = channelId
            };
        }
    }

    public CommunicationResultCode UnregisterWorldServer(Guid worldId)
    {
        lock (_syncRoot)
        {
            if (!_worlds.Remove(worldId))
            {
                return CommunicationResultCode.NotFound;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            foreach (AccountRegistration account in _accounts.Values)
            {
                if (account.ConnectedWorldId == worldId)
                {
                    account.CharacterId = 0;
                    account.ConnectedWorldId = null;
                    account.LastPulse = now;
                }
            }

            return CommunicationResultCode.Success;
        }
    }

    public IReadOnlyList<WorldSnapshot> ListVisibleWorldServers()
    {
        lock (_syncRoot)
        {
            RemoveExpiredAccounts(_timeProvider.GetUtcNow());
            return _worlds.Values
                .Where(world =>
                    world.ChannelId !=
                    CommunicationRuntimeOptions.GlacernonChannelId)
                .OrderBy(world => world.WorldGroup, StringComparer.Ordinal)
                .ThenBy(world => world.ChannelId)
                .Select(world => new WorldSnapshot
                {
                    WorldId = world.WorldId,
                    EndpointIp = world.EndpointIp,
                    EndpointPort = world.EndpointPort,
                    AccountLimit = world.AccountLimit,
                    ConnectedAccounts = _accounts.Values.Count(account =>
                        account.ConnectedWorldId == world.WorldId),
                    ChannelId = world.ChannelId,
                    WorldGroup = world.WorldGroup
                })
                .ToArray();
        }
    }

    private int FindAvailableChannel(string worldGroup)
    {
        var usedChannels = _worlds.Values
            .Where(world =>
                world.ChannelId !=
                    CommunicationRuntimeOptions.GlacernonChannelId &&
                string.Equals(
                    world.WorldGroup,
                    worldGroup,
                    StringComparison.Ordinal))
            .Select(world => world.ChannelId)
            .ToHashSet();
        for (int channelId = 1;
             channelId <= CommunicationRuntimeOptions.MaximumChannelsPerGroup;
             channelId++)
        {
            if (!usedChannels.Contains(channelId))
            {
                return channelId;
            }
        }

        return 0;
    }

    private void RemoveExpiredAccounts(DateTimeOffset now)
    {
        DateTimeOffset threshold =
            now.AddSeconds(-_options.SessionTtlSeconds);
        long[] expired = _accounts
            .Where(pair => pair.Value.LastPulse <= threshold)
            .Select(pair => pair.Key)
            .ToArray();
        foreach (long accountId in expired)
        {
            _accounts.Remove(accountId);
        }
    }

    private bool TryGetExactAccount(
        long accountId,
        int sessionId,
        out AccountRegistration account)
    {
        return _accounts.TryGetValue(accountId, out account) &&
               account.SessionId == sessionId;
    }
}
