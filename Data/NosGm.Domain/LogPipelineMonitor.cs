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

    public sealed class LogPipelineOperation
    {
        internal LogPipelineOperation(LogPipelineCounterState state)
        {
            State = state;
        }

        internal LogPipelineCounterState State { get; }
    }

    internal sealed class LogPipelineCounterState
    {
        public LogPipelineCounterState()
        {
            Operation = new LogPipelineOperation(this);
        }

        public LogPipelineOperation Operation { get; }

        public long GeneralLogEnqueued;
        public long GeneralLogQueueFallbacks;
        public long GeneralLogDropped;
        public long GeneralLogWritten;
        public long GeneralLogBatches;
        public long GeneralLogWriteErrors;
        public long GeneralLogTotalBatchTicks;
        public long GeneralLogMaximumBatchTicks;

        public long DailyActionAttempts;
        public long DailyActionClaimed;
        public long DailyActionDuplicates;
        public long DailyActionUnavailable;
        public long DailyActionErrors;

        public long UdpEnqueued;
        public long UdpSent;
        public long UdpDropped;
        public long UdpErrors;

        public long MongoAttempts;
        public long MongoSucceeded;
        public long MongoErrors;
        public long MongoTotalTicks;
        public long MongoMaximumTicks;
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
        public long UdpInFlight { get; internal set; }

        public long MongoAttempts { get; internal set; }
        public long MongoSucceeded { get; internal set; }
        public long MongoErrors { get; internal set; }
        public double MongoAverageMilliseconds { get; internal set; }
        public double MongoMaximumMilliseconds { get; internal set; }
    }

    public static class LogPipelineMonitor
    {
        public const int DefaultGeneralLogQueueCapacity = 5000;
        public const int DefaultUdpQueueCapacity = 4096;

        private static LogPipelineCounterState _state = new LogPipelineCounterState();

        private static int _generalLogQueueDepth;
        private static int _generalLogQueueCapacity = DefaultGeneralLogQueueCapacity;
        private static int _udpQueueDepth;
        private static int _udpQueueCapacity = DefaultUdpQueueCapacity;

        public static LogPipelineOperation CurrentOperation =>
            Volatile.Read(ref _state).Operation;

        public static void RecordGeneralLogEnqueued(int queueDepth, int queueCapacity) =>
            RecordGeneralLogEnqueued(CurrentOperation, queueDepth, queueCapacity);

        public static void RecordGeneralLogEnqueued(
            LogPipelineOperation operation,
            int queueDepth,
            int queueCapacity)
        {
            LogPipelineCounterState state = Resolve(operation);
            Interlocked.Increment(ref state.GeneralLogEnqueued);
            UpdateGeneralLogQueue(queueDepth, queueCapacity);
        }

        public static void UpdateGeneralLogQueue(int queueDepth, int queueCapacity)
        {
            Volatile.Write(ref _generalLogQueueDepth, Math.Max(0, queueDepth));
            Volatile.Write(ref _generalLogQueueCapacity,
                queueCapacity > 0 ? queueCapacity : DefaultGeneralLogQueueCapacity);
        }

        public static void RecordGeneralLogFallback() =>
            RecordGeneralLogFallback(CurrentOperation);

        public static void RecordGeneralLogFallback(LogPipelineOperation operation)
        {
            LogPipelineCounterState state = Resolve(operation);
            Interlocked.Increment(ref state.GeneralLogQueueFallbacks);
        }

        public static void RecordGeneralLogDropped(int count = 1) =>
            RecordGeneralLogDropped(CurrentOperation, count);

        public static void RecordGeneralLogDropped(LogPipelineOperation operation, int count = 1)
        {
            if (count <= 0)
            {
                return;
            }

            LogPipelineCounterState state = Resolve(operation);
            Interlocked.Add(ref state.GeneralLogDropped, count);
        }

        public static void RecordGeneralLogWrite(
            int recordCount,
            long elapsedStopwatchTicks,
            bool success) =>
            RecordGeneralLogWrite(CurrentOperation, recordCount, elapsedStopwatchTicks, success);

        public static void RecordGeneralLogWrite(
            LogPipelineOperation operation,
            int recordCount,
            long elapsedStopwatchTicks,
            bool success)
        {
            LogPipelineCounterState state = Resolve(operation);
            Interlocked.Increment(ref state.GeneralLogBatches);
            Interlocked.Add(ref state.GeneralLogTotalBatchTicks, Math.Max(0, elapsedStopwatchTicks));
            AtomicMaximum(ref state.GeneralLogMaximumBatchTicks, elapsedStopwatchTicks);

            if (success)
            {
                Interlocked.Add(ref state.GeneralLogWritten, Math.Max(0, recordCount));
            }
            else
            {
                Interlocked.Increment(ref state.GeneralLogWriteErrors);
            }
        }

        public static void RecordDailyAction(DailyActionClaimResult result)
        {
            LogPipelineCounterState state = Volatile.Read(ref _state);
            Interlocked.Increment(ref state.DailyActionAttempts);
            switch (result)
            {
                case DailyActionClaimResult.Claimed:
                    Interlocked.Increment(ref state.DailyActionClaimed);
                    break;
                case DailyActionClaimResult.AlreadyClaimed:
                    Interlocked.Increment(ref state.DailyActionDuplicates);
                    break;
                case DailyActionClaimResult.StorageUnavailable:
                    Interlocked.Increment(ref state.DailyActionUnavailable);
                    break;
                default:
                    Interlocked.Increment(ref state.DailyActionErrors);
                    break;
            }
        }

        public static void RecordUdpEnqueued(int queueDepth, int queueCapacity) =>
            RecordUdpEnqueued(CurrentOperation, queueDepth, queueCapacity);

        public static void RecordUdpEnqueued(
            LogPipelineOperation operation,
            int queueDepth,
            int queueCapacity)
        {
            LogPipelineCounterState state = Resolve(operation);
            Interlocked.Increment(ref state.UdpEnqueued);
            UpdateUdpQueue(queueDepth, queueCapacity);
        }

        public static void RecordUdpSent(int queueDepth, int queueCapacity) =>
            RecordUdpSent(CurrentOperation, queueDepth, queueCapacity);

        public static void RecordUdpSent(
            LogPipelineOperation operation,
            int queueDepth,
            int queueCapacity)
        {
            LogPipelineCounterState state = Resolve(operation);
            Interlocked.Increment(ref state.UdpSent);
            UpdateUdpQueue(queueDepth, queueCapacity);
        }

        public static void RecordUdpDropped(int queueDepth, int queueCapacity) =>
            RecordUdpDropped(CurrentOperation, queueDepth, queueCapacity);

        public static void RecordUdpDropped(
            LogPipelineOperation operation,
            int queueDepth,
            int queueCapacity)
        {
            LogPipelineCounterState state = Resolve(operation);
            Interlocked.Increment(ref state.UdpDropped);
            UpdateUdpQueue(queueDepth, queueCapacity);
        }

        public static void RecordUdpError(int queueDepth, int queueCapacity) =>
            RecordUdpError(CurrentOperation, queueDepth, queueCapacity);

        public static void RecordUdpError(
            LogPipelineOperation operation,
            int queueDepth,
            int queueCapacity)
        {
            LogPipelineCounterState state = Resolve(operation);
            Interlocked.Increment(ref state.UdpErrors);
            UpdateUdpQueue(queueDepth, queueCapacity);
        }

        public static void UpdateUdpQueue(int queueDepth, int queueCapacity)
        {
            Volatile.Write(ref _udpQueueDepth, Math.Max(0, queueDepth));
            Volatile.Write(ref _udpQueueCapacity,
                queueCapacity > 0 ? queueCapacity : DefaultUdpQueueCapacity);
        }

        public static void RecordMongoWrite(long elapsedStopwatchTicks, bool success) =>
            RecordMongoWrite(CurrentOperation, elapsedStopwatchTicks, success);

        public static void RecordMongoWrite(
            LogPipelineOperation operation,
            long elapsedStopwatchTicks,
            bool success)
        {
            LogPipelineCounterState state = Resolve(operation);
            Interlocked.Increment(ref state.MongoAttempts);
            Interlocked.Add(ref state.MongoTotalTicks, Math.Max(0, elapsedStopwatchTicks));
            AtomicMaximum(ref state.MongoMaximumTicks, elapsedStopwatchTicks);
            if (success)
            {
                Interlocked.Increment(ref state.MongoSucceeded);
            }
            else
            {
                Interlocked.Increment(ref state.MongoErrors);
            }
        }

        public static LogPipelineSnapshot Capture()
        {
            LogPipelineCounterState state = Volatile.Read(ref _state);
            long generalBatches = Interlocked.Read(ref state.GeneralLogBatches);
            long mongoAttempts = Interlocked.Read(ref state.MongoAttempts);
            long udpEnqueued = Interlocked.Read(ref state.UdpEnqueued);
            long udpSent = Interlocked.Read(ref state.UdpSent);
            long udpDropped = Interlocked.Read(ref state.UdpDropped);

            return new LogPipelineSnapshot
            {
                GeneralLogQueueDepth = Volatile.Read(ref _generalLogQueueDepth),
                GeneralLogQueueCapacity = Volatile.Read(ref _generalLogQueueCapacity),
                GeneralLogEnqueued = Interlocked.Read(ref state.GeneralLogEnqueued),
                GeneralLogQueueFallbacks = Interlocked.Read(ref state.GeneralLogQueueFallbacks),
                GeneralLogDropped = Interlocked.Read(ref state.GeneralLogDropped),
                GeneralLogWritten = Interlocked.Read(ref state.GeneralLogWritten),
                GeneralLogBatches = generalBatches,
                GeneralLogWriteErrors = Interlocked.Read(ref state.GeneralLogWriteErrors),
                GeneralLogAverageBatchMilliseconds = StopwatchTicksToMilliseconds(
                    generalBatches == 0
                        ? 0
                        : Interlocked.Read(ref state.GeneralLogTotalBatchTicks) / generalBatches),
                GeneralLogMaximumBatchMilliseconds = StopwatchTicksToMilliseconds(
                    Interlocked.Read(ref state.GeneralLogMaximumBatchTicks)),

                DailyActionAttempts = Interlocked.Read(ref state.DailyActionAttempts),
                DailyActionClaimed = Interlocked.Read(ref state.DailyActionClaimed),
                DailyActionDuplicates = Interlocked.Read(ref state.DailyActionDuplicates),
                DailyActionUnavailable = Interlocked.Read(ref state.DailyActionUnavailable),
                DailyActionErrors = Interlocked.Read(ref state.DailyActionErrors),

                UdpQueueDepth = Volatile.Read(ref _udpQueueDepth),
                UdpQueueCapacity = Volatile.Read(ref _udpQueueCapacity),
                UdpEnqueued = udpEnqueued,
                UdpSent = udpSent,
                UdpDropped = udpDropped,
                UdpErrors = Interlocked.Read(ref state.UdpErrors),
                UdpInFlight = Math.Max(0, udpEnqueued - udpSent - udpDropped),

                MongoAttempts = mongoAttempts,
                MongoSucceeded = Interlocked.Read(ref state.MongoSucceeded),
                MongoErrors = Interlocked.Read(ref state.MongoErrors),
                MongoAverageMilliseconds = StopwatchTicksToMilliseconds(
                    mongoAttempts == 0
                        ? 0
                        : Interlocked.Read(ref state.MongoTotalTicks) / mongoAttempts),
                MongoMaximumMilliseconds = StopwatchTicksToMilliseconds(
                    Interlocked.Read(ref state.MongoMaximumTicks))
            };
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref _state, new LogPipelineCounterState());
        }

        private static LogPipelineCounterState Resolve(LogPipelineOperation operation) =>
            operation?.State ?? Volatile.Read(ref _state);

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
