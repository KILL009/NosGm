using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Frostvein.Core.Diagnostics
{
    /// <summary>
    /// Low-overhead process, network and packet-handler telemetry shared by the
    /// networking core and the GM diagnostics commands.
    /// </summary>
    public sealed class ServerPerformanceMonitor : IDisposable
    {
        private static readonly Lazy<ServerPerformanceMonitor> LazyInstance =
            new Lazy<ServerPerformanceMonitor>(() => new ServerPerformanceMonitor());

        private readonly ConcurrentDictionary<string, HandlerCounter> _handlers =
            new ConcurrentDictionary<string, HandlerCounter>(StringComparer.OrdinalIgnoreCase);
        private readonly System.Threading.Timer _sampleTimer;

        private long _receivedPackets;
        private long _receivedBytes;
        private long _sentPackets;
        private long _sentBytes;
        private long _handledPackets;
        private long _handlerErrors;
        private long _handlerElapsedTicks;
        private long _handlerMaxTicks;

        private long _receivedPacketsInterval;
        private long _receivedBytesInterval;
        private long _sentPacketsInterval;
        private long _sentBytesInterval;
        private long _handledPacketsInterval;
        private long _handlerErrorsInterval;
        private long _handlerElapsedTicksInterval;
        private long _handlerMaxTicksInterval;

        private long _receivedPacketsPerSecond;
        private long _receivedBytesPerSecond;
        private long _sentPacketsPerSecond;
        private long _sentBytesPerSecond;
        private long _handledPacketsPerSecond;
        private long _handlerErrorsPerSecond;
        private long _handlerAverageMicroseconds;
        private long _handlerMaximumMicroseconds;
        private long _cpuHundredths;

        private long _lastSampleTimestamp;
        private long _lastProcessorTicks;
        private long _peakWorkingSet;
        private long _peakManagedHeap;
        private int _disposed;

        private ServerPerformanceMonitor()
        {
            using (Process process = Process.GetCurrentProcess())
            {
                process.Refresh();
                _lastProcessorTicks = process.TotalProcessorTime.Ticks;
            }

            _lastSampleTimestamp = Stopwatch.GetTimestamp();
            _sampleTimer = new System.Threading.Timer(Sample, null, 1000, 1000);
        }

        public static ServerPerformanceMonitor Instance => LazyInstance.Value;

        public void RecordReceived(int byteCount)
        {
            Interlocked.Increment(ref _receivedPackets);
            Interlocked.Increment(ref _receivedPacketsInterval);
            if (byteCount > 0)
            {
                Interlocked.Add(ref _receivedBytes, byteCount);
                Interlocked.Add(ref _receivedBytesInterval, byteCount);
            }
        }

        public void RecordSent(int byteCount)
        {
            Interlocked.Increment(ref _sentPackets);
            Interlocked.Increment(ref _sentPacketsInterval);
            if (byteCount > 0)
            {
                Interlocked.Add(ref _sentBytes, byteCount);
                Interlocked.Add(ref _sentBytesInterval, byteCount);
            }
        }

        public void RecordHandler(string header, long elapsedStopwatchTicks, bool succeeded)
        {
            if (elapsedStopwatchTicks < 0)
            {
                elapsedStopwatchTicks = 0;
            }

            Interlocked.Increment(ref _handledPackets);
            Interlocked.Increment(ref _handledPacketsInterval);
            Interlocked.Add(ref _handlerElapsedTicks, elapsedStopwatchTicks);
            Interlocked.Add(ref _handlerElapsedTicksInterval, elapsedStopwatchTicks);
            UpdateMaximum(ref _handlerMaxTicks, elapsedStopwatchTicks);
            UpdateMaximum(ref _handlerMaxTicksInterval, elapsedStopwatchTicks);

            if (!succeeded)
            {
                Interlocked.Increment(ref _handlerErrors);
                Interlocked.Increment(ref _handlerErrorsInterval);
            }

            string safeHeader = string.IsNullOrWhiteSpace(header) ? "<unknown>" : header.Trim();
            if (_handlers.Count < 1024 || _handlers.ContainsKey(safeHeader))
            {
                _handlers.GetOrAdd(safeHeader, _ => new HandlerCounter())
                    .Record(elapsedStopwatchTicks, succeeded);
            }
        }

        public PerformanceSnapshot Capture()
        {
            using (Process process = Process.GetCurrentProcess())
            {
                process.Refresh();
                long managedHeap = GC.GetTotalMemory(false);
                UpdateMaximum(ref _peakWorkingSet, process.WorkingSet64);
                UpdateMaximum(ref _peakManagedHeap, managedHeap);

                ThreadPool.GetAvailableThreads(out int availableWorker, out int availableIo);
                ThreadPool.GetMaxThreads(out int maximumWorker, out int maximumIo);

                return new PerformanceSnapshot
                {
                    CapturedAtUtc = DateTime.UtcNow,
                    Uptime = DateTime.Now - process.StartTime,
                    CpuPercent = Interlocked.Read(ref _cpuHundredths) / 100d,
                    WorkingSetBytes = process.WorkingSet64,
                    PrivateBytes = process.PrivateMemorySize64,
                    ManagedHeapBytes = managedHeap,
                    PeakWorkingSetBytes = Interlocked.Read(ref _peakWorkingSet),
                    PeakManagedHeapBytes = Interlocked.Read(ref _peakManagedHeap),
                    ProcessThreads = process.Threads.Count,
                    HandleCount = process.HandleCount,
                    ThreadPoolBusyWorker = maximumWorker - availableWorker,
                    ThreadPoolMaximumWorker = maximumWorker,
                    ThreadPoolBusyIo = maximumIo - availableIo,
                    ThreadPoolMaximumIo = maximumIo,
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2),
                    ReceivedPackets = Interlocked.Read(ref _receivedPackets),
                    ReceivedBytes = Interlocked.Read(ref _receivedBytes),
                    SentPackets = Interlocked.Read(ref _sentPackets),
                    SentBytes = Interlocked.Read(ref _sentBytes),
                    HandledPackets = Interlocked.Read(ref _handledPackets),
                    HandlerErrors = Interlocked.Read(ref _handlerErrors),
                    ReceivedPacketsPerSecond = Interlocked.Read(ref _receivedPacketsPerSecond),
                    ReceivedBytesPerSecond = Interlocked.Read(ref _receivedBytesPerSecond),
                    SentPacketsPerSecond = Interlocked.Read(ref _sentPacketsPerSecond),
                    SentBytesPerSecond = Interlocked.Read(ref _sentBytesPerSecond),
                    HandledPacketsPerSecond = Interlocked.Read(ref _handledPacketsPerSecond),
                    HandlerErrorsPerSecond = Interlocked.Read(ref _handlerErrorsPerSecond),
                    HandlerAverageMilliseconds = Interlocked.Read(ref _handlerAverageMicroseconds) / 1000d,
                    HandlerMaximumMilliseconds = Interlocked.Read(ref _handlerMaximumMicroseconds) / 1000d,
                    HandlerLifetimeAverageMilliseconds = StopwatchTicksToMilliseconds(
                        Interlocked.Read(ref _handlerElapsedTicks),
                        Math.Max(1, Interlocked.Read(ref _handledPackets))),
                    HandlerLifetimeMaximumMilliseconds = StopwatchTicksToMilliseconds(
                        Interlocked.Read(ref _handlerMaxTicks), 1)
                };
            }
        }

        public IReadOnlyList<HandlerPerformanceSnapshot> GetTopHandlers(
            int take = 10,
            HandlerSort sort = HandlerSort.TotalTime)
        {
            if (take < 1)
            {
                take = 1;
            }
            else if (take > 50)
            {
                take = 50;
            }

            IEnumerable<HandlerPerformanceSnapshot> snapshots = _handlers
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
                    snapshots = snapshots.OrderByDescending(metric => metric.Errors)
                        .ThenByDescending(metric => metric.Count);
                    break;
                default:
                    snapshots = snapshots.OrderByDescending(metric => metric.TotalMilliseconds);
                    break;
            }

            return snapshots.Take(take).ToList();
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _receivedPackets, 0);
            Interlocked.Exchange(ref _receivedBytes, 0);
            Interlocked.Exchange(ref _sentPackets, 0);
            Interlocked.Exchange(ref _sentBytes, 0);
            Interlocked.Exchange(ref _handledPackets, 0);
            Interlocked.Exchange(ref _handlerErrors, 0);
            Interlocked.Exchange(ref _handlerElapsedTicks, 0);
            Interlocked.Exchange(ref _handlerMaxTicks, 0);
            Interlocked.Exchange(ref _peakWorkingSet, 0);
            Interlocked.Exchange(ref _peakManagedHeap, 0);
            _handlers.Clear();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _sampleTimer.Dispose();
            }
        }

        private void Sample(object state)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            try
            {
                long now = Stopwatch.GetTimestamp();
                long previousTimestamp = Interlocked.Exchange(ref _lastSampleTimestamp, now);
                long elapsedTimestamp = now - previousTimestamp;

                using (Process process = Process.GetCurrentProcess())
                {
                    process.Refresh();
                    long processorTicks = process.TotalProcessorTime.Ticks;
                    long previousProcessorTicks = Interlocked.Exchange(ref _lastProcessorTicks, processorTicks);
                    long processorDelta = Math.Max(0, processorTicks - previousProcessorTicks);
                    double elapsedSeconds = elapsedTimestamp <= 0
                        ? 0
                        : elapsedTimestamp / (double)Stopwatch.Frequency;
                    double cpu = elapsedSeconds <= 0
                        ? 0
                        : processorDelta / (double)TimeSpan.TicksPerSecond /
                          elapsedSeconds / Math.Max(1, Environment.ProcessorCount) * 100d;
                    cpu = Math.Max(0, Math.Min(100, cpu));
                    Interlocked.Exchange(ref _cpuHundredths, (long)Math.Round(cpu * 100d));

                    UpdateMaximum(ref _peakWorkingSet, process.WorkingSet64);
                    UpdateMaximum(ref _peakManagedHeap, GC.GetTotalMemory(false));
                }

                Interlocked.Exchange(ref _receivedPacketsPerSecond,
                    Interlocked.Exchange(ref _receivedPacketsInterval, 0));
                Interlocked.Exchange(ref _receivedBytesPerSecond,
                    Interlocked.Exchange(ref _receivedBytesInterval, 0));
                Interlocked.Exchange(ref _sentPacketsPerSecond,
                    Interlocked.Exchange(ref _sentPacketsInterval, 0));
                Interlocked.Exchange(ref _sentBytesPerSecond,
                    Interlocked.Exchange(ref _sentBytesInterval, 0));
                long handled = Interlocked.Exchange(ref _handledPacketsInterval, 0);
                long elapsed = Interlocked.Exchange(ref _handlerElapsedTicksInterval, 0);
                long maximum = Interlocked.Exchange(ref _handlerMaxTicksInterval, 0);
                Interlocked.Exchange(ref _handledPacketsPerSecond, handled);
                Interlocked.Exchange(ref _handlerErrorsPerSecond,
                    Interlocked.Exchange(ref _handlerErrorsInterval, 0));
                Interlocked.Exchange(ref _handlerAverageMicroseconds,
                    handled <= 0 ? 0 : StopwatchTicksToMicroseconds(elapsed) / handled);
                Interlocked.Exchange(ref _handlerMaximumMicroseconds,
                    StopwatchTicksToMicroseconds(maximum));
            }
            catch
            {
                // Telemetry must never affect the game loop.
            }
        }

        private static void UpdateMaximum(ref long target, long value)
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

        private static long StopwatchTicksToMicroseconds(long ticks) =>
            ticks <= 0 ? 0 : (long)(ticks * 1000000d / Stopwatch.Frequency);

        private static double StopwatchTicksToMilliseconds(long ticks, long divisor) =>
            ticks <= 0 || divisor <= 0
                ? 0
                : ticks * 1000d / Stopwatch.Frequency / divisor;

        private sealed class HandlerCounter
        {
            private long _count;
            private long _errors;
            private long _elapsedTicks;
            private long _maximumTicks;

            public void Record(long elapsedTicks, bool succeeded)
            {
                Interlocked.Increment(ref _count);
                Interlocked.Add(ref _elapsedTicks, elapsedTicks);
                UpdateMaximum(ref _maximumTicks, elapsedTicks);
                if (!succeeded)
                {
                    Interlocked.Increment(ref _errors);
                }
            }

            public HandlerPerformanceSnapshot Capture(string header)
            {
                long count = Interlocked.Read(ref _count);
                long elapsed = Interlocked.Read(ref _elapsedTicks);
                long maximum = Interlocked.Read(ref _maximumTicks);
                return new HandlerPerformanceSnapshot
                {
                    Header = header,
                    Count = count,
                    Errors = Interlocked.Read(ref _errors),
                    TotalMilliseconds = StopwatchTicksToMilliseconds(elapsed, 1),
                    AverageMilliseconds = StopwatchTicksToMilliseconds(elapsed, Math.Max(1, count)),
                    MaximumMilliseconds = StopwatchTicksToMilliseconds(maximum, 1)
                };
            }
        }
    }

    public enum HandlerSort
    {
        TotalTime = 0,
        Count = 1,
        AverageTime = 2,
        MaximumTime = 3,
        Errors = 4
    }

    public sealed class PerformanceSnapshot
    {
        public DateTime CapturedAtUtc { get; set; }
        public TimeSpan Uptime { get; set; }
        public double CpuPercent { get; set; }
        public long WorkingSetBytes { get; set; }
        public long PrivateBytes { get; set; }
        public long ManagedHeapBytes { get; set; }
        public long PeakWorkingSetBytes { get; set; }
        public long PeakManagedHeapBytes { get; set; }
        public int ProcessThreads { get; set; }
        public int HandleCount { get; set; }
        public int ThreadPoolBusyWorker { get; set; }
        public int ThreadPoolMaximumWorker { get; set; }
        public int ThreadPoolBusyIo { get; set; }
        public int ThreadPoolMaximumIo { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public long ReceivedPackets { get; set; }
        public long ReceivedBytes { get; set; }
        public long SentPackets { get; set; }
        public long SentBytes { get; set; }
        public long HandledPackets { get; set; }
        public long HandlerErrors { get; set; }
        public long ReceivedPacketsPerSecond { get; set; }
        public long ReceivedBytesPerSecond { get; set; }
        public long SentPacketsPerSecond { get; set; }
        public long SentBytesPerSecond { get; set; }
        public long HandledPacketsPerSecond { get; set; }
        public long HandlerErrorsPerSecond { get; set; }
        public double HandlerAverageMilliseconds { get; set; }
        public double HandlerMaximumMilliseconds { get; set; }
        public double HandlerLifetimeAverageMilliseconds { get; set; }
        public double HandlerLifetimeMaximumMilliseconds { get; set; }
    }

    public sealed class HandlerPerformanceSnapshot
    {
        public string Header { get; set; }
        public long Count { get; set; }
        public long Errors { get; set; }
        public double TotalMilliseconds { get; set; }
        public double AverageMilliseconds { get; set; }
        public double MaximumMilliseconds { get; set; }
    }
}
