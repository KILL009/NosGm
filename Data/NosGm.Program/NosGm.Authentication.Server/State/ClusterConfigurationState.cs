using System.Threading.Channels;
using NosGm.Cluster.Contracts.Configuration.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Server.State;

public enum ConfigurationSubscriptionOpenResult
{
    Success = 0,
    Unavailable = 1,
    InvalidResumeCursor = 2,
    CapacityExceeded = 3,
    RuntimeChanged = 4
}

public enum ConfigurationSubscriptionTerminationReason
{
    None = 0,
    QueueOverflow = 1,
    Superseded = 2,
    RuntimeRestarted = 3
}

public sealed class ClusterConfigurationSubscription : IAsyncDisposable
{
    private readonly ClusterConfigurationState _owner;
    private readonly string _subscriberKey;
    private readonly Guid _leaseId;
    private readonly ClusterConfigurationState.ActiveSubscription _active;
    private int _disposed;

    internal ClusterConfigurationSubscription(
        ClusterConfigurationState owner,
        string subscriberKey,
        Guid leaseId,
        ClusterConfigurationState.ActiveSubscription active,
        IReadOnlyList<ClusterConfigurationState.SnapshotState> replayUpdates)
    {
        _owner = owner;
        _subscriberKey = subscriberKey;
        _leaseId = leaseId;
        _active = active;
        ReplayUpdates = replayUpdates;
    }

    public IReadOnlyList<ClusterConfigurationState.SnapshotState> ReplayUpdates
    {
        get;
    }

    public ChannelReader<ClusterConfigurationState.SnapshotState> PendingUpdates =>
        _active.Channel.Reader;

    public CancellationToken TerminationToken => _active.Termination.Token;

    public ConfigurationSubscriptionTerminationReason TerminationReason =>
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

public sealed class ClusterConfigurationState
{
    internal sealed class ActiveSubscription : IDisposable
    {
        private int _terminationReason;

        public required Guid LeaseId { get; init; }

        public required Channel<SnapshotState> Channel { get; init; }

        public CancellationTokenSource Termination { get; } = new();

        public ConfigurationSubscriptionTerminationReason TerminationReason =>
            (ConfigurationSubscriptionTerminationReason)Volatile.Read(
                ref _terminationReason);

        public void Terminate(
            ConfigurationSubscriptionTerminationReason reason)
        {
            Interlocked.CompareExchange(
                ref _terminationReason,
                (int)reason,
                (int)ConfigurationSubscriptionTerminationReason.None);
            Channel.Writer.TryComplete();
            try
            {
                Termination.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The stream already released this lease.
            }
        }

        public void Dispose()
        {
            Channel.Writer.TryComplete();
            Termination.Dispose();
        }
    }

    private readonly LinkedList<SnapshotState> _history = new();
    private readonly Dictionary<string, ActiveSubscription> _subscriptions =
        new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private ulong _generation;
    private bool _retired;
    private WireV1.ConfigurationSnapshot _snapshot;

    public int SubscriptionCount
    {
        get
        {
            lock (_sync)
            {
                return _subscriptions.Count;
            }
        }
    }

    public bool TryGet(out SnapshotState state)
    {
        lock (_sync)
        {
            if (_retired || _snapshot == null)
            {
                state = default;
                return false;
            }

            state = NewState(_snapshot, _generation);
            return true;
        }
    }

    public SnapshotState Update(WireV1.ConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_sync)
        {
            if (_retired)
            {
                throw new InvalidOperationException(
                    "The Configuration runtime state is retired.");
            }
            if (_snapshot != null && AreEqual(_snapshot, snapshot))
            {
                return NewState(_snapshot, _generation);
            }

            _snapshot = Clone(snapshot);
            checked
            {
                _generation++;
            }

            SnapshotState accepted = NewState(_snapshot, _generation);
            _history.AddLast(Clone(accepted));
            while (_history.Count >
                   ConfigurationContractLimits.MaxRetainedUpdates)
            {
                _history.RemoveFirst();
            }

            foreach (KeyValuePair<string, ActiveSubscription> pair in
                     _subscriptions.ToArray())
            {
                if (pair.Value.Channel.Writer.TryWrite(Clone(accepted)))
                {
                    continue;
                }

                _subscriptions.Remove(pair.Key);
                pair.Value.Terminate(
                    ConfigurationSubscriptionTerminationReason.QueueOverflow);
            }

            return Clone(accepted);
        }
    }

