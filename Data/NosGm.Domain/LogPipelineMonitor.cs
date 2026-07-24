using System;
using System.Diagnostics;
using System.Threading;

namespace NosGm.Domain
{
    public enum DailyActionClaimResult : byte
    {
        Claimed = 0,
        AlreadyClaimed = 1,
        StorageUnavailable = 2,
        Error = 3
    }

    public sealed class LogPipelineSnapshot
    {
        public int GeneralLogQueueDepth { get; internal set; }
        public int GeneralLogQueueCapacity { get; internal set; }
        public long GeneralLogEnqueued { get; internal set; }
        public long GeneralLogQueueFallbacks { get; internal set; }
        public long GeneralLogDropped { get; internal set; }
        public long GeneralLogWritten { get; internal set; }
        public long GeneralLogBatches { get; internal set; }
        public long GeneralLogWriteErrors { get; internal set; }
        public double GeneralLogAverageBatchMilliseconds { get; internal set; }
        public double GeneralLogMaximumBatchMilliseconds { get; internal set; }

        public long DailyActionAttempts { get; internal set; }
        public long DailyActionClaimed { get; internal set; }
        public long DailyActionDuplicates { get; internal set; }
        public long DailyActionUnavailable { get; internal set; }
        public long DailyActionErrors { get; internal set; }

        public int UdpQueueDepth { get; internal set; }
        public int UdpQueueCapacity { get; internal set; }
        public long UdpEnqueued { get; internal set; }
        public long UdpSent { get; internal set; }
        public long UdpDropped { get; internal set; }
        public long UdpErrors { get; internal set; }

        public long MongoAttempts { get; internal set; }
        public long MongoSucceeded { get; internal set; }
        public long MongoErrors { get; internal set; }
        public double MongoAverageMilliseconds { get; internal set; }
        public double MongoMaximumMilliseconds { get; internal set; }
    }

    public static class LogPipelineMonitor
    {
        private static int _generalLogQueueDepth;
        private static int _generalLogQueueCapacity;
        private static long _generalLogEnqueued;
        private static long _generalLogQueueFallbacks;
        private static long _generalLogDropped;
        private static long _generalLogWritten;
        private static long _generalLogBatches;
        private static long _generalLogWriteErrors;
        private static long _generalLogTotalBatchTicks;
        private static long _generalLogMaximumBatchTicks;

        private static long _dailyActionAttempts;
        private static long _dailyActionClaimed;
        private static long _dailyActionDuplicates;
        private static long _dailyActionUnavailable;
        private static long _dailyActionErrors;

        private static int _udpQueueDepth;
        private static int _udpQueueCapacity;
        private static long _udpEnqueued;
        private static long _udpSent;
        private static long _udpDropped;
        private static long _udpErrors;

        private static long _mongoAttempts;
        private static long _mongoSucceeded;
        private static long _mongoErrors;
        private static long _mongoTotalTicks;
        private static long _mongoMaximumTicks;

        public static void RecordGeneralLogEnqueued(int queueDepth, int queueCapacity)
        {
            Interlocked.Increment(ref _generalLogEnqueued);
            Volatile.Write(ref _generalLogQueueDepth, queueDepth);
            Volatile.Write(ref _generalLogQueueCapacity, queueCapacity);
        }

        public static void UpdateGeneralLogQueue(int queueDepth, int queueCapacity)
        {
            Volatile.Write(ref _generalLogQueueDepth, queueDepth);
            Volatile.Write(ref _generalLogQueueCapacity, queueCapacity);
        }

        public static void RecordGeneralLogFallback()
        {
            Interlocked.Increment(ref _generalLogQueueFallbacks);
        }

        public static void RecordGeneralLogDropped(int count = 1)
        {
            if (count > 0)
            {
                Interlocked.Add(ref _generalLogDropped, count);
            }
        }

        public static void RecordGeneralLogWrite(int recordCount, long elapsedStopwatchTicks, bool success)
        {
            Interlocked.Increment(ref _generalLogBatches);
            Interlocked.Add(ref _generalLogTotalBatchTicks, Math.Max(0, elapsedStopwatchTicks));
            AtomicMaximum(ref _generalLogMaximumBatchTicks, elapsedStopwatchTicks);

            if (success)
            {
                Interlocked.Add(ref _generalLogWritten, Math.Max(0, recordCount));
            }
            else
            {
                Interlocked.Increment(ref _generalLogWriteErrors);
            }
        }

        public static void RecordDailyAction(DailyActionClaimResult result)
        {
            Interlocked.Increment(ref _dailyActionAttempts);
            switch (result)
            {
                case DailyActionClaimResult.Claimed:
                    Interlocked.Increment(ref _dailyActionClaimed);
                    break;
                case DailyActionClaimResult.AlreadyClaimed:
                    Interlocked.Increment(ref _dailyActionDuplicates);
                    break;
                case DailyActionClaimResult.StorageUnavailable:
                    Interlocked.Increment(ref _dailyActionUnavailable);
                    break;
                default:
                    Interlocked.Increment(ref _dailyActionErrors);
                    break;
            }
        }

        public static void RecordUdpEnqueued(int queueDepth, int queueCapacity)
        {
            Interlocked.Increment(ref _udpEnqueued);
            Volatile.Write(ref _udpQueueDepth, queueDepth);
            Volatile.Write(ref _udpQueueCapacity, queueCapacity);
        }

        public static void RecordUdpSent(int queueDepth, int queueCapacity)
        {
            Interlocked.Increment(ref _udpSent);
            Volatile.Write(ref _udpQueueDepth, queueDepth);
            Volatile.Write(ref _udpQueueCapacity, queueCapacity);
        }

