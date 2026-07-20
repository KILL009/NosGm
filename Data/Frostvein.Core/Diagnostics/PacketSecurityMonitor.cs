using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Frostvein.Core.Diagnostics
{
    /// <summary>
    /// Global low-overhead telemetry for packets rejected before handler execution.
    /// </summary>
    public sealed class PacketSecurityMonitor
    {
        private static readonly Lazy<PacketSecurityMonitor> LazyInstance =
            new Lazy<PacketSecurityMonitor>(() => new PacketSecurityMonitor());

        private readonly ConcurrentDictionary<string, BlockCounter> _blockedKeys =
            new ConcurrentDictionary<string, BlockCounter>(StringComparer.OrdinalIgnoreCase);

        private long _transportAccepted;
        private long _handlerAccepted;
        private long _transportBlocked;
        private long _handlerBlocked;
        private long _oversizedMessages;
        private long _droppedBytes;
        private long _disconnects;

        private PacketSecurityMonitor()
        {
        }

        public static PacketSecurityMonitor Instance => LazyInstance.Value;

        public void RecordTransportAccepted() => Interlocked.Increment(ref _transportAccepted);

        public void RecordHandlerAccepted() => Interlocked.Increment(ref _handlerAccepted);

        public void RecordBlocked(
            PacketSecurityStage stage,
            string reason,
            string key,
            int byteCount,
            bool disconnected)
        {
            if (stage == PacketSecurityStage.Transport)
            {
                Interlocked.Increment(ref _transportBlocked);
            }
            else
            {
                Interlocked.Increment(ref _handlerBlocked);
            }

            if (string.Equals(reason, PacketSecurityReasons.OversizedMessage, StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _oversizedMessages);
            }

            if (byteCount > 0)
            {
                Interlocked.Add(ref _droppedBytes, byteCount);
            }

            if (disconnected)
            {
                Interlocked.Increment(ref _disconnects);
            }

            string safeKey = string.IsNullOrWhiteSpace(key) ? "<unknown>" : key.Trim();
            string metricKey = $"{stage}:{safeKey}:{reason}";
            if (_blockedKeys.Count < 512 || _blockedKeys.ContainsKey(metricKey))
            {
                _blockedKeys.GetOrAdd(metricKey, _ => new BlockCounter())
                    .Record(disconnected);
            }
        }

        public PacketSecuritySnapshot Capture()
        {
            return new PacketSecuritySnapshot
            {
                TransportAccepted = Interlocked.Read(ref _transportAccepted),
                HandlerAccepted = Interlocked.Read(ref _handlerAccepted),
                TransportBlocked = Interlocked.Read(ref _transportBlocked),
                HandlerBlocked = Interlocked.Read(ref _handlerBlocked),
                OversizedMessages = Interlocked.Read(ref _oversizedMessages),
                DroppedBytes = Interlocked.Read(ref _droppedBytes),
                Disconnects = Interlocked.Read(ref _disconnects)
            };
        }

        public IReadOnlyList<PacketSecurityBlockSnapshot> GetTopBlocked(int take = 12)
        {
            if (take < 1)
            {
                take = 1;
            }
            else if (take > 50)
            {
                take = 50;
            }

            return _blockedKeys
                .Select(entry => entry.Value.Capture(entry.Key))
                .OrderByDescending(entry => entry.Count)
                .ThenByDescending(entry => entry.Disconnects)
                .Take(take)
                .ToList();
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _transportAccepted, 0);
            Interlocked.Exchange(ref _handlerAccepted, 0);
            Interlocked.Exchange(ref _transportBlocked, 0);
            Interlocked.Exchange(ref _handlerBlocked, 0);
            Interlocked.Exchange(ref _oversizedMessages, 0);
            Interlocked.Exchange(ref _droppedBytes, 0);
            Interlocked.Exchange(ref _disconnects, 0);
            _blockedKeys.Clear();
        }

        private sealed class BlockCounter
        {
            private long _count;
            private long _disconnects;

            public void Record(bool disconnected)
            {
                Interlocked.Increment(ref _count);
                if (disconnected)
                {
                    Interlocked.Increment(ref _disconnects);
                }
            }

            public PacketSecurityBlockSnapshot Capture(string key)
            {
                return new PacketSecurityBlockSnapshot
                {
                    Key = key,
                    Count = Interlocked.Read(ref _count),
                    Disconnects = Interlocked.Read(ref _disconnects)
                };
            }
        }
    }

    /// <summary>
    /// Per-network-connection limiter. It rejects oversized encrypted messages and
    /// sustained packet or byte floods before they reach the session receive queue.
    /// </summary>
    public sealed class ConnectionPacketRateGuard
    {
        public const int MaximumMessageBytes = 128 * 1024;

        private const double PacketsPerSecond = 350d;
        private const double PacketBurst = 700d;
        private const double BytesPerSecond = 2d * 1024d * 1024d;
        private const double ByteBurst = 4d * 1024d * 1024d;

        private readonly object _sync = new object();
        private readonly TokenBucket _packetBucket = new TokenBucket(PacketsPerSecond, PacketBurst);
        private readonly TokenBucket _byteBucket = new TokenBucket(BytesPerSecond, ByteBurst);
        private readonly ViolationWindow _violations = new ViolationWindow();
        private long _lastLogTimestamp;

        public PacketRateDecision Check(int byteCount)
        {
            long now = Stopwatch.GetTimestamp();
            PacketRateDecision decision;

            lock (_sync)
            {
                if (byteCount <= 0)
                {
                    decision = Block(PacketSecurityReasons.InvalidMessage, 3d, now, byteCount);
                }
                else if (byteCount > MaximumMessageBytes)
                {
                    decision = Block(PacketSecurityReasons.OversizedMessage, 10d, now, byteCount, true);
                }
                else if (!_packetBucket.TryConsume(1d, now))
                {
                    decision = Block(PacketSecurityReasons.TransportPacketRate, 1.5d, now, byteCount);
                }
                else if (!_byteBucket.TryConsume(byteCount, now))
                {
                    decision = Block(PacketSecurityReasons.TransportByteRate, 2d, now, byteCount);
                }
                else
                {
                    PacketSecurityMonitor.Instance.RecordTransportAccepted();
                    return PacketRateDecision.Allow;
                }
            }

            PacketSecurityMonitor.Instance.RecordBlocked(
                PacketSecurityStage.Transport,
                decision.Reason,
                "connection",
                Math.Max(0, byteCount),
                decision.Disconnect);
            return decision;
        }

        private PacketRateDecision Block(
            string reason,
            double severity,
            long now,
            int byteCount,
            bool forceDisconnect = false)
        {
            bool disconnect = forceDisconnect || _violations.Add(severity, now) >= 10d;
            bool shouldLog = disconnect || HasLogIntervalElapsed(now);
            return PacketRateDecision.Block(reason, disconnect, shouldLog);
        }

        private bool HasLogIntervalElapsed(long now)
        {
            long minimumInterval = Stopwatch.Frequency * 5L;
            if (now - _lastLogTimestamp < minimumInterval)
            {
                return false;
            }

            _lastLogTimestamp = now;
            return true;
        }
    }

    /// <summary>
    /// Per-session, per-handler limiter. Every HandlerMethodReference owns one guard,
    /// so a player cannot evade a limit by mixing the same packet with other traffic.
    /// </summary>
    public sealed class HandlerPacketRateGuard
    {
        private readonly object _sync = new object();
        private readonly string _header;
        private readonly TokenBucket _bucket;
        private readonly ViolationWindow _violations = new ViolationWindow();
        private long _lastLogTimestamp;

        public HandlerPacketRateGuard(string header)
        {
            _header = string.IsNullOrWhiteSpace(header) ? "<unidentified>" : header.Trim();
            HandlerRateProfile profile = HandlerRateProfile.Resolve(_header);
            _bucket = new TokenBucket(profile.RatePerSecond, profile.Burst);
        }

        public PacketRateDecision Check()
        {
            long now = Stopwatch.GetTimestamp();
            PacketRateDecision decision;

            lock (_sync)
            {
                if (_bucket.TryConsume(1d, now))
                {
                    PacketSecurityMonitor.Instance.RecordHandlerAccepted();
                    return PacketRateDecision.Allow;
                }

                bool disconnect = _violations.Add(1d, now) >= 10d;
                bool shouldLog = disconnect || HasLogIntervalElapsed(now);
                decision = PacketRateDecision.Block(
                    PacketSecurityReasons.HandlerRate,
                    disconnect,
                    shouldLog);
            }

            PacketSecurityMonitor.Instance.RecordBlocked(
                PacketSecurityStage.Handler,
                decision.Reason,
                _header,
                0,
                decision.Disconnect);
            return decision;
        }

        private bool HasLogIntervalElapsed(long now)
        {
            long minimumInterval = Stopwatch.Frequency * 5L;
            if (now - _lastLogTimestamp < minimumInterval)
            {
                return false;
            }

            _lastLogTimestamp = now;
            return true;
        }
    }

    public sealed class PacketRateDecision
    {
        private PacketRateDecision(bool allowed, bool disconnect, bool shouldLog, string reason)
        {
            Allowed = allowed;
            Disconnect = disconnect;
            ShouldLog = shouldLog;
            Reason = reason;
        }

        public static PacketRateDecision Allow { get; } =
            new PacketRateDecision(true, false, false, null);

        public bool Allowed { get; }

        public bool Disconnect { get; }

        public bool ShouldLog { get; }

        public string Reason { get; }

        public static PacketRateDecision Block(string reason, bool disconnect, bool shouldLog) =>
            new PacketRateDecision(false, disconnect, shouldLog, reason);
    }

    public enum PacketSecurityStage
    {
        Transport = 0,
        Handler = 1
    }

    public static class PacketSecurityReasons
    {
        public const string InvalidMessage = "invalid-message";
        public const string OversizedMessage = "oversized-message";
        public const string TransportPacketRate = "transport-packet-rate";
        public const string TransportByteRate = "transport-byte-rate";
        public const string HandlerRate = "handler-rate";
    }

    public sealed class PacketSecuritySnapshot
    {
        public long TransportAccepted { get; set; }

        public long HandlerAccepted { get; set; }

        public long TransportBlocked { get; set; }

        public long HandlerBlocked { get; set; }

        public long OversizedMessages { get; set; }

        public long DroppedBytes { get; set; }

        public long Disconnects { get; set; }
    }

    public sealed class PacketSecurityBlockSnapshot
    {
        public string Key { get; set; }

        public long Count { get; set; }

        public long Disconnects { get; set; }
    }

    internal sealed class TokenBucket
    {
        private readonly double _ratePerSecond;
        private readonly double _capacity;
        private double _tokens;
        private long _lastTimestamp;

        public TokenBucket(double ratePerSecond, double capacity)
        {
            _ratePerSecond = Math.Max(1d, ratePerSecond);
            _capacity = Math.Max(1d, capacity);
            _tokens = _capacity;
            _lastTimestamp = Stopwatch.GetTimestamp();
        }

        public bool TryConsume(double amount, long now)
        {
            Refill(now);
            if (_tokens < amount)
            {
                return false;
            }

            _tokens -= amount;
            return true;
        }

        private void Refill(long now)
        {
            long elapsed = now - _lastTimestamp;
            if (elapsed <= 0)
            {
                return;
            }

            _lastTimestamp = now;
            _tokens = Math.Min(
                _capacity,
                _tokens + elapsed / (double)Stopwatch.Frequency * _ratePerSecond);
        }
    }

    internal sealed class ViolationWindow
    {
        private double _score;
        private long _lastTimestamp = Stopwatch.GetTimestamp();

        public double Add(double severity, long now)
        {
            long elapsed = now - _lastTimestamp;
            if (elapsed > 0)
            {
                double elapsedSeconds = elapsed / (double)Stopwatch.Frequency;
                _score = Math.Max(0d, _score - elapsedSeconds / 5d);
            }

            _lastTimestamp = now;
            _score += Math.Max(0d, severity);
            return _score;
        }
    }

    internal sealed class HandlerRateProfile
    {
        private static readonly string[] ChatHeaders =
        {
            "/", ":", ";", "say", "spk", "shout", "whisper", "buddy"
        };

        private static readonly string[] SensitivePrefixes =
        {
            "c_", "rc_", "exc", "exchange", "mvi", "mve", "shop", "buy",
            "sell", "mail", "parcel", "post", "req_", "guri"
        };

        private HandlerRateProfile(double ratePerSecond, double burst)
        {
            RatePerSecond = ratePerSecond;
            Burst = burst;
        }

        public double RatePerSecond { get; }

        public double Burst { get; }

        public static HandlerRateProfile Resolve(string header)
        {
            string normalized = (header ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized.StartsWith("$", StringComparison.Ordinal))
            {
                return new HandlerRateProfile(20d, 40d);
            }

            if (ChatHeaders.Contains(normalized))
            {
                return new HandlerRateProfile(12d, 24d);
            }

            if (SensitivePrefixes.Any(prefix =>
                    normalized.StartsWith(prefix, StringComparison.Ordinal)))
            {
                return new HandlerRateProfile(35d, 70d);
            }

            return new HandlerRateProfile(120d, 240d);
        }
    }
}
