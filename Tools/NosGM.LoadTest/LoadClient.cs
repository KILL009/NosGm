using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;

namespace NosGM.LoadTest;

internal sealed class LoadClient : IAsyncDisposable
{
    private const byte LoginServerPacketTerminator = 25;
    private const int MaximumLoginResponseBytes = 65536;
    private const string MovementPercentVariable = "NOSGM_LOADTEST_MOVEMENT_PERCENT";
    private const string MovementIntervalVariable = "NOSGM_LOADTEST_MOVEMENT_INTERVAL_MS";
    private const string MovementBaseXVariable = "NOSGM_LOADTEST_MOVEMENT_BASE_X";
    private const string MovementBaseYVariable = "NOSGM_LOADTEST_MOVEMENT_BASE_Y";
    private const string MovementStepVariable = "NOSGM_LOADTEST_MOVEMENT_STEP";
    private const string MovementSpeedVariable = "NOSGM_LOADTEST_MOVEMENT_SPEED";

    private readonly TcpClient _tcpClient = new();
    private CancellationTokenSource? _receiveCancellation;
    private CancellationTokenSource? _movementCancellation;
    private Task? _receiveTask;
    private Task? _movementTask;
    private long _bytesReceived;
    private long _bytesSent;
    private long _movementPacketsSent;
    private int _nextWorldPacketId = 5;
    private int _disconnected;
    private int _disposed;

    private LoadClient(int clientIndex)
    {
        ClientIndex = clientIndex;
        _tcpClient.NoDelay = true;
    }

    public int ClientIndex { get; }
    public double ConnectMilliseconds { get; private set; }
    public double AuthBridgeMilliseconds { get; private set; }
    public double LoginMilliseconds { get; private set; }
    public double WorldReadyMilliseconds { get; private set; }
    public string? Failure { get; private set; }
    public string? LoginResponse { get; private set; }
    public bool LoginResponseTimedOut { get; private set; }
    public bool LoginResponseComplete { get; private set; }
    public int LoginResponseBytes { get; private set; }
    public bool AuthTicketIssued { get; private set; }
    public bool LoginAccepted { get; private set; }
    public bool WorldEntryAccepted { get; private set; }
    public bool CharacterSelected { get; private set; }
    public bool WorldReady { get; private set; }
    public bool ActiveMovementEnabled { get; private set; }
    public int WorldSessionId { get; private set; }
    public string? AdvertisedWorldHost { get; private set; }
    public int AdvertisedWorldPort { get; private set; }
    public long BytesReceived => Interlocked.Read(ref _bytesReceived);
    public long BytesSent => Interlocked.Read(ref _bytesSent);
    public long MovementPacketsSent => Interlocked.Read(ref _movementPacketsSent);
    public bool IsConnected =>
        Volatile.Read(ref _disposed) == 0 &&
        Volatile.Read(ref _disconnected) == 0 &&
        Failure == null &&
        _tcpClient.Connected;

