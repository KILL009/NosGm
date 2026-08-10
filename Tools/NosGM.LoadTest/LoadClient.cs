using System.Diagnostics;
using System.Net.Sockets;

namespace NosGM.LoadTest;

internal sealed class LoadClient : IAsyncDisposable
{
    private readonly TcpClient _tcpClient = new();
    private CancellationTokenSource? _receiveCancellation;
    private Task? _receiveTask;
    private long _bytesReceived;
    private long _bytesSent;
    private int _disconnected;
    private int _disposed;

    private LoadClient(int clientIndex)
    {
        ClientIndex = clientIndex;
        _tcpClient.NoDelay = true;
    }

    public int ClientIndex { get; }
    public double ConnectMilliseconds { get; private set; }
    public double LoginMilliseconds { get; private set; }
    public double WorldReadyMilliseconds { get; private set; }
    public string? Failure { get; private set; }
    public string? LoginResponse { get; private set; }
    public bool LoginAccepted { get; private set; }
    public bool WorldEntryAccepted { get; private set; }
    public bool CharacterSelected { get; private set; }
    public bool WorldReady { get; private set; }
    public int WorldSessionId { get; private set; }
    public string? AdvertisedWorldHost { get; private set; }
    public int AdvertisedWorldPort { get; private set; }
    public long BytesReceived => Interlocked.Read(ref _bytesReceived);
    public long BytesSent => Interlocked.Read(ref _bytesSent);
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
            exception is SocketException or IOException or InvalidOperationException)
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

        if (_receiveCancellation != null)
        {
            await _receiveCancellation.CancelAsync().ConfigureAwait(false);
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

        _receiveCancellation?.Dispose();
        _tcpClient.Dispose();
    }

    private async Task ConnectSingleEndpointAsync(
        LoadTestOptions options,
        LoadAccount? account,
        CancellationToken cancellationToken)
    {
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
            if (account == null)
            {
                throw new InvalidOperationException("A login client requires an account.");
            }

            var loginClock = Stopwatch.StartNew();
            string packet = NosTaleLoginCodec.BuildPacket(options, account, ClientIndex);
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
            LoginAccepted = response != null && NosTaleLoginCodec.LooksAccepted(response);
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
        await WriteWorldPacketAsync(
                stream,
                WorldSessionId,
                $"2 0 0 {account.Password}",
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
    }

    private async Task PerformLoginForWorldAsync(
        LoadTestOptions options,
        LoadAccount account,
        CancellationToken cancellationToken)
    {
        using var loginClient = new TcpClient
        {
            NoDelay = true
        };
        var loginClock = Stopwatch.StartNew();

        await ConnectWithTimeoutAsync(
                loginClient,
                options.LoginHost,
                options.LoginPort,
                options.ConnectTimeoutMilliseconds,
                cancellationToken)
            .ConfigureAwait(false);

        NetworkStream stream = loginClient.GetStream();
        string packet = NosTaleLoginCodec.BuildPacket(options, account, ClientIndex);
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
        LoginAccepted = response != null && NosTaleLoginCodec.LooksAccepted(response);
        if (!LoginAccepted ||
            response == null ||
            !NosTaleLoginCodec.TryParseWorldTicket(
                response,
                out int sessionId,
                out string? advertisedHost,
                out int advertisedPort))
        {
            throw new InvalidOperationException("Login did not return a usable NsTeST World session ticket.");
        }

        WorldSessionId = sessionId;
        AdvertisedWorldHost = advertisedHost;
        AdvertisedWorldPort = advertisedPort;
    }

    private async Task<string?> ReadLoginResponseAsync(
        NetworkStream stream,
        int timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        byte[] responseBuffer = new byte[8192];
        using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readCancellation.CancelAfter(timeoutMilliseconds);

        try
        {
            int received = await stream
                .ReadAsync(responseBuffer.AsMemory(), readCancellation.Token)
                .ConfigureAwait(false);
            if (received <= 0)
            {
                Interlocked.Exchange(ref _disconnected, 1);
                return null;
            }

            Interlocked.Add(ref _bytesReceived, received);
            return NosTaleLoginCodec.DecodeServerPacket(responseBuffer.AsSpan(0, received));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
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
