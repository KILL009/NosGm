using System.Security.Cryptography;
using System.Threading.Channels;
using Google.Protobuf;
using NosGm.Cluster.Contracts.Communication.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Server.State;

public enum CallbackSubscriptionOpenResult
{
    Success = 0,
    InvalidResumeCursor = 1,
    Conflict = 2,
    CapacityExceeded = 3,
    NotFound = 4
}

public enum CallbackSubscriptionTerminationReason
{
    None = 0,
    QueueOverflow = 1,
    WorldUnregistered = 2
}

public sealed class CommunicationCallbackPublishResult
{
    public WireV1.CommunicationResultCode Result { get; init; }

    public ulong Sequence { get; init; }

    public uint MatchedSubscribers { get; init; }
}

public sealed class CommunicationCallbackSubscription : IAsyncDisposable
{
    private readonly CommunicationCallbackHub _owner;
    private readonly string _subscriberKey;
    private readonly Guid _leaseId;
    private readonly CommunicationCallbackHub.ActiveSubscription _active;
    private int _disposed;

    internal CommunicationCallbackSubscription(
        CommunicationCallbackHub owner,
        string subscriberKey,
        Guid leaseId,
        CommunicationCallbackHub.ActiveSubscription active,
        IReadOnlyList<WireV1.CommunicationCallbackEnvelope> replayEvents)
    {
        _owner = owner;
        _subscriberKey = subscriberKey;
        _leaseId = leaseId;
        _active = active;
        ReplayEvents = replayEvents;
    }

    public IReadOnlyList<WireV1.CommunicationCallbackEnvelope> ReplayEvents
    {
        get;
    }

    public ChannelReader<WireV1.CommunicationCallbackEnvelope> PendingEvents =>
        _active.Channel.Reader;

    public CancellationToken TerminationToken => _active.Termination.Token;

    public CallbackSubscriptionTerminationReason TerminationReason =>
        _active.TerminationReason;

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _owner.CloseSubscription(
                _subscriberKey,
                _leaseId,
                _active);
        }

        return ValueTask.CompletedTask;
    }
}

public sealed class CommunicationCallbackHub
{
    internal sealed class ActiveSubscription : IDisposable
    {
        private int _terminationReason;

        public required Guid LeaseId { get; init; }

        public required Channel<WireV1.CommunicationCallbackEnvelope> Channel
        {
            get;
            init;
        }

        public CancellationTokenSource Termination { get; } = new();

        public CallbackSubscriptionTerminationReason TerminationReason =>
            (CallbackSubscriptionTerminationReason)Volatile.Read(
                ref _terminationReason);

        public void Terminate(CallbackSubscriptionTerminationReason reason)
        {
            Interlocked.CompareExchange(
                ref _terminationReason,
                (int)reason,
                (int)CallbackSubscriptionTerminationReason.None);
            Channel.Writer.TryComplete();
            try
            {
                Termination.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The stream already completed and released the lease.
            }
        }

        public void Dispose()
        {
            Channel.Writer.TryComplete();
            Termination.Dispose();
        }
    }

    private sealed class SubscriberState
    {
        public required string Key { get; init; }

        public required WireV1.ClusterNodeRole Role { get; init; }

        public required string CallerInstanceId { get; init; }

        public Guid? WorldId { get; init; }

        public int ChannelId { get; init; }

        public string WorldGroup { get; init; }

        public required HashSet<WireV1.CommunicationCallbackKind> AcceptedKinds
        {
            get;
            init;
        }

        public LinkedList<WireV1.CommunicationCallbackEnvelope> RetainedEvents
        {
            get;
        } = new();

        public ulong HighestCapacityEvictedSequence { get; set; }

        public ActiveSubscription Active { get; set; }

        public DateTimeOffset LastSeen { get; set; }
    }

    private sealed class WorldRoute
    {
        public required Guid WorldId { get; init; }

        public required int ChannelId { get; init; }

        public required string WorldGroup { get; init; }
    }

    private sealed class CharacterRoute
    {
        public required Guid WorldId { get; init; }

        public required long AccountId { get; init; }

        public required int SessionId { get; init; }

        public required long CharacterId { get; init; }

        public DateTimeOffset LastPulse { get; set; }
    }

    private sealed class PublishedEventRecord
    {
        public required string Fingerprint { get; init; }

