using System.Diagnostics;

namespace NosGM.LoadTest;

internal sealed record ProcessMetricSample(
    string ProcessName,
    double CpuPercent,
    long WorkingSetBytes,
    long PrivateBytes,
    int ThreadCount,
    int HandleCount,
    int ProcessCount);

internal sealed record ProcessSample(
    DateTime CapturedAtUtc,
    double CpuPercent,
    long WorkingSetBytes,
    long PrivateBytes,
    int ThreadCount,
    int HandleCount,
    int ProcessCount,
    ProcessMetricSample[] Processes);

internal sealed class ProcessSampler
{
    private readonly string[] _processNames;
    private readonly Dictionary<int, PreviousProcessSample> _previous = new();

    public ProcessSampler(IEnumerable<string> processNames)
    {
        ArgumentNullException.ThrowIfNull(processNames);
        _processNames = processNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(NormalizeProcessName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public ProcessSample Capture()
    {
        DateTime nowUtc = DateTime.UtcNow;
        long nowTimestamp = Stopwatch.GetTimestamp();
        double totalCpu = 0;
        long totalWorkingSet = 0;
        long totalPrivateBytes = 0;
        int totalThreadCount = 0;
        int totalHandleCount = 0;
        int processCount = 0;
        var liveProcessIds = new HashSet<int>();
        var processMetrics = new List<ProcessMetricSample>(_processNames.Length);

        foreach (string processName in _processNames)
        {
            double processCpu = 0;
            long processWorkingSet = 0;
            long processPrivateBytes = 0;
            int processThreadCount = 0;
            int processHandleCount = 0;
            int namedProcessCount = 0;

            foreach (Process process in Process.GetProcessesByName(processName))
            {
                using (process)
                {
                    try
                    {
                        process.Refresh();
                        int processId = process.Id;
                        long workingSet = Math.Max(0, process.WorkingSet64);
                        long privateBytes = Math.Max(0, process.PrivateMemorySize64);
                        int threadCount = Math.Max(0, process.Threads.Count);
                        int handleCount = Math.Max(0, process.HandleCount);
                        long totalProcessorTicks = process.TotalProcessorTime.Ticks;
                        double cpu = 0;

                        liveProcessIds.Add(processId);
                        namedProcessCount++;
                        processWorkingSet += workingSet;
                        processPrivateBytes += privateBytes;
                        processThreadCount += threadCount;
                        processHandleCount += handleCount;

                        if (_previous.TryGetValue(processId, out PreviousProcessSample previous))
                        {
                            long elapsedTimestamp = nowTimestamp - previous.Timestamp;
                            long processorDelta = Math.Max(0, totalProcessorTicks - previous.TotalProcessorTicks);
                            double elapsedSeconds = elapsedTimestamp <= 0
                                ? 0
                                : elapsedTimestamp / (double)Stopwatch.Frequency;
                            if (elapsedSeconds > 0)
                            {
                                cpu = processorDelta /
                                    (double)TimeSpan.TicksPerSecond /
                                    elapsedSeconds /
                                    Math.Max(1, Environment.ProcessorCount) *
                                    100d;
                                processCpu += Math.Max(0, cpu);
                            }
                        }

                        _previous[processId] = new PreviousProcessSample(
                            nowTimestamp,
                            totalProcessorTicks);
                    }
                    catch (InvalidOperationException)
                    {
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                    }
                }
            }

            processCpu = Math.Min(100d, processCpu);
            totalCpu += processCpu;
            totalWorkingSet += processWorkingSet;
            totalPrivateBytes += processPrivateBytes;
            totalThreadCount += processThreadCount;
            totalHandleCount += processHandleCount;
            processCount += namedProcessCount;

            processMetrics.Add(
                new ProcessMetricSample(
                    processName,
                    processCpu,
                    processWorkingSet,
                    processPrivateBytes,
                    processThreadCount,
                    processHandleCount,
                    namedProcessCount));
        }

        foreach (int processId in _previous.Keys.Where(id => !liveProcessIds.Contains(id)).ToArray())
        {
            _previous.Remove(processId);
        }

        return new ProcessSample(
            nowUtc,
            Math.Min(100d, totalCpu),
            totalWorkingSet,
            totalPrivateBytes,
            totalThreadCount,
            totalHandleCount,
            processCount,
            processMetrics.ToArray());
    }

    private static string NormalizeProcessName(string configuredName)
    {
        string trimmed = configuredName.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }

    private readonly record struct PreviousProcessSample(long Timestamp, long TotalProcessorTicks);
}
