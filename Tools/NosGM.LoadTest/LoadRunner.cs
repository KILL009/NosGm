using System.Globalization;
using System.Text;
using System.Text.Json;

namespace NosGM.LoadTest;

internal sealed class LoadRunner : IAsyncDisposable
{
    private readonly LoadTestOptions _options;
    private readonly IReadOnlyList<LoadAccount> _accounts;
    private readonly List<LoadClient> _clients = [];
    private readonly ProcessSampler _processSampler;
    private readonly List<StageResult> _stageResults = [];

    public LoadRunner(LoadTestOptions options, IReadOnlyList<LoadAccount>? accounts = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _accounts = accounts ?? Array.Empty<LoadAccount>();
        _processSampler = new ProcessSampler(options.ProcessNames);

        if ((_options.Scenario == LoadScenario.Login || _options.Scenario == LoadScenario.World) &&
            _accounts.Count < _options.Stages.Max())
        {
            throw new InvalidOperationException(
                $"{_options.Scenario} scenario needs at least {_options.Stages.Max():N0} unique accounts, " +
                $"but the CSV contains only {_accounts.Count:N0}.");
        }
    }

    public IReadOnlyList<StageResult> Results => _stageResults;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_options.OutputDirectory);
        _ = _processSampler.Capture();

        Console.WriteLine("NosGM staged load test");
        Console.WriteLine($"Target   : {_options.Host}:{_options.Port}");
        if (_options.Scenario == LoadScenario.World)
        {
            Console.WriteLine($"Login    : {_options.LoginHost}:{_options.LoginPort}");
            Console.WriteLine($"Ready    : {_options.WorldReadyPacket}");
        }
        Console.WriteLine($"Scenario : {_options.Scenario}");
        Console.WriteLine($"Stages   : {string.Join(" -> ", _options.Stages)}");
        Console.WriteLine($"Ramp     : {_options.RampPerSecond:N0} clients/s");
        Console.WriteLine($"Hold     : {_options.HoldSeconds:N0}s per stage");
        Console.WriteLine($"Output   : {Path.GetFullPath(_options.OutputDirectory)}");
        Console.WriteLine();

        foreach (int target in _options.Stages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int existing = _clients.Count;
            int toCreate = target - existing;
            Console.WriteLine($"=== Stage {target:N0} clients | adding {toCreate:N0} ===");

            await RampToAsync(target, cancellationToken).ConfigureAwait(false);
            StageResult result = await HoldAndMeasureAsync(target, cancellationToken)
                .ConfigureAwait(false);
            _stageResults.Add(result);
            await WriteReportsAsync(cancellationToken).ConfigureAwait(false);

            Console.Write(
                $"Stage {target:N0}: connected={result.Connected:N0}/{result.Attempted:N0} " +
                $"failed={result.Failed:N0}");
            if (_options.Scenario is LoadScenario.Login or LoadScenario.World)
            {
                Console.Write($" login-ok={result.LoginAccepted:N0}");
            }
            if (_options.Scenario == LoadScenario.World)
            {
                Console.Write(
                    $" entry={result.WorldEntryAccepted:N0} selected={result.CharacterSelected:N0} " +
                    $"world-ready={result.WorldReady:N0} ready-p95={result.WorldReadyP95Milliseconds:N1}ms");
            }
            Console.WriteLine(
                $" connect-p95={result.ConnectP95Milliseconds:N1}ms CPU max={result.ProcessCpuMaximumPercent:N1}% " +
                $"WS max={ToMegabytes(result.ProcessWorkingSetMaximumBytes):N1}MB");
            Console.WriteLine();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (LoadClient client in _clients)
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }

        _clients.Clear();
    }

    private async Task RampToAsync(int target, CancellationToken cancellationToken)
    {
        int firstIndex = _clients.Count + 1;
        int count = target - _clients.Count;
        if (count <= 0)
        {
            return;
        }

        var tasks = new List<Task<LoadClient>>(count);
        double delayMilliseconds = 1000d / _options.RampPerSecond;
        var rampClock = System.Diagnostics.Stopwatch.StartNew();

        for (int offset = 0; offset < count; offset++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int clientIndex = firstIndex + offset;
            LoadAccount? account = _options.Scenario is LoadScenario.Login or LoadScenario.World
                ? _accounts[clientIndex - 1]
                : null;

            tasks.Add(LoadClient.ConnectAsync(
                _options,
                account,
                clientIndex,
                cancellationToken));

            double expectedElapsed = (offset + 1) * delayMilliseconds;
            double remaining = expectedElapsed - rampClock.Elapsed.TotalMilliseconds;
            if (remaining >= 1)
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(remaining),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        LoadClient[] newClients = await Task.WhenAll(tasks).ConfigureAwait(false);
        _clients.AddRange(newClients);
    }

    private async Task<StageResult> HoldAndMeasureAsync(
        int target,
        CancellationToken cancellationToken)
    {
        int sampleCount = Math.Max(1, _options.HoldSeconds);
        var processSamples = new List<ProcessSample>(sampleCount);
        DateTime startedAtUtc = DateTime.UtcNow;

        for (int second = 0; second < sampleCount; second++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProcessSample process = _processSampler.Capture();
            processSamples.Add(process);

            int connected = _clients.Count(client => client.IsConnected);
            int loginAccepted = _clients.Count(client => client.LoginAccepted);
            int worldReady = _clients.Count(client => client.WorldReady);
            long received = _clients.Sum(client => client.BytesReceived);
            long sent = _clients.Sum(client => client.BytesSent);

            Console.Write(
                $"\rhold {Math.Min(second + 1, _options.HoldSeconds),3}/{_options.HoldSeconds,3}s " +
                $"connected={connected,5} login-ok={loginAccepted,5} world-ready={worldReady,5} " +
                $"rx={FormatBytes(received),10} tx={FormatBytes(sent),10} " +
                $"CPU={process.CpuPercent,6:N1}% WS={ToMegabytes(process.WorkingSetBytes),8:N1}MB");

            if (_options.HoldSeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
        }

        Console.WriteLine();

        double[] connectTimes = _clients
            .Where(client => client.Failure == null)
            .Select(client => client.ConnectMilliseconds)
            .OrderBy(value => value)
            .ToArray();
        double[] loginTimes = _clients
            .Where(client => client.LoginAccepted && client.LoginMilliseconds > 0)
            .Select(client => client.LoginMilliseconds)
            .OrderBy(value => value)
            .ToArray();
        double[] worldReadyTimes = _clients
            .Where(client => client.WorldReady && client.WorldReadyMilliseconds > 0)
            .Select(client => client.WorldReadyMilliseconds)
            .OrderBy(value => value)
            .ToArray();

        int attempted = _clients.Count;
        int connectedNow = _clients.Count(client => client.IsConnected);
        int failed = _clients.Count(client => client.Failure != null);
        int loginOk = _clients.Count(client => client.LoginAccepted);
        int loginRejected = _options.Scenario is LoadScenario.Login or LoadScenario.World
            ? _clients.Count(client => !client.LoginAccepted)
            : 0;
        int worldEntry = _clients.Count(client => client.WorldEntryAccepted);
        int characterSelected = _clients.Count(client => client.CharacterSelected);
        int worldReady = _clients.Count(client => client.WorldReady);
        int worldRejected = _options.Scenario == LoadScenario.World
            ? attempted - worldReady
            : 0;

        return new StageResult
        {
            Target = target,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = DateTime.UtcNow,
            Attempted = attempted,
            Connected = connectedNow,
            Failed = failed,
            LoginAccepted = loginOk,
            LoginRejectedOrTimedOut = loginRejected,
            WorldEntryAccepted = worldEntry,
            CharacterSelected = characterSelected,
            WorldReady = worldReady,
            WorldRejectedOrTimedOut = worldRejected,
            ConnectP50Milliseconds = Percentile(connectTimes, 0.50),
            ConnectP95Milliseconds = Percentile(connectTimes, 0.95),
            ConnectP99Milliseconds = Percentile(connectTimes, 0.99),
            LoginP95Milliseconds = Percentile(loginTimes, 0.95),
            WorldReadyP50Milliseconds = Percentile(worldReadyTimes, 0.50),
            WorldReadyP95Milliseconds = Percentile(worldReadyTimes, 0.95),
            WorldReadyP99Milliseconds = Percentile(worldReadyTimes, 0.99),
            BytesReceived = _clients.Sum(client => client.BytesReceived),
            BytesSent = _clients.Sum(client => client.BytesSent),
            ProcessCpuAveragePercent = processSamples.Average(sample => sample.CpuPercent),
            ProcessCpuMaximumPercent = processSamples.Max(sample => sample.CpuPercent),
            ProcessWorkingSetMaximumBytes = processSamples.Max(sample => sample.WorkingSetBytes),
            ProcessPrivateBytesMaximum = processSamples.Max(sample => sample.PrivateBytes),
            ObservedProcessCountMaximum = processSamples.Max(sample => sample.ProcessCount)
        };
    }

    private async Task WriteReportsAsync(CancellationToken cancellationToken)
    {
        var report = new LoadTestReport
        {
            GeneratedAtUtc = DateTime.UtcNow,
            TargetHost = _options.Host,
            TargetPort = _options.Port,
            LoginHost = _options.Scenario == LoadScenario.World ? _options.LoginHost : null,
            LoginPort = _options.Scenario == LoadScenario.World ? _options.LoginPort : null,
            Scenario = _options.Scenario.ToString(),
            WorldReadyPacket = _options.Scenario == LoadScenario.World ? _options.WorldReadyPacket : null,
            RampPerSecond = _options.RampPerSecond,
            HoldSeconds = _options.HoldSeconds,
            Stages = _stageResults.ToArray()
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        string json = JsonSerializer.Serialize(report, jsonOptions);
        await File.WriteAllTextAsync(
            Path.Combine(_options.OutputDirectory, "load-test.json"),
            json,
            Encoding.UTF8,
            cancellationToken).ConfigureAwait(false);

        var csv = new StringBuilder();
        csv.AppendLine(
            "target,attempted,connected,failed,loginAccepted,loginRejectedOrTimedOut," +
            "worldEntryAccepted,characterSelected,worldReady,worldRejectedOrTimedOut," +
            "connectP50Ms,connectP95Ms,connectP99Ms,loginP95Ms," +
            "worldReadyP50Ms,worldReadyP95Ms,worldReadyP99Ms,bytesReceived,bytesSent," +
            "processCpuAvgPct,processCpuMaxPct,processWorkingSetMaxBytes,processPrivateMaxBytes,processCountMax");
        foreach (StageResult stage in _stageResults)
        {
            csv.Append(stage.Target.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.Attempted.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.Connected.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.Failed.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.LoginAccepted.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.LoginRejectedOrTimedOut.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.WorldEntryAccepted.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.CharacterSelected.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.WorldReady.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.WorldRejectedOrTimedOut.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ConnectP50Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ConnectP95Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ConnectP99Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.LoginP95Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.WorldReadyP50Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.WorldReadyP95Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.WorldReadyP99Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.BytesReceived.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.BytesSent.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ProcessCpuAveragePercent.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ProcessCpuMaximumPercent.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ProcessWorkingSetMaximumBytes.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ProcessPrivateBytesMaximum.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ObservedProcessCountMaximum.ToString(CultureInfo.InvariantCulture))
                .AppendLine();
        }

        await File.WriteAllTextAsync(
            Path.Combine(_options.OutputDirectory, "load-test.csv"),
            csv.ToString(),
            Encoding.UTF8,
            cancellationToken).ConfigureAwait(false);
    }

    private static double Percentile(double[] sortedValues, double percentile)
    {
        if (sortedValues.Length == 0)
        {
            return 0;
        }

        int index = (int)Math.Ceiling(percentile * sortedValues.Length) - 1;
        return sortedValues[Math.Clamp(index, 0, sortedValues.Length - 1)];
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB"];
        double value = Math.Max(0, bytes);
        int suffix = 0;
        while (value >= 1024 && suffix < suffixes.Length - 1)
        {
            value /= 1024;
            suffix++;
        }

        return value.ToString("N1", CultureInfo.InvariantCulture) + " " + suffixes[suffix];
    }

    private static double ToMegabytes(long bytes) => bytes / 1024d / 1024d;
}

internal sealed class StageResult
{
    public int Target { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime CompletedAtUtc { get; init; }
    public int Attempted { get; init; }
    public int Connected { get; init; }
    public int Failed { get; init; }
    public int LoginAccepted { get; init; }
    public int LoginRejectedOrTimedOut { get; init; }
    public int WorldEntryAccepted { get; init; }
    public int CharacterSelected { get; init; }
    public int WorldReady { get; init; }
    public int WorldRejectedOrTimedOut { get; init; }
    public double ConnectP50Milliseconds { get; init; }
    public double ConnectP95Milliseconds { get; init; }
    public double ConnectP99Milliseconds { get; init; }
    public double LoginP95Milliseconds { get; init; }
    public double WorldReadyP50Milliseconds { get; init; }
    public double WorldReadyP95Milliseconds { get; init; }
    public double WorldReadyP99Milliseconds { get; init; }
    public long BytesReceived { get; init; }
    public long BytesSent { get; init; }
    public double ProcessCpuAveragePercent { get; init; }
    public double ProcessCpuMaximumPercent { get; init; }
    public long ProcessWorkingSetMaximumBytes { get; init; }
    public long ProcessPrivateBytesMaximum { get; init; }
    public int ObservedProcessCountMaximum { get; init; }
}

internal sealed class LoadTestReport
{
    public DateTime GeneratedAtUtc { get; init; }
    public string TargetHost { get; init; } = string.Empty;
    public int TargetPort { get; init; }
    public string? LoginHost { get; init; }
    public int? LoginPort { get; init; }
    public string Scenario { get; init; } = string.Empty;
    public string? WorldReadyPacket { get; init; }
    public int RampPerSecond { get; init; }
    public int HoldSeconds { get; init; }
    public StageResult[] Stages { get; init; } = [];
}