        public required ulong Sequence { get; init; }

        public required uint MatchedSubscribers { get; init; }

        public required DateTimeOffset ExpiresAt { get; init; }
    }

    private const int MaximumPublishedEventIds =
        CommunicationCallbackContractLimits.MaxRetainedEventsPerSubscriber * 4;

    private readonly Dictionary<long, CharacterRoute> _characters = new();
    private readonly CommunicationRuntimeOptions _options;
    private readonly Dictionary<string, PublishedEventRecord> _publishedEvents =
        new(StringComparer.Ordinal);
    private readonly Queue<string> _publishedEventOrder = new();
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, SubscriberState> _subscribers =
        new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<Guid, WorldRoute> _worlds = new();
    private long _sequence;

    public CommunicationCallbackHub(
        CommunicationRuntimeOptions options,
        TimeProvider timeProvider)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public int SubscriberStateCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _subscribers.Count;
            }
        }
    }

    public ulong CurrentSequence => checked((ulong)Math.Max(
        0,
        Interlocked.Read(ref _sequence)));

    public WireV1.CommunicationResultCode RegisterWorld(
        Guid worldId,
        int channelId,
        string worldGroup)
    {
        if (worldId == Guid.Empty ||
            channelId <= 0 ||
            string.IsNullOrWhiteSpace(worldGroup))
        {
            return WireV1.CommunicationResultCode.InvalidRequest;
        }

        lock (_syncRoot)
        {
            if (_worlds.TryGetValue(worldId, out WorldRoute existing))
            {
                return existing.ChannelId == channelId &&
                       string.Equals(
                           existing.WorldGroup,
                           worldGroup,
                           StringComparison.Ordinal)
                    ? WireV1.CommunicationResultCode.Success
                    : WireV1.CommunicationResultCode.Conflict;
            }

            _worlds.Add(
                worldId,
                new WorldRoute
                {
                    WorldId = worldId,
                    ChannelId = channelId,
                    WorldGroup = worldGroup
                });
            return WireV1.CommunicationResultCode.Success;
        }
    }

    public void UnregisterWorld(Guid worldId)
    {
        lock (_syncRoot)
        {
            _worlds.Remove(worldId);
            foreach (long characterId in _characters
                         .Where(pair => pair.Value.WorldId == worldId)
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                _characters.Remove(characterId);
            }

            foreach (SubscriberState subscriber in _subscribers.Values
                         .Where(candidate => candidate.WorldId == worldId)
                         .ToArray())
            {
                subscriber.Active?.Terminate(
                    CallbackSubscriptionTerminationReason.WorldUnregistered);
                _subscribers.Remove(subscriber.Key);
            }
        }
    }

    public void BindCharacter(
        Guid worldId,
        long accountId,
        int sessionId,
        long characterId)
    {
        lock (_syncRoot)
        {
            if (!_worlds.ContainsKey(worldId))
            {
                return;
            }

            _characters[characterId] = new CharacterRoute
            {
                WorldId = worldId,
                AccountId = accountId,
                SessionId = sessionId,
                CharacterId = characterId,
                LastPulse = _timeProvider.GetUtcNow()
            };
        }
    }

    public void PulseAccount(long accountId, int sessionId)
    {
        lock (_syncRoot)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            foreach (CharacterRoute route in _characters.Values)
            {
                if (route.AccountId == accountId && route.SessionId == sessionId)
                {
                    route.LastPulse = now;
                }
            }
        }
    }

    public void UnbindCharacter(
        Guid worldId,
        long accountId,
        int sessionId,
        long characterId)
    {
        lock (_syncRoot)
        {
            if (_characters.TryGetValue(
                    characterId,
                    out CharacterRoute route) &&
                route.WorldId == worldId &&
                route.AccountId == accountId &&
                route.SessionId == sessionId)
            {
                _characters.Remove(characterId);
            }
        }
    }

    public void DisconnectAccount(long accountId, int sessionId)
    {
        lock (_syncRoot)
        {
            foreach (long characterId in _characters
                         .Where(pair =>
                             pair.Value.AccountId == accountId &&
                             (sessionId <= 0 ||
                              pair.Value.SessionId == sessionId))
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                _characters.Remove(characterId);
            }
        }
    }

    public CallbackSubscriptionOpenResult TryOpenSubscription(
        WireV1.SubscribeCommunicationCallbacksRequest request,
        out CommunicationCallbackSubscription subscription)
    {
        subscription = null;
        DateTimeOffset now = _timeProvider.GetUtcNow();
        WireV1.ClusterNodeRole role = request.Context.CallerRole;
        Guid? worldId = role == WireV1.ClusterNodeRole.World
            ? Guid.ParseExact(request.WorldId, "D")
            : null;
        HashSet<WireV1.CommunicationCallbackKind> acceptedKinds =
            NormalizeAcceptedKinds(role, request.AcceptedKinds);
        string key = CreateSubscriberKey(
            role,
            request.Context.CallerInstanceId);

        lock (_syncRoot)
        {
            PurgeExpiredState(now);
            if (worldId.HasValue &&
                !IsWorldIdentityRegistered(
                    worldId.Value,
                    request.ChannelId,
                    request.WorldGroup))
            {
                return CallbackSubscriptionOpenResult.NotFound;
            }

            bool isNew = !_subscribers.TryGetValue(
                key,
                out SubscriberState state);
            if (isNew)
            {
                if (request.ResumeAfterSequence != 0)
                {
                    return CallbackSubscriptionOpenResult.InvalidResumeCursor;
                }
                if (!MakeSubscriberCapacity())
                {
                    return CallbackSubscriptionOpenResult.CapacityExceeded;
                }

                state = new SubscriberState
                {
                    Key = key,
                    Role = role,
                    CallerInstanceId = request.Context.CallerInstanceId,
                    WorldId = worldId,
                    ChannelId = request.ChannelId,
                    WorldGroup = request.WorldGroup ?? string.Empty,
                    AcceptedKinds = acceptedKinds,
                    LastSeen = now
                };
                _subscribers.Add(key, state);
            }
            else if (!MatchesSubscriberDefinition(
                         state,
                         worldId,
                         request.ChannelId,
                         request.WorldGroup,
                         acceptedKinds) ||
                     state.Active != null)
            {
                return CallbackSubscriptionOpenResult.Conflict;
            }

            ulong currentSequence = CurrentSequence;
            if (request.ResumeAfterSequence > currentSequence ||
                (state.HighestCapacityEvictedSequence > 0 &&
                 request.ResumeAfterSequence <
                 state.HighestCapacityEvictedSequence))
            {
                if (isNew)
                {
                    _subscribers.Remove(key);
                }
                return CallbackSubscriptionOpenResult.InvalidResumeCursor;
            }

            var replay = state.RetainedEvents
                .Where(envelope =>
                    envelope.Sequence > request.ResumeAfterSequence &&
                    envelope.ExpiresAtUnixTimeMs > now.ToUnixTimeMilliseconds())
                .Select(envelope => envelope.Clone())
                .ToArray();
            var channel = Channel.CreateBounded<
                WireV1.CommunicationCallbackEnvelope>(
                new BoundedChannelOptions(
                    CommunicationCallbackContractLimits
                        .MaxPendingEventsPerSubscriber)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                });
            var active = new ActiveSubscription
            {
                LeaseId = Guid.NewGuid(),
                Channel = channel
            };
            state.Active = active;
            state.LastSeen = now;
            subscription = new CommunicationCallbackSubscription(
                this,
                key,
                active.LeaseId,
                active,
                replay);
            return CallbackSubscriptionOpenResult.Success;
        }
    }

    public CommunicationCallbackPublishResult Publish(
        WireV1.PublishCommunicationCallbackRequest request)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        DateTimeOffset expiresAt = now.AddSeconds(request.TtlSeconds);
        string fingerprint = CreatePublishFingerprint(request);

        lock (_syncRoot)
        {
            PurgeExpiredState(now);
            if (_publishedEvents.TryGetValue(
                    request.EventId,
                    out PublishedEventRecord existing))
            {
                return new CommunicationCallbackPublishResult
                {
                    Result = string.Equals(
                        existing.Fingerprint,
                        fingerprint,
                        StringComparison.Ordinal)
                        ? WireV1.CommunicationResultCode.Success
                        : WireV1.CommunicationResultCode.Conflict,
                    Sequence = existing.Sequence,
                    MatchedSubscribers = existing.MatchedSubscribers
                };
            }

            Guid? characterWorldId = null;
            if (request.Target.Kind ==
                WireV1.CommunicationCallbackTargetKind.WorldId &&
                (!_worlds.ContainsKey(
                    Guid.ParseExact(request.Target.WorldId, "D"))))
            {
                return NotFoundPublishResult();
            }
            if (request.Target.Kind ==
                WireV1.CommunicationCallbackTargetKind.CharacterId)
            {
                if (!_characters.TryGetValue(
                        request.Target.CharacterId,
                        out CharacterRoute characterRoute))
                {
                    return NotFoundPublishResult();
                }
                characterWorldId = characterRoute.WorldId;
            }

            long next = Interlocked.Increment(ref _sequence);
            if (next <= 0)
            {
                throw new InvalidOperationException(
                    "The communication callback sequence was exhausted.");
            }
            ulong sequence = checked((ulong)next);
            WireV1.CommunicationCallbackEnvelope envelope = CreateEnvelope(
                request,
                sequence,
                now,
                expiresAt);
            WireV1.CommunicationCallbackKind callbackKind =
                ResolveCallbackKind(request.CallbackCase);

            uint matched = 0;
            foreach (SubscriberState subscriber in _subscribers.Values)
            {
                if (!subscriber.AcceptedKinds.Contains(callbackKind) ||
                    !MatchesTarget(
                        subscriber,
                        request.Target,
                        characterWorldId))
                {
                    continue;
                }

                matched++;
                RetainEvent(subscriber, envelope, now);
                ActiveSubscription active = subscriber.Active;
                if (active != null &&
                    !active.Channel.Writer.TryWrite(envelope.Clone()))
                {
                    active.Terminate(
                        CallbackSubscriptionTerminationReason.QueueOverflow);
                }
            }

            _publishedEvents.Add(
                request.EventId,
                new PublishedEventRecord
                {
                    Fingerprint = fingerprint,
                    Sequence = sequence,
                    MatchedSubscribers = matched,
                    ExpiresAt = expiresAt
                });
            _publishedEventOrder.Enqueue(request.EventId);
            TrimPublishedEventIds();
            return new CommunicationCallbackPublishResult
            {
                Result = WireV1.CommunicationResultCode.Success,
                Sequence = sequence,
                MatchedSubscribers = matched
            };
        }
    }

    internal void CloseSubscription(
        string subscriberKey,
        Guid leaseId,
        ActiveSubscription active)
    {
        lock (_syncRoot)
        {
            if (_subscribers.TryGetValue(
                    subscriberKey,
                    out SubscriberState state) &&
                state.Active?.LeaseId == leaseId)
            {
                state.Active = null;
                state.LastSeen = _timeProvider.GetUtcNow();
            }
        }

        active.Dispose();
    }

    private static WireV1.CommunicationCallbackEnvelope CreateEnvelope(
        WireV1.PublishCommunicationCallbackRequest request,
        ulong sequence,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        var envelope = new WireV1.CommunicationCallbackEnvelope
        {
            EventId = request.EventId,
            Sequence = sequence,
            IssuedAtUnixTimeMs = issuedAt.ToUnixTimeMilliseconds(),
            ExpiresAtUnixTimeMs = expiresAt.ToUnixTimeMilliseconds(),
            Target = request.Target.Clone()
        };
        switch (request.CallbackCase)
        {
            case WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .CharacterPresence:
                envelope.CharacterPresence = request.CharacterPresence.Clone();
                break;
            case WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .KickSession:
                envelope.KickSession = request.KickSession.Clone();
                break;
            case WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .Lifecycle:
                envelope.Lifecycle = request.Lifecycle.Clone();
                break;
            case WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .GlobalEvent:
                envelope.GlobalEvent = request.GlobalEvent.Clone();
                break;
            case WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .BazaarRefresh:
                envelope.BazaarRefresh = request.BazaarRefresh.Clone();
                break;
            case WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .FamilyRefresh:
                envelope.FamilyRefresh = request.FamilyRefresh.Clone();
                break;
            case WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .PenaltyRefresh:
                envelope.PenaltyRefresh = request.PenaltyRefresh.Clone();
                break;
            case WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .RelationRefresh:
                envelope.RelationRefresh = request.RelationRefresh.Clone();
                break;
            case WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .StaticBonusRefresh:
                envelope.StaticBonusRefresh =
                    request.StaticBonusRefresh.Clone();
                break;
            default:
                throw new InvalidOperationException(
                    "A validated callback request has no payload.");
        }

        return envelope;
    }

    private static string CreatePublishFingerprint(
        WireV1.PublishCommunicationCallbackRequest request)
    {
        WireV1.PublishCommunicationCallbackRequest clone = request.Clone();
        clone.Context = null;
        clone.EventId = string.Empty;
        byte[] payload = clone.ToByteArray();
        byte[] hash = SHA256.HashData(payload);
        return Convert.ToHexString(hash);
    }

    private static string CreateSubscriberKey(
        WireV1.ClusterNodeRole role,
        string callerInstanceId)
    {
        return ((int)role).ToString(
                   System.Globalization.CultureInfo.InvariantCulture) +
               ":" +
               callerInstanceId;
    }

    private static HashSet<WireV1.CommunicationCallbackKind>
        NormalizeAcceptedKinds(
            WireV1.ClusterNodeRole role,
            IEnumerable<WireV1.CommunicationCallbackKind> requested)
    {
        WireV1.CommunicationCallbackKind[] explicitKinds =
            requested?.ToArray() ?? Array.Empty<
                WireV1.CommunicationCallbackKind>();
        if (explicitKinds.Length > 0)
        {
            return explicitKinds.ToHashSet();
        }

        if (role == WireV1.ClusterNodeRole.Login)
        {
            return new HashSet<WireV1.CommunicationCallbackKind>
            {
                WireV1.CommunicationCallbackKind.PenaltyRefresh
            };
        }

        return Enum.GetValues<WireV1.CommunicationCallbackKind>()
            .Where(kind =>
                kind != WireV1.CommunicationCallbackKind.Unspecified)
            .ToHashSet();
    }

    private static WireV1.CommunicationCallbackKind ResolveCallbackKind(
        WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase callback)
    {
        return callback switch
        {
            WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .CharacterPresence =>
                WireV1.CommunicationCallbackKind.CharacterPresence,
            WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .KickSession => WireV1.CommunicationCallbackKind.KickSession,
            WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .Lifecycle => WireV1.CommunicationCallbackKind.Lifecycle,
            WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .GlobalEvent => WireV1.CommunicationCallbackKind.GlobalEvent,
            WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .BazaarRefresh =>
                WireV1.CommunicationCallbackKind.BazaarRefresh,
            WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .FamilyRefresh =>
                WireV1.CommunicationCallbackKind.FamilyRefresh,
            WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .PenaltyRefresh =>
                WireV1.CommunicationCallbackKind.PenaltyRefresh,
            WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .RelationRefresh =>
                WireV1.CommunicationCallbackKind.RelationRefresh,
            WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase
                .StaticBonusRefresh =>
                WireV1.CommunicationCallbackKind.StaticBonusRefresh,
            _ => WireV1.CommunicationCallbackKind.Unspecified
        };
    }

    private bool IsWorldIdentityRegistered(
        Guid worldId,
        int channelId,
        string worldGroup)
    {
        return _worlds.TryGetValue(worldId, out WorldRoute world) &&
               world.ChannelId == channelId &&
               string.Equals(
                   world.WorldGroup,
                   worldGroup,
                   StringComparison.Ordinal);
    }

    private bool MatchesSubscriberDefinition(
        SubscriberState state,
        Guid? worldId,
        int channelId,
        string worldGroup,
        HashSet<WireV1.CommunicationCallbackKind> acceptedKinds)
    {
        return state.WorldId == worldId &&
               state.ChannelId == channelId &&
               string.Equals(
                   state.WorldGroup,
                   worldGroup ?? string.Empty,
                   StringComparison.Ordinal) &&
               state.AcceptedKinds.SetEquals(acceptedKinds);
    }

    private bool MatchesTarget(
        SubscriberState subscriber,
        WireV1.CommunicationCallbackTarget target,
        Guid? characterWorldId)
    {
        bool validWorld = subscriber.Role == WireV1.ClusterNodeRole.World &&
                          subscriber.WorldId.HasValue &&
                          IsWorldIdentityRegistered(
                              subscriber.WorldId.Value,
                              subscriber.ChannelId,
                              subscriber.WorldGroup);
        return target.Kind switch
        {
            WireV1.CommunicationCallbackTargetKind.AllWorlds => validWorld,
            WireV1.CommunicationCallbackTargetKind.WorldGroup =>
                validWorld &&
                string.Equals(
                    subscriber.WorldGroup,
                    target.WorldGroup,
                    StringComparison.Ordinal),
            WireV1.CommunicationCallbackTargetKind.WorldId =>
                validWorld &&
                subscriber.WorldId == Guid.ParseExact(target.WorldId, "D"),
            WireV1.CommunicationCallbackTargetKind.AllLoginNodes =>
                subscriber.Role == WireV1.ClusterNodeRole.Login,
            WireV1.CommunicationCallbackTargetKind.AllNodes =>
                subscriber.Role == WireV1.ClusterNodeRole.Login || validWorld,
            WireV1.CommunicationCallbackTargetKind.CharacterId =>
                validWorld &&
                characterWorldId.HasValue &&
                subscriber.WorldId == characterWorldId,
            _ => false
        };
    }

    private void RetainEvent(
        SubscriberState subscriber,
        WireV1.CommunicationCallbackEnvelope envelope,
        DateTimeOffset now)
    {
        PurgeExpiredEvents(subscriber, now);
        while (subscriber.RetainedEvents.Count >=
               CommunicationCallbackContractLimits
                   .MaxRetainedEventsPerSubscriber)
        {
            LinkedListNode<WireV1.CommunicationCallbackEnvelope> first =
                subscriber.RetainedEvents.First;
            subscriber.HighestCapacityEvictedSequence = Math.Max(
                subscriber.HighestCapacityEvictedSequence,
                first.Value.Sequence);
            subscriber.RetainedEvents.RemoveFirst();
        }

        subscriber.RetainedEvents.AddLast(envelope.Clone());
    }

    private void PurgeExpiredState(DateTimeOffset now)
    {
        foreach (SubscriberState subscriber in _subscribers.Values)
        {
            PurgeExpiredEvents(subscriber, now);
        }

        DateTimeOffset routeThreshold =
            now.AddSeconds(-_options.SessionTtlSeconds);
        foreach (long characterId in _characters
                     .Where(pair => pair.Value.LastPulse <= routeThreshold)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _characters.Remove(characterId);
        }

        foreach (string eventId in _publishedEvents
                     .Where(pair => pair.Value.ExpiresAt <= now)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _publishedEvents.Remove(eventId);
        }
    }

    private static void PurgeExpiredEvents(
        SubscriberState subscriber,
        DateTimeOffset now)
    {
        long nowMilliseconds = now.ToUnixTimeMilliseconds();
        LinkedListNode<WireV1.CommunicationCallbackEnvelope> node =
            subscriber.RetainedEvents.First;
        while (node != null)
        {
            LinkedListNode<WireV1.CommunicationCallbackEnvelope> next =
                node.Next;
            if (node.Value.ExpiresAtUnixTimeMs <= nowMilliseconds)
            {
                subscriber.RetainedEvents.Remove(node);
            }
            node = next;
        }
    }

    private bool MakeSubscriberCapacity()
    {
        if (_subscribers.Count < _options.MaximumCallbackSubscribers)
        {
            return true;
        }

        SubscriberState candidate = _subscribers.Values
            .Where(subscriber => subscriber.Active == null)
            .OrderBy(subscriber => subscriber.LastSeen)
            .FirstOrDefault();
        if (candidate == null)
        {
            return false;
        }

        _subscribers.Remove(candidate.Key);
        return true;
    }

    private void TrimPublishedEventIds()
    {
        while (_publishedEvents.Count > MaximumPublishedEventIds &&
               _publishedEventOrder.Count > 0)
        {
            _publishedEvents.Remove(_publishedEventOrder.Dequeue());
        }
        while (_publishedEventOrder.Count > MaximumPublishedEventIds * 2)
        {
            _publishedEventOrder.Dequeue();
        }
    }

    private static CommunicationCallbackPublishResult NotFoundPublishResult()
    {
        return new CommunicationCallbackPublishResult
        {
            Result = WireV1.CommunicationResultCode.NotFound
        };
    }
}
