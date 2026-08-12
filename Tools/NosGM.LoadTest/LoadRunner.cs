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
    private readonly List<StageTelemetrySample> _telemetrySamples = [];

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
        if (_options.Scenario is LoadScenario.Login or LoadScenario.World)
        {
            Console.WriteLine($"LoginMode: {_options.LoginMode}");
        }
        if (_options.Scenario == LoadScenario.World)
        {
            Console.WriteLine($"Login    : {_options.LoginHost}:{_options.LoginPort}");
            Console.WriteLine($"Ready    : {_options.WorldReadyPacket}");
        }
        if (_options.Scenario is LoadScenario.Login or LoadScenario.World &&
            _options.LoginMode == LoginMode.Modern)
        {
            Console.WriteLine(
                $"Modern   : {_options.ModernLoginHeader} via " +
                $"{_options.AuthBridgeUri.Scheme}://{_options.AuthBridgeUri.Host}:{_options.AuthBridgeUri.Port}");
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
                if (_options.LoginMode == LoginMode.Modern)
                {
                    Console.Write($" auth-ok={result.AuthTicketIssued:N0}");
                }
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
                $"WS max={ToMegabytes(result.ProcessWorkingSetMaximumBytes):N1}MB " +
                $"rx/s avg={FormatBytes((long)result.ReceiveBytesPerSecondAverage)} " +
                $"tx/s avg={FormatBytes((long)result.SendBytesPerSecondAverage)} " +
                $"move/s avg={result.MovementPacketsPerSecondAverage:N1}");

            foreach (ProcessSummary process in result.Processes)
            {
                Console.WriteLine(
                    $"  [PROC] {process.ProcessName,-28} CPU avg/max={process.CpuAveragePercent,5:N1}/{process.CpuMaximumPercent,5:N1}% " +
                    $"WS max={ToMegabytes(process.WorkingSetMaximumBytes),8:N1}MB " +
                    $"Private max={ToMegabytes(process.PrivateBytesMaximum),8:N1}MB " +
                    $"threads max={process.ThreadCountMaximum,4} handles max={process.HandleCountMaximum,5}");
            }

            foreach (string failure in result.FailureSamples)
            {
                Console.WriteLine($"  [FAIL] {failure}");
            }
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
        var stageTelemetry = new List<StageTelemetrySample>(sampleCount);
        DateTime startedAtUtc = DateTime.UtcNow;
        DateTime previousCapturedAtUtc = startedAtUtc;
        long previousReceived = _clients.Sum(client => client.BytesReceived);
        long previousSent = _clients.Sum(client => client.BytesSent);
        long previousMovement = _clients.Sum(client => client.MovementPacketsSent);
        bool hasPreviousRateSample = false;

        for (int second = 0; second < sampleCount; second++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProcessSample process = _processSampler.Capture();
            processSamples.Add(process);

            int connected = _clients.Count(client => client.IsConnected);
            int disconnected = _clients.Count - connected;
            int authIssued = _clients.Count(client => client.AuthTicketIssued);
            int loginAccepted = _clients.Count(client => client.LoginAccepted);
            int worldReadyNow = _clients.Count(client => client.WorldReady);
            long received = _clients.Sum(client => client.BytesReceived);
            long sent = _clients.Sum(client => client.BytesSent);
            long movement = _clients.Sum(client => client.MovementPacketsSent);

            double elapsedSeconds = Math.Max(
                0.001d,
                (process.CapturedAtUtc - previousCapturedAtUtc).TotalSeconds);
            double receiveRate = hasPreviousRateSample
                ? Math.Max(0, received - previousReceived) / elapsedSeconds
                : 0;
            double sendRate = hasPreviousRateSample
                ? Math.Max(0, sent - previousSent) / elapsedSeconds
                : 0;
            double movementRate = hasPreviousRateSample
                ? Math.Max(0, movement - previousMovement) / elapsedSeconds
                : 0;

            var telemetry = new StageTelemetrySample
            {
                Target = target,
                Second = second + 1,
                CapturedAtUtc = process.CapturedAtUtc,
                Connected = connected,
                Disconnected = disconnected,
                AuthTicketIssued = authIssued,
                LoginAccepted = loginAccepted,
                WorldReady = worldReadyNow,
                BytesReceived = received,
                BytesSent = sent,
                MovementPacketsSent = movement,
                ReceiveBytesPerSecond = receiveRate,
                SendBytesPerSecond = sendRate,
                MovementPacketsPerSecond = movementRate,
                ProcessCpuPercent = process.CpuPercent,
                ProcessWorkingSetBytes = process.WorkingSetBytes,
                ProcessPrivateBytes = process.PrivateBytes,
                ProcessThreadCount = process.ThreadCount,
                ProcessHandleCount = process.HandleCount,
                ObservedProcessCount = process.ProcessCount,
                Processes = process.Processes
            };
            stageTelemetry.Add(telemetry);
            _telemetrySamples.Add(telemetry);

            Console.Write(
                $"\rhold {Math.Min(second + 1, _options.HoldSeconds),3}/{_options.HoldSeconds,3}s " +
                $"connected={connected,5} auth-ok={authIssued,5} login-ok={loginAccepted,5} " +
                $"world-ready={worldReadyNow,5} rx/s={FormatBytes((long)receiveRate),10} " +
                $"tx/s={FormatBytes((long)sendRate),10} move/s={movementRate,8:N1} " +
                $"CPU={process.CpuPercent,6:N1}% WS={ToMegabytes(process.WorkingSetBytes),8:N1}MB");

            previousCapturedAtUtc = process.CapturedAtUtc;
            previousReceived = received;
            previousSent = sent;
            previousMovement = movement;
            hasPreviousRateSample = true;

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
        double[] authBridgeTimes = _clients
            .Where(client => client.AuthTicketIssued && client.AuthBridgeMilliseconds > 0)
            .Select(client => client.AuthBridgeMilliseconds)
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
        int authTicketIssued = _clients.Count(client => client.AuthTicketIssued);
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
        string[] failureSamples = _clients
            .Where(client => !string.IsNullOrWhiteSpace(client.Failure))
            .Select(client => client.Failure!)
            .Distinct(StringComparer.Ordinal)
            .Take(5)
            .ToArray();

        return new StageResult
        {
            Target = target,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = DateTime.UtcNow,
            Attempted = attempted,
            Connected = connectedNow,
            Failed = failed,
            AuthTicketIssued = authTicketIssued,
            LoginAccepted = loginOk,
            LoginRejectedOrTimedOut = loginRejected,
            WorldEntryAccepted = worldEntry,
            CharacterSelected = characterSelected,
            WorldReady = worldReady,
            WorldRejectedOrTimedOut = worldRejected,
            ConnectP50Milliseconds = Percentile(connectTimes, 0.50),
            ConnectP95Milliseconds = Percentile(connectTimes, 0.95),
            ConnectP99Milliseconds = Percentile(connectTimes, 0.99),
            AuthBridgeP95Milliseconds = Percentile(authBridgeTimes, 0.95),
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
            ProcessThreadCountMaximum = processSamples.Max(sample => sample.ThreadCount),
            ProcessHandleCountMaximum = processSamples.Max(sample => sample.HandleCount),
            ObservedProcessCountMaximum = processSamples.Max(sample => sample.ProcessCount),
            ReceiveBytesPerSecondAverage = stageTelemetry.Average(sample => sample.ReceiveBytesPerSecond),
            ReceiveBytesPerSecondMaximum = stageTelemetry.Max(sample => sample.ReceiveBytesPerSecond),
            SendBytesPerSecondAverage = stageTelemetry.Average(sample => sample.SendBytesPerSecond),
            SendBytesPerSecondMaximum = stageTelemetry.Max(sample => sample.SendBytesPerSecond),
            MovementPacketsPerSecondAverage = stageTelemetry.Average(sample => sample.MovementPacketsPerSecond),
            MovementPacketsPerSecondMaximum = stageTelemetry.Max(sample => sample.MovementPacketsPerSecond),
            Processes = BuildProcessSummaries(processSamples),
            FailureSamples = failureSamples
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
            LoginMode = _options.Scenario is LoadScenario.Login or LoadScenario.World
                ? _options.LoginMode.ToString()
                : null,
            ModernLoginHeader = _options.LoginMode == LoginMode.Modern &&
                                _options.Scenario is LoadScenario.Login or LoadScenario.World
                ? _options.ModernLoginHeader
                : null,
            WorldReadyPacket = _options.Scenario == LoadScenario.World ? _options.WorldReadyPacket : null,
            RampPerSecond = _options.RampPerSecond,
            HoldSeconds = _options.HoldSeconds,
            Stages = _stageResults.ToArray(),
            Telemetry = _telemetrySamples.ToArray()
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

        await WriteStageSummaryCsvAsync(cancellationToken).ConfigureAwait(false);
        await WriteTelemetryCsvAsync(cancellationToken).ConfigureAwait(false);
        await WriteProcessTelemetryCsvAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteStageSummaryCsvAsync(CancellationToken cancellationToken)
    {
        var csv = new StringBuilder();
        csv.AppendLine(
            "target,attempted,connected,failed,authTicketIssued,loginAccepted,loginRejectedOrTimedOut," +
            "worldEntryAccepted,characterSelected,worldReady,worldRejectedOrTimedOut," +
            "connectP50Ms,connectP95Ms,connectP99Ms,authBridgeP95Ms,loginP95Ms," +
            "worldReadyP50Ms,worldReadyP95Ms,worldReadyP99Ms,bytesReceived,bytesSent," +
            "processCpuAvgPct,processCpuMaxPct,processWorkingSetMaxBytes,processPrivateMaxBytes," +
            "processThreadCountMax,processHandleCountMax,processCountMax," +
            "rxBytesPerSecondAvg,rxBytesPerSecondMax,txBytesPerSecondAvg,txBytesPerSecondMax," +
            "movementPacketsPerSecondAvg,movementPacketsPerSecondMax");
        foreach (StageResult stage in _stageResults)
        {
            csv.Append(stage.Target.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.Attempted.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.Connected.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.Failed.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.AuthTicketIssued.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.LoginAccepted.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.LoginRejectedOrTimedOut.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.WorldEntryAccepted.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.CharacterSelected.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.WorldReady.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.WorldRejectedOrTimedOut.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ConnectP50Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ConnectP95Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ConnectP99Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.AuthBridgeP95Milliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
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
                .Append(stage.ProcessThreadCountMaximum.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ProcessHandleCountMaximum.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ObservedProcessCountMaximum.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ReceiveBytesPerSecondAverage.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.ReceiveBytesPerSecondMaximum.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.SendBytesPerSecondAverage.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.SendBytesPerSecondMaximum.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.MovementPacketsPerSecondAverage.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(stage.MovementPacketsPerSecondMaximum.ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine();
        }

        await File.WriteAllTextAsync(
            Path.Combine(_options.OutputDirectory, "load-test.csv"),
            csv.ToString(),
            Encoding.UTF8,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteTelemetryCsvAsync(CancellationToken cancellationToken)
    {
        var csv = new StringBuilder();
        csv.AppendLine(
            "target,second,capturedAtUtc,connected,disconnected,authTicketIssued,loginAccepted,worldReady," +
            "bytesReceived,bytesSent,movementPacketsSent,rxBytesPerSecond,txBytesPerSecond," +
            "movementPacketsPerSecond,processCpuPct,processWorkingSetBytes,processPrivateBytes," +
            "processThreadCount,processHandleCount,observedProcessCount");

        foreach (StageTelemetrySample sample in _telemetrySamples)
        {
            csv.Append(sample.Target.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.Second.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.Connected.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.Disconnected.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.AuthTicketIssued.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.LoginAccepted.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.WorldReady.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.BytesReceived.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.BytesSent.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.MovementPacketsSent.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.ReceiveBytesPerSecond.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.SendBytesPerSecond.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.MovementPacketsPerSecond.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.ProcessCpuPercent.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.ProcessWorkingSetBytes.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.ProcessPrivateBytes.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.ProcessThreadCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.ProcessHandleCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(sample.ObservedProcessCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine();
        }

        await File.WriteAllTextAsync(
            Path.Combine(_options.OutputDirectory, "load-test-telemetry.csv"),
            csv.ToString(),
            Encoding.UTF8,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteProcessTelemetryCsvAsync(CancellationToken cancellationToken)
    {
        var csv = new StringBuilder();
        csv.AppendLine(
            "target,second,capturedAtUtc,processName,cpuPct,workingSetBytes,privateBytes," +
            "threadCount,handleCount,processCount");

        foreach (StageTelemetrySample sample in _telemetrySamples)
        {
            foreach (ProcessMetricSample process in sample.Processes)
            {
                csv.Append(sample.Target.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(sample.Second.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(sample.CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture)).Append(',')
                    .Append(process.ProcessName).Append(',')
                    .Append(process.CpuPercent.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(process.WorkingSetBytes.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(process.PrivateBytes.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(process.ThreadCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(process.HandleCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(process.ProcessCount.ToString(CultureInfo.InvariantCulture))
                    .AppendLine();
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(_options.OutputDirectory, "load-test-processes.csv"),
            csv.ToString(),
            Encoding.UTF8,
            cancellationToken).ConfigureAwait(false);
    }

    private static ProcessSummary[] BuildProcessSummaries(IEnumerable<ProcessSample> samples)
    {
        return samples
            .SelectMany(sample => sample.Processes)
            .GroupBy(sample => sample.ProcessName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProcessSummary
            {
                ProcessName = group.Key,
                CpuAveragePercent = group.Average(sample => sample.CpuPercent),
                CpuMaximumPercent = group.Max(sample => sample.CpuPercent),
                WorkingSetMaximumBytes = group.Max(sample => sample.WorkingSetBytes),
                PrivateBytesMaximum = group.Max(sample => sample.PrivateBytes),
                ThreadCountMaximum = group.Max(sample => sample.ThreadCount),
                HandleCountMaximum = group.Max(sample => sample.HandleCount),
                ProcessCountMaximum = group.Max(sample => sample.ProcessCount)
            })
            .OrderBy(summary => summary.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
    public int AuthTicketIssued { get; init; }
    public int LoginAccepted { get; init; }
    public int LoginRejectedOrTimedOut { get; init; }
    public int WorldEntryAccepted { get; init; }
    public int CharacterSelected { get; init; }
    public int WorldReady { get; init; }
    public int WorldRejectedOrTimedOut { get; init; }
    public double ConnectP50Milliseconds { get; init; }
    public double ConnectP95Milliseconds { get; init; }
    public double ConnectP99Milliseconds { get; init; }
    public double AuthBridgeP95Milliseconds { get; init; }
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
    public int ProcessThreadCountMaximum { get; init; }
    public int ProcessHandleCountMaximum { get; init; }
    public int ObservedProcessCountMaximum { get; init; }
    public double ReceiveBytesPerSecondAverage { get; init; }
    public double ReceiveBytesPerSecondMaximum { get; init; }
    public double SendBytesPerSecondAverage { get; init; }
    public double SendBytesPerSecondMaximum { get; init; }
    public double MovementPacketsPerSecondAverage { get; init; }
    public double MovementPacketsPerSecondMaximum { get; init; }
    public ProcessSummary[] Processes { get; init; } = [];
    public string[] FailureSamples { get; init; } = [];
}

internal sealed class ProcessSummary
{
    public string ProcessName { get; init; } = string.Empty;
    public double CpuAveragePercent { get; init; }
    public double CpuMaximumPercent { get; init; }
    public long WorkingSetMaximumBytes { get; init; }
    public long PrivateBytesMaximum { get; init; }
    public int ThreadCountMaximum { get; init; }
    public int HandleCountMaximum { get; init; }
    public int ProcessCountMaximum { get; init; }
}

internal sealed class StageTelemetrySample
{
    public int Target { get; init; }
    public int Second { get; init; }
    public DateTime CapturedAtUtc { get; init; }
    public int Connected { get; init; }
    public int Disconnected { get; init; }
    public int AuthTicketIssued { get; init; }
    public int LoginAccepted { get; init; }
    public int WorldReady { get; init; }
    public long BytesReceived { get; init; }
    public long BytesSent { get; init; }
    public long MovementPacketsSent { get; init; }
    public double ReceiveBytesPerSecond { get; init; }
    public double SendBytesPerSecond { get; init; }
    public double MovementPacketsPerSecond { get; init; }
    public double ProcessCpuPercent { get; init; }
    public long ProcessWorkingSetBytes { get; init; }
    public long ProcessPrivateBytes { get; init; }
    public int ProcessThreadCount { get; init; }
    public int ProcessHandleCount { get; init; }
    public int ObservedProcessCount { get; init; }
    public ProcessMetricSample[] Processes { get; init; } = [];
}

internal sealed class LoadTestReport
{
    public DateTime GeneratedAtUtc { get; init; }
    public string TargetHost { get; init; } = string.Empty;
    public int TargetPort { get; init; }
    public string? LoginHost { get; init; }
    public int? LoginPort { get; init; }
    public string Scenario { get; init; } = string.Empty;
    public string? LoginMode { get; init; }
    public string? ModernLoginHeader { get; init; }
    public string? WorldReadyPacket { get; init; }
    public int RampPerSecond { get; init; }
    public int HoldSeconds { get; init; }
    public StageResult[] Stages { get; init; } = [];
    public StageTelemetrySample[] Telemetry { get; init; } = [];
}
