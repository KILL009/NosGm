using System.Diagnostics;
using System.Threading;

namespace NosGm.Domain
{
    public sealed class PacketIngressSnapshot
    {
        public int QueueCapacityPerSession { get; internal set; }
        public long QueueDepth { get; internal set; }
        public long MaximumSessionQueueDepth { get; internal set; }
        public long QueueHighWatermark { get; internal set; }
        public long Enqueued { get; internal set; }
        public long Processed { get; internal set; }
        public long Dropped { get; internal set; }
        public long Overflows { get; internal set; }
        public long Drains { get; internal set; }
        public long Reschedules { get; internal set; }
        public long Cleared { get; internal set; }
        public int ActiveWorkers { get; internal set; }
        public long MaximumActiveWorkers { get; internal set; }
        public double AverageDrainMilliseconds { get; internal set; }
        public double MaximumDrainMilliseconds { get; internal set; }
        public double AveragePacketsPerDrain { get; internal set; }
        public long MaximumPacketsPerDrain { get; internal set; }
    }

    public static class PacketIngressMonitor
    {
        private sealed class CounterState
        {
            internal long QueueDepth;
            internal long MaximumSessionQueueDepth;
            internal long QueueHighWatermark;
            internal long Enqueued;
            internal long Processed;
            internal long Dropped;
            internal long Overflows;
            internal long Drains;
            internal long Reschedules;
            internal long Cleared;
            internal int ActiveWorkers;
            internal long MaximumActiveWorkers;
            internal long DrainTicks;
            internal long MaximumDrainTicks;
            internal long PacketsInDrains;
            internal long MaximumPacketsPerDrain;
        }

        private const int QueueCapacity = 4096;
        private static CounterState _state = new CounterState();

        public static int QueueCapacityPerSession => QueueCapacity;

        public static void RecordEnqueued(int sessionDepth)
        {
            CounterState state = Volatile.Read(ref _state);
            Interlocked.Increment(ref state.Enqueued);
            Interlocked.Increment(ref state.QueueDepth);
            UpdateMaximum(ref state.MaximumSessionQueueDepth, sessionDepth);
            UpdateMaximum(ref state.QueueHighWatermark, Interlocked.Read(ref state.QueueDepth));
        }

        public static void RecordDequeued()
        {
            CounterState state = Volatile.Read(ref _state);
            Interlocked.Increment(ref state.Processed);
            DecrementNonNegative(ref state.QueueDepth);
        }

        public static void RecordDropped(bool overflow)
        {
            CounterState state = Volatile.Read(ref _state);
            Interlocked.Increment(ref state.Dropped);
            if (overflow)
            {
                Interlocked.Increment(ref state.Overflows);
            }
        }

        public static void RecordCleared(int count)
        {
            if (count <= 0)
            {
                return;
            }

            CounterState state = Volatile.Read(ref _state);
            Interlocked.Add(ref state.Cleared, count);
            for (int i = 0; i < count; i++)
            {
                DecrementNonNegative(ref state.QueueDepth);
            }
        }

        public static void RecordRescheduled() =>
            Interlocked.Increment(ref Volatile.Read(ref _state).Reschedules);

        public static void RecordWorkerStarted()
        {
            CounterState state = Volatile.Read(ref _state);
            int active = Interlocked.Increment(ref state.ActiveWorkers);
            UpdateMaximum(ref state.MaximumActiveWorkers, active);
        }

        public static void RecordDrain(long elapsedTicks, int packetCount)
        {
            CounterState state = Volatile.Read(ref _state);
            Interlocked.Decrement(ref state.ActiveWorkers);
            Interlocked.Increment(ref state.Drains);
            Interlocked.Add(ref state.DrainTicks, elapsedTicks);
            Interlocked.Add(ref state.PacketsInDrains, packetCount);
            UpdateMaximum(ref state.MaximumDrainTicks, elapsedTicks);
            UpdateMaximum(ref state.MaximumPacketsPerDrain, packetCount);
        }

        public static PacketIngressSnapshot Capture()
        {
            CounterState state = Volatile.Read(ref _state);
            long drains = Interlocked.Read(ref state.Drains);
            long drainTicks = Interlocked.Read(ref state.DrainTicks);
            long packetsInDrains = Interlocked.Read(ref state.PacketsInDrains);

            return new PacketIngressSnapshot
            {
                QueueCapacityPerSession = QueueCapacity,
                QueueDepth = Interlocked.Read(ref state.QueueDepth),
                MaximumSessionQueueDepth = Interlocked.Read(ref state.MaximumSessionQueueDepth),
                QueueHighWatermark = Interlocked.Read(ref state.QueueHighWatermark),
                Enqueued = Interlocked.Read(ref state.Enqueued),
                Processed = Interlocked.Read(ref state.Processed),
                Dropped = Interlocked.Read(ref state.Dropped),
                Overflows = Interlocked.Read(ref state.Overflows),
                Drains = drains,
                Reschedules = Interlocked.Read(ref state.Reschedules),
                Cleared = Interlocked.Read(ref state.Cleared),
                ActiveWorkers = Volatile.Read(ref state.ActiveWorkers),
                MaximumActiveWorkers = Interlocked.Read(ref state.MaximumActiveWorkers),
                AverageDrainMilliseconds = drains == 0 ? 0 : TicksToMilliseconds(drainTicks) / drains,
                MaximumDrainMilliseconds = TicksToMilliseconds(Interlocked.Read(ref state.MaximumDrainTicks)),
                AveragePacketsPerDrain = drains == 0 ? 0 : packetsInDrains / (double)drains,
                MaximumPacketsPerDrain = Interlocked.Read(ref state.MaximumPacketsPerDrain)
            };
        }

        public static void Reset() => Interlocked.Exchange(ref _state, new CounterState());

        private static double TicksToMilliseconds(long ticks) =>
            ticks * 1000d / Stopwatch.Frequency;

        private static void DecrementNonNegative(ref long value)
        {
            while (true)
            {
                long current = Interlocked.Read(ref value);
                if (current <= 0)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref value, current - 1, current) == current)
                {
                    return;
                }
            }
        }

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
