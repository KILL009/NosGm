using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace NosGm.Core.Diagnostics
{
    public sealed class GuriPerformanceSnapshot
    {
        public long Type { get; internal set; }

        public long Count { get; internal set; }

        public long Errors { get; internal set; }

        public long MissingHandlers { get; internal set; }

        public double TotalMilliseconds { get; internal set; }

        public double AverageMilliseconds { get; internal set; }

        public double MaximumMilliseconds { get; internal set; }
    }

    /// <summary>
    /// Low-overhead diagnostics for the multiplexed guri packet. One guri header
    /// represents many unrelated game actions, so the normal packet metrics cannot
    /// identify which concrete type is slow.
    /// </summary>
    public static class GuriPerformanceMonitor
    {
        private sealed class Counter
        {
            private long _count;
            private long _errors;
            private long _missingHandlers;
            private long _elapsedTicks;
            private long _maximumTicks;

            public void Record(long elapsedTicks, bool succeeded)
            {
                Interlocked.Increment(ref _count);
                Interlocked.Add(ref _elapsedTicks, Math.Max(0, elapsedTicks));
                UpdateMaximum(ref _maximumTicks, Math.Max(0, elapsedTicks));
                if (!succeeded)
                {
                    Interlocked.Increment(ref _errors);
                }
            }

            public void RecordMissingHandler() => Interlocked.Increment(ref _missingHandlers);

            public GuriPerformanceSnapshot Capture(long type)
            {
                long count = Interlocked.Read(ref _count);
                long elapsedTicks = Interlocked.Read(ref _elapsedTicks);
                return new GuriPerformanceSnapshot
                {
                    Type = type,
                    Count = count,
                    Errors = Interlocked.Read(ref _errors),
                    MissingHandlers = Interlocked.Read(ref _missingHandlers),
                    TotalMilliseconds = ToMilliseconds(elapsedTicks),
                    AverageMilliseconds = count <= 0 ? 0 : ToMilliseconds(elapsedTicks) / count,
                    MaximumMilliseconds = ToMilliseconds(Interlocked.Read(ref _maximumTicks))
                };
            }
        }

        private const int MaximumTrackedTypes = 512;
        private static readonly ConcurrentDictionary<long, Counter> Counters =
            new ConcurrentDictionary<long, Counter>();

        public static void Record(long type, long elapsedStopwatchTicks, bool succeeded)
        {
            Counter counter = GetCounter(type);
            counter?.Record(elapsedStopwatchTicks, succeeded);
        }

        public static void RecordMissingHandler(long type)
        {
            Counter counter = GetCounter(type);
            counter?.RecordMissingHandler();
        }

        public static IReadOnlyList<GuriPerformanceSnapshot> GetTop(
            int take = 12,
            HandlerSort sort = HandlerSort.TotalTime)
        {
            take = Math.Max(1, Math.Min(50, take));
            IEnumerable<GuriPerformanceSnapshot> snapshots = Counters
                .Select(pair => pair.Value.Capture(pair.Key));

            switch (sort)
            {
                case HandlerSort.Count:
                    snapshots = snapshots.OrderByDescending(metric => metric.Count);
                    break;
                case HandlerSort.AverageTime:
                    snapshots = snapshots.OrderByDescending(metric => metric.AverageMilliseconds);
                    break;
                case HandlerSort.MaximumTime:
                    snapshots = snapshots.OrderByDescending(metric => metric.MaximumMilliseconds);
                    break;
                case HandlerSort.Errors:
                    snapshots = snapshots.OrderByDescending(metric => metric.Errors + metric.MissingHandlers)
                        .ThenByDescending(metric => metric.Count);
                    break;
                default:
                    snapshots = snapshots.OrderByDescending(metric => metric.TotalMilliseconds);
                    break;
            }

            return snapshots.Take(take).ToList();
        }

        public static void Reset() => Counters.Clear();

        private static Counter GetCounter(long type)
        {
            if (Counters.TryGetValue(type, out Counter existing))
            {
                return existing;
            }

            if (Counters.Count >= MaximumTrackedTypes)
            {
                return null;
            }

            return Counters.GetOrAdd(type, _ => new Counter());
        }

        private static double ToMilliseconds(long ticks) =>
            ticks <= 0 ? 0 : ticks * 1000d / Stopwatch.Frequency;

        private static void UpdateMaximum(ref long target, long value)
        {
            long current;
            while (value > (current = Interlocked.Read(ref target)) &&
                   Interlocked.CompareExchange(ref target, value, current) != current)
            {
            }
        }
    }
}
