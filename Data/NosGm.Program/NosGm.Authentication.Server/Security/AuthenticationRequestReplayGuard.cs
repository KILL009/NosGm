using System.Collections.Concurrent;
using NosGm.Cluster.Contracts.V1;

namespace NosGm.Authentication.Server.Security;

public sealed class AuthenticationRequestReplayGuard
{
    private readonly ConcurrentDictionary<string, long> _requestDeadlines =
        new(StringComparer.Ordinal);
    private readonly int _maximumEntries;
    private int _entryCount;

    public AuthenticationRequestReplayGuard()
    {
        _maximumEntries = AuthenticationServerOptions.MaximumReplayEntries;
    }

    public int Count => _requestDeadlines.Count;

    public bool TryAccept(
        string requestId,
        long deadlineUnixTimeMilliseconds,
        long nowUnixTimeMilliseconds)
    {
        RemoveExpired(nowUnixTimeMilliseconds);
        if (_requestDeadlines.ContainsKey(requestId))
        {
            return false;
        }

        int reservedCount = Interlocked.Increment(ref _entryCount);
        if (reservedCount > _maximumEntries)
        {
            Interlocked.Decrement(ref _entryCount);
            return false;
        }

        long retainedUntil =
            deadlineUnixTimeMilliseconds +
            ClusterProtocolLimits.MaxClockSkewMilliseconds;
        if (_requestDeadlines.TryAdd(requestId, retainedUntil))
        {
            return true;
        }

        Interlocked.Decrement(ref _entryCount);
        return false;
    }

    private void RemoveExpired(long nowUnixTimeMilliseconds)
    {
        foreach (KeyValuePair<string, long> entry in _requestDeadlines)
        {
            if (entry.Value <= nowUnixTimeMilliseconds)
            {
                if (_requestDeadlines.TryRemove(entry.Key, out _))
                {
                    Interlocked.Decrement(ref _entryCount);
                }
            }
        }
    }
}