        public static void RecordUdpDropped(int queueDepth, int queueCapacity)
        {
            Interlocked.Increment(ref _udpDropped);
            Volatile.Write(ref _udpQueueDepth, queueDepth);
            Volatile.Write(ref _udpQueueCapacity, queueCapacity);
        }

        public static void RecordUdpError(int queueDepth, int queueCapacity)
        {
            Interlocked.Increment(ref _udpErrors);
            Volatile.Write(ref _udpQueueDepth, queueDepth);
            Volatile.Write(ref _udpQueueCapacity, queueCapacity);
        }

        public static void RecordMongoWrite(long elapsedStopwatchTicks, bool success)
        {
            Interlocked.Increment(ref _mongoAttempts);
            Interlocked.Add(ref _mongoTotalTicks, Math.Max(0, elapsedStopwatchTicks));
            AtomicMaximum(ref _mongoMaximumTicks, elapsedStopwatchTicks);
            if (success)
            {
                Interlocked.Increment(ref _mongoSucceeded);
            }
            else
            {
                Interlocked.Increment(ref _mongoErrors);
            }
        }

        public static LogPipelineSnapshot Capture()
        {
            long generalBatches = Interlocked.Read(ref _generalLogBatches);
            long mongoAttempts = Interlocked.Read(ref _mongoAttempts);

            return new LogPipelineSnapshot
            {
                GeneralLogQueueDepth = Volatile.Read(ref _generalLogQueueDepth),
                GeneralLogQueueCapacity = Volatile.Read(ref _generalLogQueueCapacity),
                GeneralLogEnqueued = Interlocked.Read(ref _generalLogEnqueued),
                GeneralLogQueueFallbacks = Interlocked.Read(ref _generalLogQueueFallbacks),
                GeneralLogDropped = Interlocked.Read(ref _generalLogDropped),
                GeneralLogWritten = Interlocked.Read(ref _generalLogWritten),
                GeneralLogBatches = generalBatches,
                GeneralLogWriteErrors = Interlocked.Read(ref _generalLogWriteErrors),
                GeneralLogAverageBatchMilliseconds = StopwatchTicksToMilliseconds(
                    generalBatches == 0 ? 0 : Interlocked.Read(ref _generalLogTotalBatchTicks) / generalBatches),
                GeneralLogMaximumBatchMilliseconds = StopwatchTicksToMilliseconds(
                    Interlocked.Read(ref _generalLogMaximumBatchTicks)),

                DailyActionAttempts = Interlocked.Read(ref _dailyActionAttempts),
                DailyActionClaimed = Interlocked.Read(ref _dailyActionClaimed),
                DailyActionDuplicates = Interlocked.Read(ref _dailyActionDuplicates),
                DailyActionUnavailable = Interlocked.Read(ref _dailyActionUnavailable),
                DailyActionErrors = Interlocked.Read(ref _dailyActionErrors),

                UdpQueueDepth = Volatile.Read(ref _udpQueueDepth),
                UdpQueueCapacity = Volatile.Read(ref _udpQueueCapacity),
                UdpEnqueued = Interlocked.Read(ref _udpEnqueued),
                UdpSent = Interlocked.Read(ref _udpSent),
                UdpDropped = Interlocked.Read(ref _udpDropped),
                UdpErrors = Interlocked.Read(ref _udpErrors),

                MongoAttempts = mongoAttempts,
                MongoSucceeded = Interlocked.Read(ref _mongoSucceeded),
                MongoErrors = Interlocked.Read(ref _mongoErrors),
                MongoAverageMilliseconds = StopwatchTicksToMilliseconds(
                    mongoAttempts == 0 ? 0 : Interlocked.Read(ref _mongoTotalTicks) / mongoAttempts),
                MongoMaximumMilliseconds = StopwatchTicksToMilliseconds(
                    Interlocked.Read(ref _mongoMaximumTicks))
            };
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref _generalLogEnqueued, 0);
            Interlocked.Exchange(ref _generalLogQueueFallbacks, 0);
            Interlocked.Exchange(ref _generalLogDropped, 0);
            Interlocked.Exchange(ref _generalLogWritten, 0);
            Interlocked.Exchange(ref _generalLogBatches, 0);
            Interlocked.Exchange(ref _generalLogWriteErrors, 0);
            Interlocked.Exchange(ref _generalLogTotalBatchTicks, 0);
            Interlocked.Exchange(ref _generalLogMaximumBatchTicks, 0);

            Interlocked.Exchange(ref _dailyActionAttempts, 0);
            Interlocked.Exchange(ref _dailyActionClaimed, 0);
            Interlocked.Exchange(ref _dailyActionDuplicates, 0);
            Interlocked.Exchange(ref _dailyActionUnavailable, 0);
            Interlocked.Exchange(ref _dailyActionErrors, 0);

            Interlocked.Exchange(ref _udpEnqueued, 0);
            Interlocked.Exchange(ref _udpSent, 0);
            Interlocked.Exchange(ref _udpDropped, 0);
            Interlocked.Exchange(ref _udpErrors, 0);

            Interlocked.Exchange(ref _mongoAttempts, 0);
            Interlocked.Exchange(ref _mongoSucceeded, 0);
            Interlocked.Exchange(ref _mongoErrors, 0);
            Interlocked.Exchange(ref _mongoTotalTicks, 0);
            Interlocked.Exchange(ref _mongoMaximumTicks, 0);
        }

        private static void AtomicMaximum(ref long target, long value)
        {
            long current = Interlocked.Read(ref target);
            while (value > current)
            {
                long observed = Interlocked.CompareExchange(ref target, value, current);
                if (observed == current)
                {
                    return;
                }
                current = observed;
            }
        }

        private static double StopwatchTicksToMilliseconds(long ticks) =>
            ticks <= 0 ? 0 : ticks * 1000d / Stopwatch.Frequency;
    }
}
