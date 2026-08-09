using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Server.State;

public enum ConfigurationRuntimeRestartResult
{
    Success = 0,
    Disabled = 1,
    Unavailable = 2,
    RuntimeGenerationChanged = 3
}

public readonly record struct ConfigurationRuntimeStatus(
    Guid RuntimeGenerationId,
    DateTimeOffset StartedAt,
    ulong ConfigurationGeneration,
    bool Seeded,
    int ActiveSubscriptions,
    uint RestartCount,
    bool ControlEnabled);

public sealed class ConfigurationRuntimeController
{
    private readonly ConfigurationRuntimeControlOptions _options;
    private readonly object _sync = new();
    private readonly TimeProvider _timeProvider;
    private Guid _runtimeGenerationId;
    private DateTimeOffset _startedAt;
    private ClusterConfigurationState _state;
    private uint _restartCount;

    public ConfigurationRuntimeController(
        ConfigurationRuntimeControlOptions options,
        TimeProvider timeProvider)
    {
        _options = options ??
            throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ??
            throw new ArgumentNullException(nameof(timeProvider));
        _runtimeGenerationId = Guid.NewGuid();
        _startedAt = _timeProvider.GetUtcNow();
        _state = new ClusterConfigurationState();
    }

    public ConfigurationRuntimeStatus GetStatus()
    {
        lock (_sync)
        {
            return GetStatusLocked();
        }
    }

    public bool TryGet(
        out ClusterConfigurationState.SnapshotState state,
        out Guid runtimeGenerationId)
    {
        lock (_sync)
        {
            runtimeGenerationId = _runtimeGenerationId;
            return _state.TryGet(out state);
        }
    }

    public bool TrySeed(
        WireV1.ConfigurationSnapshot snapshot,
        out ClusterConfigurationState.SnapshotState state,
        out Guid runtimeGenerationId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_sync)
        {
            runtimeGenerationId = _runtimeGenerationId;
            if (_state.TryGet(out state))
            {
                return false;
            }

            state = _state.Update(snapshot);
            return true;
        }
    }

    public ClusterConfigurationState.SnapshotState Update(
        WireV1.ConfigurationSnapshot snapshot,
        out Guid runtimeGenerationId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_sync)
        {
            ClusterConfigurationState.SnapshotState accepted =
                _state.Update(snapshot);
            runtimeGenerationId = _runtimeGenerationId;
            return accepted;
        }
    }

    public ConfigurationSubscriptionOpenResult TryOpenSubscription(
        Guid expectedRuntimeGenerationId,
        string subscriberKey,
        ulong resumeAfterGeneration,
        out ClusterConfigurationSubscription subscription,
        out Guid runtimeGenerationId)
    {
        lock (_sync)
        {
            runtimeGenerationId = _runtimeGenerationId;
            if (expectedRuntimeGenerationId != _runtimeGenerationId)
            {
                subscription = null;
                return ConfigurationSubscriptionOpenResult.RuntimeChanged;
            }

            return _state.TryOpenSubscription(
                subscriberKey,
                resumeAfterGeneration,
                out subscription);
        }
    }

    public ConfigurationRuntimeRestartResult TryRestart(
        Guid expectedRuntimeGenerationId,
        out ConfigurationRuntimeStatus status)
    {
        lock (_sync)
        {
            if (!_options.Enabled)
            {
                status = GetStatusLocked();
                return ConfigurationRuntimeRestartResult.Disabled;
            }
            if (expectedRuntimeGenerationId != _runtimeGenerationId)
            {
                status = GetStatusLocked();
                return ConfigurationRuntimeRestartResult
                    .RuntimeGenerationChanged;
            }
            if (!_state.TryGet(
                    out ClusterConfigurationState.SnapshotState current))
            {
                status = GetStatusLocked();
                return ConfigurationRuntimeRestartResult.Unavailable;
            }

            var replacement = new ClusterConfigurationState();
            replacement.Update(current.Configuration);
            ClusterConfigurationState retiredState = _state;
            _state = replacement;
            Guid nextGeneration;
            do
            {
                nextGeneration = Guid.NewGuid();
            }
            while (nextGeneration == _runtimeGenerationId ||
                   nextGeneration == Guid.Empty);
            _runtimeGenerationId = nextGeneration;
            _startedAt = _timeProvider.GetUtcNow();
            checked
            {
                _restartCount++;
            }
            retiredState.RetireForRuntimeRestart();
            status = GetStatusLocked();
        }
        return ConfigurationRuntimeRestartResult.Success;
    }

    private ConfigurationRuntimeStatus GetStatusLocked()
    {
        bool seeded = _state.TryGet(
            out ClusterConfigurationState.SnapshotState current);
        return new ConfigurationRuntimeStatus(
            _runtimeGenerationId,
            _startedAt,
            seeded ? current.Generation : 0,
            seeded,
            _state.SubscriptionCount,
            _restartCount,
            _options.Enabled);
    }
}
