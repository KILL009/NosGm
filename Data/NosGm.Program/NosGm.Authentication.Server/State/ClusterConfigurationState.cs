using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Server.State;

public sealed class ClusterConfigurationState
{
    private readonly object _sync = new();
    private ulong _generation;
    private WireV1.ConfigurationSnapshot _snapshot;

    public bool TryGet(out SnapshotState state)
    {
        lock (_sync)
        {
            if (_snapshot == null)
            {
                state = default;
                return false;
            }

            state = new SnapshotState(
                Clone(_snapshot),
                _generation);
            return true;
        }
    }

    public SnapshotState Update(WireV1.ConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_sync)
        {
            _snapshot = Clone(snapshot);
            checked
            {
                _generation++;
            }

            return new SnapshotState(
                Clone(_snapshot),
                _generation);
        }
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