    public static async Task<LoadClient> ConnectAsync(
        LoadTestOptions options,
        LoadAccount? account,
        int clientIndex,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        var client = new LoadClient(clientIndex);
        try
        {
            if (options.Scenario == LoadScenario.World)
            {
                if (account == null)
                {
                    throw new InvalidOperationException("A World client requires an account.");
                }

                await client
                    .ConnectWorldAsync(options, account, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await client
                    .ConnectSingleEndpointAsync(options, account, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            client.Failure = "timeout";
            await client.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is SocketException or IOException or InvalidOperationException or HttpRequestException)
        {
            client.Failure = exception.GetType().Name + ": " + exception.Message;
            await client.DisposeAsync().ConfigureAwait(false);
        }

        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Interlocked.Exchange(ref _disconnected, 1);

        if (_movementCancellation != null)
        {
            await _movementCancellation.CancelAsync().ConfigureAwait(false);
        }
        if (_receiveCancellation != null)
        {
            await _receiveCancellation.CancelAsync().ConfigureAwait(false);
        }

        if (_movementTask != null)
        {
            try
            {
                await _movementTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        try
        {
            _tcpClient.Close();
        }
        catch (SocketException)
        {
        }

        if (_receiveTask != null)
        {
            try
            {
                await _receiveTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (IOException)
            {
            }
            catch (SocketException)
            {
            }
        }

        _movementCancellation?.Dispose();
        _receiveCancellation?.Dispose();
        _tcpClient.Dispose();
    }

    private async Task ConnectSingleEndpointAsync(
        LoadTestOptions options,
        LoadAccount? account,
        CancellationToken cancellationToken)
    {
        string? loginPacket = null;
        if (options.Scenario == LoadScenario.Login)
        {
            if (account == null)
            {
                throw new InvalidOperationException("A login client requires an account.");
            }

            loginPacket = await PrepareLoginPacketAsync(options, account, cancellationToken)
                .ConfigureAwait(false);
        }

        var stopwatch = Stopwatch.StartNew();
        await ConnectWithTimeoutAsync(
                _tcpClient,
                options.Host,
                options.Port,
                options.ConnectTimeoutMilliseconds,
                cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();
        ConnectMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

        NetworkStream stream = _tcpClient.GetStream();
        if (options.Scenario == LoadScenario.Login)
        {
            var loginClock = Stopwatch.StartNew();
            await WriteAsync(
                    stream,
                    NosTaleLoginCodec.EncodeClientPacket(loginPacket!),
                    cancellationToken)
                .ConfigureAwait(false);

            string? response = await ReadLoginResponseAsync(
                    stream,
                    options.ReadTimeoutMilliseconds,
                    cancellationToken)
                .ConfigureAwait(false);
            loginClock.Stop();
            LoginMilliseconds = loginClock.Elapsed.TotalMilliseconds;
            LoginResponse = response ?? "<login-response-timeout>";

            if (response == null || LoginResponseTimedOut || !NosTaleLoginCodec.LooksAccepted(response))
            {
                throw new InvalidOperationException(
                    DescribeLoginFailure(response, options.ReadTimeoutMilliseconds));
            }

            LoginAccepted = true;
        }

        StartDrain(stream, cancellationToken);
    }

    private async Task ConnectWorldAsync(
        LoadTestOptions options,
        LoadAccount account,
        CancellationToken cancellationToken)
    {
        await PerformLoginForWorldAsync(options, account, cancellationToken).ConfigureAwait(false);

        var worldClock = Stopwatch.StartNew();
        var connectClock = Stopwatch.StartNew();
        await ConnectWithTimeoutAsync(
                _tcpClient,
                options.Host,
                options.Port,
                options.ConnectTimeoutMilliseconds,
                cancellationToken)
            .ConfigureAwait(false);
        connectClock.Stop();
        ConnectMilliseconds = connectClock.Elapsed.TotalMilliseconds;

        NetworkStream stream = _tcpClient.GetStream();
        var reader = new NosTaleWorldPacketReader();

        await WriteAsync(
                stream,
                NosTaleWorldCodec.EncodeInitialHandshake(WorldSessionId),
                cancellationToken)
            .ConfigureAwait(false);
        await WriteWorldPacketAsync(
                stream,
                WorldSessionId,
                $"1 {account.Username}",
                cancellationToken)
            .ConfigureAwait(false);

        string worldCredential = options.LoginMode == LoginMode.Modern
            ? "thisisgfmode"
            : account.Password;
        await WriteWorldPacketAsync(
                stream,
                WorldSessionId,
                $"2 0 0 {worldCredential}",
                cancellationToken)
            .ConfigureAwait(false);

        bool receivedCharacterList = await ReadWorldUntilAsync(
                stream,
                reader,
                packet => HasPacketHeader(packet, "clist_end"),
                options.ReadTimeoutMilliseconds,
                cancellationToken)
            .ConfigureAwait(false);
        if (!receivedCharacterList)
        {
            throw new InvalidOperationException("World entry did not reach clist_end before timeout.");
        }
        WorldEntryAccepted = true;

        await WriteWorldPacketAsync(
                stream,
                WorldSessionId,
                $"3 select {account.Slot}",
                cancellationToken)
            .ConfigureAwait(false);
        bool selected = await ReadWorldUntilAsync(
                stream,
                reader,
                packet => string.Equals(packet.Trim(), "OK", StringComparison.OrdinalIgnoreCase),
                options.ReadTimeoutMilliseconds,
                cancellationToken)
            .ConfigureAwait(false);
        if (!selected)
        {
            throw new InvalidOperationException("Character selection did not return OK before timeout.");
        }
        CharacterSelected = true;

        await WriteWorldPacketAsync(
                stream,
                WorldSessionId,
                "4 game_start",
                cancellationToken)
            .ConfigureAwait(false);
        bool ready = await ReadWorldUntilAsync(
                stream,
                reader,
                packet => HasPacketHeader(packet, options.WorldReadyPacket),
                options.ReadTimeoutMilliseconds,
                cancellationToken)
            .ConfigureAwait(false);
        worldClock.Stop();
        WorldReadyMilliseconds = worldClock.Elapsed.TotalMilliseconds;
        if (!ready)
        {
            throw new InvalidOperationException(
                $"World game_start did not reach '{options.WorldReadyPacket}' before timeout.");
        }

        WorldReady = true;
        StartDrain(stream, cancellationToken);
        StartMovementIfEnabled(stream, cancellationToken);
    }

    private async Task PerformLoginForWorldAsync(
        LoadTestOptions options,
        LoadAccount account,
        CancellationToken cancellationToken)
    {
        string packet = await PrepareLoginPacketAsync(options, account, cancellationToken)
            .ConfigureAwait(false);

        using var loginClient = new TcpClient
        {
            NoDelay = true
        };

        await ConnectWithTimeoutAsync(
                loginClient,
                options.LoginHost,
                options.LoginPort,
                options.ConnectTimeoutMilliseconds,
                cancellationToken)
            .ConfigureAwait(false);

        NetworkStream stream = loginClient.GetStream();
        var loginClock = Stopwatch.StartNew();
        await WriteAsync(
                stream,
                NosTaleLoginCodec.EncodeClientPacket(packet),
                cancellationToken)
            .ConfigureAwait(false);

        string? response = await ReadLoginResponseAsync(
                stream,
                options.ReadTimeoutMilliseconds,
                cancellationToken)
            .ConfigureAwait(false);
        loginClock.Stop();
        LoginMilliseconds = loginClock.Elapsed.TotalMilliseconds;
        LoginResponse = response ?? "<login-response-timeout>";

        if (response == null || LoginResponseTimedOut || !NosTaleLoginCodec.LooksAccepted(response))
        {
            throw new InvalidOperationException(
                DescribeLoginFailure(response, options.ReadTimeoutMilliseconds));
        }

        if (!NosTaleLoginCodec.TryParseWorldTicket(
                response,
                out int sessionId,
                out string? advertisedHost,
                out int advertisedPort))
        {
            throw new InvalidOperationException(
                $"Login returned an incomplete or malformed NsTeST response " +
                $"(bytes={LoginResponseBytes}, complete={LoginResponseComplete}).");
        }

        LoginAccepted = true;
        WorldSessionId = sessionId;
        AdvertisedWorldHost = advertisedHost;
        AdvertisedWorldPort = advertisedPort;
    }

    private async Task<string> PrepareLoginPacketAsync(
        LoadTestOptions options,
        LoadAccount account,
        CancellationToken cancellationToken)
    {
        if (options.LoginMode == LoginMode.Legacy)
        {
            return NosTaleLoginCodec.BuildLegacyPacket(options, account, ClientIndex);
        }

        Guid installationId = Guid.NewGuid();
        var authClock = Stopwatch.StartNew();
        ModernLoginTicket ticket = await ModernLoginTicketClient
            .IssueAsync(options, account, installationId, cancellationToken)
            .ConfigureAwait(false);
        authClock.Stop();
        AuthBridgeMilliseconds = authClock.Elapsed.TotalMilliseconds;
        AuthTicketIssued = true;
        return NosTaleLoginCodec.BuildModernPacket(options, ticket, installationId);
    }

    private async Task<string?> ReadLoginResponseAsync(
        NetworkStream stream,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];
        using var response = new MemoryStream();
        using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readCancellation.CancelAfter(timeoutMilliseconds);

        LoginResponseTimedOut = false;
        LoginResponseComplete = false;
        LoginResponseBytes = 0;

        try
        {
            while (!readCancellation.IsCancellationRequested)
            {
                int received = await stream
                    .ReadAsync(buffer.AsMemory(), readCancellation.Token)
                    .ConfigureAwait(false);
                if (received <= 0)
                {
                    Interlocked.Exchange(ref _disconnected, 1);
                    return response.Length == 0
                        ? null
                        : NosTaleLoginCodec.DecodeServerPacket(response.ToArray());
                }

                Interlocked.Add(ref _bytesReceived, received);
                int terminatorIndex = Array.IndexOf(
                    buffer,
                    LoginServerPacketTerminator,
                    0,
                    received);
                int bytesToAppend = terminatorIndex >= 0
                    ? terminatorIndex + 1
                    : received;
                response.Write(buffer, 0, bytesToAppend);
                LoginResponseBytes += bytesToAppend;

                if (response.Length > MaximumLoginResponseBytes)
                {
                    throw new InvalidOperationException(
                        $"Login response exceeded {MaximumLoginResponseBytes} bytes without a packet terminator.");
                }

                if (terminatorIndex >= 0)
                {
                    LoginResponseComplete = true;
                    return NosTaleLoginCodec.DecodeServerPacket(response.ToArray());
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LoginResponseTimedOut = true;
            return response.Length == 0
                ? null
                : NosTaleLoginCodec.DecodeServerPacket(response.ToArray());
        }

        return response.Length == 0
            ? null
            : NosTaleLoginCodec.DecodeServerPacket(response.ToArray());
    }

    private string DescribeLoginFailure(string? response, int timeoutMilliseconds)
    {
        if (LoginResponseTimedOut)
        {
            return $"Login response timed out after {timeoutMilliseconds} ms " +
                   $"(bytes={LoginResponseBytes}, complete={LoginResponseComplete}).";
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            return "Login closed the connection without a response.";
        }

        string value = response.Trim();
        string[] tokens = value.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length >= 2 &&
            string.Equals(tokens[0], "failc", StringComparison.OrdinalIgnoreCase) &&
            byte.TryParse(tokens[1], out byte code))
        {
            return $"Login rejected the request: failc {code} ({DescribeLoginFailCode(code)}).";
        }

        string header = tokens.Length == 0 ? "<empty>" : SanitizeHeader(tokens[0]);
        return $"Login returned an unexpected response " +
               $"(header={header}, bytes={LoginResponseBytes}, complete={LoginResponseComplete}).";
    }

    private static string DescribeLoginFailCode(byte code) => code switch
    {
        1 => "OldClient",
        2 => "UnhandledError",
        3 => "Maintenance",
        4 => "AlreadyConnected",
        5 => "AccountOrPasswordWrong",
        6 => "CantConnect",
        7 => "Banned",
        8 => "WrongCountry",
        9 => "WrongCaps",
        _ => "Unknown"
    };

    private static string SanitizeHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return "<empty>";
        }

        string candidate = header.Length <= 24 ? header : header[..24];
        foreach (char value in candidate)
        {
            if (!char.IsLetterOrDigit(value) && value is not '_' and not '-' and not '$')
            {
                return "<invalid>";
            }
        }

        return candidate;
    }

    private async Task<bool> ReadWorldUntilAsync(
        NetworkStream stream,
        NosTaleWorldPacketReader reader,
        Func<string, bool> predicate,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        foreach (string bufferedPacket in reader.DrainPackets())
        {
            if (predicate(bufferedPacket))
            {
                return true;
            }
        }

        byte[] buffer = new byte[16384];
        using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readCancellation.CancelAfter(timeoutMilliseconds);

        try
        {
            while (!readCancellation.IsCancellationRequested)
            {
                int received = await stream
                    .ReadAsync(buffer.AsMemory(), readCancellation.Token)
                    .ConfigureAwait(false);
                if (received <= 0)
                {
                    Interlocked.Exchange(ref _disconnected, 1);
                    return false;
                }

                Interlocked.Add(ref _bytesReceived, received);
                reader.Append(buffer.AsSpan(0, received));
                foreach (string packet in reader.DrainPackets())
                {
                    if (predicate(packet))
                    {
                        return true;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        return false;
    }

    private void StartMovementIfEnabled(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        int percent = ReadMovementSetting(MovementPercentVariable, 0, 0, 100);
        if (percent <= 0 || (ClientIndex - 1) % 100 >= percent)
        {
            return;
        }

        int intervalMilliseconds = ReadMovementSetting(
            MovementIntervalVariable,
            1000,
            100,
            60000);
        int baseX = ReadMovementSetting(MovementBaseXVariable, 80, 0, short.MaxValue);
        int baseY = ReadMovementSetting(MovementBaseYVariable, 115, 0, short.MaxValue);
        int step = ReadMovementSetting(MovementStepVariable, 1, 1, 16);
        int speed = ReadMovementSetting(MovementSpeedVariable, 11, 1, short.MaxValue);
        if (baseX + step > short.MaxValue)
        {
            throw new InvalidOperationException(
                $"{MovementBaseXVariable} + {MovementStepVariable} must not exceed {short.MaxValue}.");
        }

        ActiveMovementEnabled = true;
        _movementCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _movementTask = MovementLoopAsync(
            stream,
            intervalMilliseconds,
            (short)baseX,
            (short)baseY,
            (short)step,
            (short)speed,
            _movementCancellation.Token);
    }

    private async Task MovementLoopAsync(
        NetworkStream stream,
        int intervalMilliseconds,
        short baseX,
        short baseY,
        short step,
        short speed,
        CancellationToken cancellationToken)
    {
        bool useOffset = (ClientIndex & 1) == 0;
        try
        {
            await Task.Delay(intervalMilliseconds, cancellationToken).ConfigureAwait(false);
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                short targetX = useOffset
                    ? checked((short)(baseX + step))
                    : baseX;
                useOffset = !useOffset;

                int packetId = _nextWorldPacketId;
                _nextWorldPacketId = packetId >= ushort.MaxValue ? 0 : packetId + 1;
                await WriteWorldPacketAsync(
                        stream,
                        WorldSessionId,
                        $"{packetId} walk {targetX} {baseY} 0 {speed}",
                        cancellationToken)
                    .ConfigureAwait(false);
                Interlocked.Increment(ref _movementPacketsSent);

                await Task.Delay(intervalMilliseconds, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (
            exception is IOException or SocketException or InvalidOperationException)
        {
            Interlocked.Exchange(ref _disconnected, 1);
            Failure ??= "active-movement " + exception.GetType().Name + ": " + exception.Message;
        }
    }

    private static int ReadMovementSetting(
        string variableName,
        int fallback,
        int minimum,
        int maximum)
    {
        string? raw = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (!int.TryParse(raw, out int parsed) || parsed < minimum || parsed > maximum)
        {
            throw new InvalidOperationException(
                $"{variableName} must be an integer between {minimum} and {maximum}.");
        }

        return parsed;
    }

    private async Task WriteWorldPacketAsync(
        NetworkStream stream,
        int sessionId,
        string packet,
        CancellationToken cancellationToken)
    {
        await WriteAsync(
                stream,
                NosTaleWorldCodec.EncodeClientPacket(sessionId, packet),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task WriteAsync(
        NetworkStream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        await stream.WriteAsync(payload.AsMemory(), cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        Interlocked.Add(ref _bytesSent, payload.Length);
    }

    private static async Task ConnectWithTimeoutAsync(
        TcpClient client,
        string host,
        int port,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        using var connectCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connectCancellation.CancelAfter(timeoutMilliseconds);
        await client
            .ConnectAsync(host, port, connectCancellation.Token)
            .ConfigureAwait(false);
    }

    private static bool HasPacketHeader(string packet, string header)
    {
        string value = packet.Trim();
        return string.Equals(value, header, StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith(header + " ", StringComparison.OrdinalIgnoreCase);
    }

    private void StartDrain(NetworkStream stream, CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disconnected) != 0)
        {
            return;
        }

        _receiveCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiveTask = DrainAsync(stream, _receiveCancellation.Token);
    }

    private async Task DrainAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[8192];
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int received = await stream
                    .ReadAsync(buffer.AsMemory(), cancellationToken)
                    .ConfigureAwait(false);
                if (received <= 0)
                {
                    Interlocked.Exchange(ref _disconnected, 1);
                    return;
                }

                Interlocked.Add(ref _bytesReceived, received);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException)
        {
            Interlocked.Exchange(ref _disconnected, 1);
        }
        catch (SocketException)
        {
            Interlocked.Exchange(ref _disconnected, 1);
        }
    }
}
