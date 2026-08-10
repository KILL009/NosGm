using System.Diagnostics;

namespace NosGM.LoadTest;

internal sealed record ProcessSample(
    DateTime CapturedAtUtc,
    double CpuPercent,
    long WorkingSetBytes,
    long PrivateBytes,
    int ProcessCount);

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
        int processCount = 0;
        var liveProcessIds = new HashSet<int>();

        foreach (string processName in _processNames)
        {
            foreach (Process process in Process.GetProcessesByName(processName))
            {
                using (process)
                {
                    try
                    {
                        process.Refresh();
                        int processId = process.Id;
                        liveProcessIds.Add(processId);
                        processCount++;
                        totalWorkingSet += Math.Max(0, process.WorkingSet64);
                        totalPrivateBytes += Math.Max(0, process.PrivateMemorySize64);

                        long totalProcessorTicks = process.TotalProcessorTime.Ticks;
                        if (_previous.TryGetValue(processId, out PreviousProcessSample previous))
                        {
                            long elapsedTimestamp = nowTimestamp - previous.Timestamp;
                            long processorDelta = Math.Max(0, totalProcessorTicks - previous.TotalProcessorTicks);
                            double elapsedSeconds = elapsedTimestamp <= 0
                                ? 0
                                : elapsedTimestamp / (double)Stopwatch.Frequency;
                            if (elapsedSeconds > 0)
                            {
                                double cpu = processorDelta /
                                    (double)TimeSpan.TicksPerSecond /
                                    elapsedSeconds /
                                    Math.Max(1, Environment.ProcessorCount) *
                                    100d;
                                totalCpu += Math.Max(0, cpu);
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
            processCount);
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
