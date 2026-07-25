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
        public long OverflowDisconnects { get; internal set; }
        public long Drains { get; internal set; }
        public long Reschedules { get; internal set; }
        public long Cleared { get; internal set; }
        public long Errors { get; internal set; }
        public int ActiveWorkers { get; internal set; }
        public long MaximumActiveWorkers { get; internal set; }
        public double AverageDrainMilliseconds { get; internal set; }
        public double MaximumDrainMilliseconds { get; internal set; }
        public double AveragePacketsPerDrain { get; internal set; }
        public long MaximumPacketsPerDrain { get; internal set; }
        public double AverageQueueWaitMilliseconds { get; internal set; }
        public double MaximumQueueWaitMilliseconds { get; internal set; }
    }

    public static class PacketIngressMonitor
    {
        private sealed class CounterState
        {
            internal CounterState(long generation, long queueDepth, int activeWorkers)
            {
                Generation = generation;
                QueueHighWatermark = queueDepth;
                MaximumActiveWorkers = activeWorkers;
            }

            internal readonly long Generation;
            internal long MaximumSessionQueueDepth;
            internal long QueueHighWatermark;
            internal long Enqueued;
            internal long Processed;
            internal long Dropped;
            internal long Overflows;
            internal long OverflowDisconnects;
            internal long Drains;
            internal long Reschedules;
            internal long Cleared;
            internal long Errors;
            internal long MaximumActiveWorkers;
            internal long DrainTicks;
            internal long MaximumDrainTicks;
            internal long PacketsInDrains;
            internal long MaximumPacketsPerDrain;
            internal long QueueWaitTicks;
            internal long MaximumQueueWaitTicks;
        }

        private const int QueueCapacity = 4096;
        private static long _generation;
        private static long _queueDepth;
        private static int _activeWorkers;
        private static CounterState _state = new CounterState(0, 0, 0);

        public static int QueueCapacityPerSession => QueueCapacity;

        public static long RecordEnqueued(int sessionDepth)
        {
            CounterState state = Volatile.Read(ref _state);
            long totalDepth = Interlocked.Increment(ref _queueDepth);
            Interlocked.Increment(ref state.Enqueued);
            UpdateMaximum(ref state.MaximumSessionQueueDepth, sessionDepth);
            UpdateMaximum(ref state.QueueHighWatermark, totalDepth);
            return state.Generation;
        }

        public static void RecordDequeued(long generation, long queueWaitTicks)
        {
            DecrementNonNegative(ref _queueDepth);
            CounterState state = Volatile.Read(ref _state);
            if (state.Generation != generation)
            {
                return;
            }

            Interlocked.Increment(ref state.Processed);
            Interlocked.Add(ref state.QueueWaitTicks, queueWaitTicks);
            UpdateMaximum(ref state.MaximumQueueWaitTicks, queueWaitTicks);
        }

        public static void RecordDropped(bool overflow, bool disconnected)
        {
            CounterState state = Volatile.Read(ref _state);
            Interlocked.Increment(ref state.Dropped);
            if (overflow)
            {
                Interlocked.Increment(ref state.Overflows);
            }
            if (disconnected)
            {
                Interlocked.Increment(ref state.OverflowDisconnects);
            }
        }

        public static void RecordCleared(long generation)
        {
            DecrementNonNegative(ref _queueDepth);
            CounterState state = Volatile.Read(ref _state);
            if (state.Generation == generation)
            {
                Interlocked.Increment(ref state.Cleared);
            }
        }

        public static void RecordRescheduled(long generation)
        {
            CounterState state = Volatile.Read(ref _state);
            if (state.Generation == generation)
            {
                Interlocked.Increment(ref state.Reschedules);
            }
        }

        public static long RecordWorkerStarted()
        {
            CounterState state = Volatile.Read(ref _state);
            int active = Interlocked.Increment(ref _activeWorkers);
            UpdateMaximum(ref state.MaximumActiveWorkers, active);
            return state.Generation;
        }

        public static void RecordDrain(long generation, long elapsedTicks, int packetCount)
        {
            DecrementNonNegative(ref _activeWorkers);
            CounterState state = Volatile.Read(ref _state);
            if (state.Generation != generation)
            {
                return;
            }

            Interlocked.Increment(ref state.Drains);
            Interlocked.Add(ref state.DrainTicks, elapsedTicks);
            Interlocked.Add(ref state.PacketsInDrains, packetCount);

            // Handler execution is included in drain duration. A single message with one
            // follower is not meaningful ingress pressure. Record the slow maximum only
            // when a drain handled multiple messages or at least two messages remain queued.
            if (packetCount > 1 || Interlocked.Read(ref _queueDepth) > 1)
            {
                UpdateMaximum(ref state.MaximumDrainTicks, elapsedTicks);
            }

            UpdateMaximum(ref state.MaximumPacketsPerDrain, packetCount);
        }

        public static void RecordError(long generation)
        {
            CounterState state = Volatile.Read(ref _state);
            if (state.Generation == generation)
            {
                Interlocked.Increment(ref state.Errors);
            }
        }

        public static PacketIngressSnapshot Capture()
        {
            CounterState state = Volatile.Read(ref _state);
            long drains = Interlocked.Read(ref state.Drains);
            long processed = Interlocked.Read(ref state.Processed);
            long drainTicks = Interlocked.Read(ref state.DrainTicks);
            long packetsInDrains = Interlocked.Read(ref state.PacketsInDrains);
            long queueWaitTicks = Interlocked.Read(ref state.QueueWaitTicks);

            return new PacketIngressSnapshot
            {
                QueueCapacityPerSession = QueueCapacity,
                QueueDepth = Interlocked.Read(ref _queueDepth),
                MaximumSessionQueueDepth = Interlocked.Read(ref state.MaximumSessionQueueDepth),
                QueueHighWatermark = Interlocked.Read(ref state.QueueHighWatermark),
                Enqueued = Interlocked.Read(ref state.Enqueued),
                Processed = processed,
                Dropped = Interlocked.Read(ref state.Dropped),
                Overflows = Interlocked.Read(ref state.Overflows),
                OverflowDisconnects = Interlocked.Read(ref state.OverflowDisconnects),
                Drains = drains,
                Reschedules = Interlocked.Read(ref state.Reschedules),
                Cleared = Interlocked.Read(ref state.Cleared),
                Errors = Interlocked.Read(ref state.Errors),
                ActiveWorkers = Volatile.Read(ref _activeWorkers),
                MaximumActiveWorkers = Interlocked.Read(ref state.MaximumActiveWorkers),
                AverageDrainMilliseconds = drains == 0 ? 0 : TicksToMilliseconds(drainTicks) / drains,
                MaximumDrainMilliseconds = TicksToMilliseconds(Interlocked.Read(ref state.MaximumDrainTicks)),
                AveragePacketsPerDrain = drains == 0 ? 0 : packetsInDrains / (double)drains,
                MaximumPacketsPerDrain = Interlocked.Read(ref state.MaximumPacketsPerDrain),
                AverageQueueWaitMilliseconds = processed == 0 ? 0 : TicksToMilliseconds(queueWaitTicks) / processed,
                MaximumQueueWaitMilliseconds = TicksToMilliseconds(Interlocked.Read(ref state.MaximumQueueWaitTicks))
            };
        }

        public static void Reset()
        {
            long generation = Interlocked.Increment(ref _generation);
            Interlocked.Exchange(
                ref _state,
                new CounterState(generation, Interlocked.Read(ref _queueDepth), Volatile.Read(ref _activeWorkers)));
        }

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

        private static void DecrementNonNegative(ref int value)
        {
            while (true)
            {
                int current = Volatile.Read(ref value);
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
