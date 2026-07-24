using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.Data;
using NosGm.Domain;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace NosGm.DAL.DAO
{
    internal static class GeneralLogBatchWriter
    {
        private const int BatchSize = 200;
        private const int FlushIntervalMilliseconds = 250;
        private const int MaximumAttempts = 3;
        private const int QueueCapacity = LogPipelineMonitor.DefaultGeneralLogQueueCapacity;

        private static readonly BlockingCollection<QueuedGeneralLog> Queue =
            new BlockingCollection<QueuedGeneralLog>(
                new ConcurrentQueue<QueuedGeneralLog>(), QueueCapacity);

        private static readonly Thread Worker = new Thread(ProcessQueue)
        {
            IsBackground = true,
            Name = "NosGM-GeneralLog-Writer"
        };

        private static int _activeWrites;
        private static int _stopping;

        static GeneralLogBatchWriter()
        {
            LogPipelineMonitor.UpdateGeneralLogQueue(0, QueueCapacity);
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => Stop();
            Worker.Start();
        }

        public static bool TryEnqueue(GeneralLogDTO generalLog)
        {
            LogPipelineOperation operation = LogPipelineMonitor.CurrentOperation;
            if (generalLog == null || Volatile.Read(ref _stopping) != 0 || Queue.IsAddingCompleted)
            {
                LogPipelineMonitor.RecordGeneralLogFallback(operation);
                return false;
            }

            var queued = new QueuedGeneralLog(Clone(generalLog), 1, operation);
            if (!Queue.TryAdd(queued))
            {
                LogPipelineMonitor.RecordGeneralLogFallback(operation);
                LogPipelineMonitor.UpdateGeneralLogQueue(Queue.Count, QueueCapacity);
                return false;
            }

            LogPipelineMonitor.RecordGeneralLogEnqueued(operation, Queue.Count, QueueCapacity);
            return true;
        }

        public static bool Flush(TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow.Add(timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout);
            while ((Queue.Count > 0 || Volatile.Read(ref _activeWrites) > 0) && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(10);
            }

            LogPipelineMonitor.UpdateGeneralLogQueue(Queue.Count, QueueCapacity);
            return Queue.Count == 0 && Volatile.Read(ref _activeWrites) == 0;
        }

        private static void Stop()
        {
            if (Interlocked.Exchange(ref _stopping, 1) != 0)
            {
                return;
            }

            Queue.CompleteAdding();
            Worker.Join(TimeSpan.FromSeconds(5));
        }

        private static void ProcessQueue()
        {
            var batch = new List<QueuedGeneralLog>(BatchSize);
            while (!Queue.IsCompleted)
            {
                QueuedGeneralLog first;
                if (!Queue.TryTake(out first, FlushIntervalMilliseconds))
                {
                    LogPipelineMonitor.UpdateGeneralLogQueue(Queue.Count, QueueCapacity);
                    continue;
                }

                batch.Clear();
                batch.Add(first);
                QueuedGeneralLog next;
                while (batch.Count < BatchSize && Queue.TryTake(out next))
                {
                    batch.Add(next);
                }

                Interlocked.Increment(ref _activeWrites);
                try
                {
                    WriteBatch(batch);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeWrites);
                    LogPipelineMonitor.UpdateGeneralLogQueue(Queue.Count, QueueCapacity);
                }
            }
        }

        private static void WriteBatch(IReadOnlyCollection<QueuedGeneralLog> batch)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    var entities = new List<GeneralLog>(batch.Count);
                    foreach (QueuedGeneralLog queued in batch)
                    {
                        var entity = new GeneralLog();
                        GeneralLogMapper.ToGeneralLog(queued.Value, entity);
                        entities.Add(entity);
                    }

                    context.GeneralLog.AddRange(entities);
                    context.SaveChanges();
                }

                stopwatch.Stop();
                RecordBatchResult(batch, stopwatch.ElapsedTicks, true);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                RecordBatchResult(batch, stopwatch.ElapsedTicks, false);
                Logger.Error($"Unable to persist a GeneralLog batch containing {batch.Count} records.", exception);
                RetryOrDrop(batch);
            }
        }

        private static void RecordBatchResult(
            IEnumerable<QueuedGeneralLog> batch,
            long elapsedStopwatchTicks,
            bool success)
        {
            foreach (IGrouping<LogPipelineOperation, QueuedGeneralLog> group in
                     batch.GroupBy(queued => queued.Operation))
            {
                LogPipelineMonitor.RecordGeneralLogWrite(
                    group.Key,
                    group.Count(),
                    elapsedStopwatchTicks,
                    success);
            }
        }

        private static void RetryOrDrop(IEnumerable<QueuedGeneralLog> batch)
        {
            Thread.Sleep(FlushIntervalMilliseconds);
            foreach (QueuedGeneralLog queued in batch)
            {
                if (queued.Attempt >= MaximumAttempts || Queue.IsAddingCompleted ||
                    !Queue.TryAdd(new QueuedGeneralLog(
                        queued.Value,
                        queued.Attempt + 1,
                        queued.Operation)))
                {
                    LogPipelineMonitor.RecordGeneralLogDropped(queued.Operation);
                }
            }
        }

        private static GeneralLogDTO Clone(GeneralLogDTO source)
        {
            return new GeneralLogDTO
            {
                AccountId = source.AccountId,
                CharacterId = source.CharacterId,
                IpAddress = source.IpAddress,
                LogData = source.LogData,
                LogId = 0,
                LogType = source.LogType,
                Timestamp = source.Timestamp == default(DateTime) ? DateTime.Now : source.Timestamp
            };
        }

        private sealed class QueuedGeneralLog
        {
            public QueuedGeneralLog(
                GeneralLogDTO value,
                int attempt,
                LogPipelineOperation operation)
            {
                Value = value;
                Attempt = attempt;
                Operation = operation;
            }

            public GeneralLogDTO Value { get; }

            public int Attempt { get; }

            public LogPipelineOperation Operation { get; }
        }
    }
}
