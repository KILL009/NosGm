using System.Diagnostics;
using System.Net.Sockets;

namespace NosGM.LoadTest;

internal sealed class LoadClient : IAsyncDisposable
{
    private readonly TcpClient _tcpClient = new(AddressFamily.InterNetwork);
    private CancellationTokenSource? _receiveCancellation;
    private Task? _receiveTask;
    private long _bytesReceived;
    private long _bytesSent;
    private int _closed;

    private LoadClient(int clientIndex)
    {
        ClientIndex = clientIndex;
        _tcpClient.NoDelay = true;
    }

    public int ClientIndex { get; }
    public double ConnectMilliseconds { get; private set; }
    public string? Failure { get; private set; }
    public string? LoginResponse { get; private set; }
    public bool LoginAccepted { get; private set; }
    public long BytesReceived => Interlocked.Read(ref _bytesReceived);
    public long BytesSent => Interlocked.Read(ref _bytesSent);
    public bool IsConnected =>
        Volatile.Read(ref _closed) == 0 &&
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
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using (var connectCancellation =
                   CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                connectCancellation.CancelAfter(options.ConnectTimeoutMilliseconds);
                await client._tcpClient
                    .ConnectAsync(options.Host, options.Port, connectCancellation.Token)
                    .ConfigureAwait(false);
            }

            stopwatch.Stop();
            client.ConnectMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

            NetworkStream stream = client._tcpClient.GetStream();
            if (options.Scenario == LoadScenario.Login)
            {
                if (account == null)
                {
                    throw new InvalidOperationException("A login client requires an account.");
                }

                string packet = NosTaleLoginCodec.BuildPacket(options, account, clientIndex);
                byte[] payload = NosTaleLoginCodec.EncodeClientPacket(packet);
                await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                Interlocked.Add(ref client._bytesSent, payload.Length);

                byte[] responseBuffer = new byte[8192];
                using var readCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                readCancellation.CancelAfter(options.ReadTimeoutMilliseconds);

                try
                {
                    int received = await stream
                        .ReadAsync(responseBuffer, readCancellation.Token)
                        .ConfigureAwait(false);
                    if (received > 0)
                    {
                        Interlocked.Add(ref client._bytesReceived, received);
                        client.LoginResponse = NosTaleLoginCodec.DecodeServerPacket(
                            responseBuffer.AsSpan(0, received));
                        client.LoginAccepted = NosTaleLoginCodec.LooksAccepted(client.LoginResponse);
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    client.LoginResponse = "<login-response-timeout>";
                }
            }

            client._receiveCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            client._receiveTask = client.DrainAsync(
                stream,
                client._receiveCancellation.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            client.ConnectMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            client.Failure = "timeout";
            await client.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is SocketException or IOException or InvalidOperationException)
        {
            stopwatch.Stop();
            client.ConnectMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            client.Failure = exception.GetType().Name + ": " + exception.Message;
            await client.DisposeAsync().ConfigureAwait(false);
        }

        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
        {
            return;
        }

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

    private async Task DrainAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[8192];
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int received = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (received <= 0)
                {
                    Interlocked.Exchange(ref _closed, 1);
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
            Interlocked.Exchange(ref _closed, 1);
        }
        catch (SocketException)
        {
            Interlocked.Exchange(ref _closed, 1);
        }
    }
}