    public ConfigurationSubscriptionOpenResult TryOpenSubscription(
        string subscriberKey,
        ulong resumeAfterGeneration,
        out ClusterConfigurationSubscription subscription)
    {
        subscription = null;
        if (string.IsNullOrWhiteSpace(subscriberKey))
        {
            return ConfigurationSubscriptionOpenResult.InvalidResumeCursor;
        }

        lock (_sync)
        {
            if (_retired)
            {
                return ConfigurationSubscriptionOpenResult.RuntimeChanged;
            }
            if (_snapshot == null)
            {
                return ConfigurationSubscriptionOpenResult.Unavailable;
            }
            if (resumeAfterGeneration > _generation)
            {
                return ConfigurationSubscriptionOpenResult.InvalidResumeCursor;
            }

            ulong earliestRetainedGeneration =
                _history.First?.Value.Generation ?? _generation;
            if (resumeAfterGeneration < earliestRetainedGeneration &&
                earliestRetainedGeneration - resumeAfterGeneration > 1)
            {
                return ConfigurationSubscriptionOpenResult.InvalidResumeCursor;
            }

            if (_subscriptions.TryGetValue(
                    subscriberKey,
                    out ActiveSubscription existing))
            {
                _subscriptions.Remove(subscriberKey);
                existing.Terminate(
                    ConfigurationSubscriptionTerminationReason.Superseded);
            }
            else if (_subscriptions.Count >=
                     ConfigurationContractLimits.MaxConcurrentSubscribers)
            {
                return ConfigurationSubscriptionOpenResult.CapacityExceeded;
            }

            Guid leaseId = Guid.NewGuid();
            var active = new ActiveSubscription
            {
                LeaseId = leaseId,
                Channel = Channel.CreateBounded<SnapshotState>(
                    new BoundedChannelOptions(
                        ConfigurationContractLimits
                            .MaxPendingUpdatesPerSubscriber)
                    {
                        SingleReader = true,
                        SingleWriter = false,
                        FullMode = BoundedChannelFullMode.Wait
                    })
            };
            SnapshotState[] replay = _history
                .Where(state => state.Generation > resumeAfterGeneration)
                .Select(Clone)
                .ToArray();
            _subscriptions.Add(subscriberKey, active);
            subscription = new ClusterConfigurationSubscription(
                this,
                subscriberKey,
                leaseId,
                active,
                replay);
            return ConfigurationSubscriptionOpenResult.Success;
        }
    }

    internal void CloseSubscription(
        string subscriberKey,
        Guid leaseId,
        ActiveSubscription active)
    {
        lock (_sync)
        {
            if (_subscriptions.TryGetValue(
                    subscriberKey,
                    out ActiveSubscription current) &&
                current.LeaseId == leaseId &&
                ReferenceEquals(current, active))
            {
                _subscriptions.Remove(subscriberKey);
            }
        }

        active.Dispose();
    }

    internal void RetireForRuntimeRestart()
    {
        ActiveSubscription[] subscriptions;
        lock (_sync)
        {
            if (_retired)
            {
                return;
            }
            _retired = true;
            subscriptions = _subscriptions.Values.ToArray();
            _subscriptions.Clear();
        }

        foreach (ActiveSubscription subscription in subscriptions)
        {
            subscription.Terminate(
                ConfigurationSubscriptionTerminationReason
                    .RuntimeRestarted);
        }
    }

    private static SnapshotState NewState(
        WireV1.ConfigurationSnapshot snapshot,
        ulong generation)
    {
        return new SnapshotState(Clone(snapshot), generation);
    }

    private static SnapshotState Clone(SnapshotState state)
    {
        return NewState(state.Configuration, state.Generation);
    }

    private static bool AreEqual(
        WireV1.ConfigurationSnapshot left,
        WireV1.ConfigurationSnapshot right)
    {
        return left.MaxGold == right.MaxGold &&
               left.TimeExpBuffUnixTimeMs == right.TimeExpBuffUnixTimeMs &&
               left.TimeGoldBuffUnixTimeMs == right.TimeGoldBuffUnixTimeMs;
    }

    private static WireV1.ConfigurationSnapshot Clone(
        WireV1.ConfigurationSnapshot snapshot)
    {
        return new WireV1.ConfigurationSnapshot
        {
            MaxGold = snapshot.MaxGold,
            TimeExpBuffUnixTimeMs = snapshot.TimeExpBuffUnixTimeMs,
            TimeGoldBuffUnixTimeMs = snapshot.TimeGoldBuffUnixTimeMs
        };
    }

    public readonly record struct SnapshotState(
        WireV1.ConfigurationSnapshot Configuration,
        ulong Generation);
}
