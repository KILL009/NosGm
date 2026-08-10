using System.Net;
using System.Net.Sockets;

namespace NosGM.LoadTest;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            LoadTestOptions options = LoadTestOptions.Parse(args);
            if (options.ShowHelp)
            {
                Console.WriteLine(LoadTestOptions.HelpText);
                return 0;
            }

            using var shutdown = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Cancel();
            };

            if (options.SelfTest)
            {
                ModernLoginTicketClient.RunSelfTest();
                NosTaleLoginCodec.RunSelfTest();
                NosTaleWorldCodec.RunSelfTest();
                await LoopbackAcceptance.RunAsync(shutdown.Token).ConfigureAwait(false);
                return 0;
            }

            await TargetSafety
                .EnsureAllowedAsync(options.Host, options.AllowPublicTarget, shutdown.Token)
                .ConfigureAwait(false);
            if (options.Scenario == LoadScenario.World &&
                !string.Equals(options.LoginHost, options.Host, StringComparison.OrdinalIgnoreCase))
            {
                await TargetSafety
                    .EnsureAllowedAsync(options.LoginHost, options.AllowPublicTarget, shutdown.Token)
                    .ConfigureAwait(false);
            }

            if ((options.Scenario is LoadScenario.Login or LoadScenario.World) &&
                options.LoginMode == LoginMode.Modern)
            {
                await TargetSafety
                    .EnsureAllowedAsync(
                        options.AuthBridgeUri.Host,
                        options.AllowPublicTarget,
                        shutdown.Token)
                    .ConfigureAwait(false);
            }

            IReadOnlyList<LoadAccount>? accounts =
                options.Scenario is LoadScenario.Login or LoadScenario.World
                    ? LoadAccountReader.Read(options.AccountsPath!)
                    : null;

            await using var runner = new LoadRunner(options, accounts);
            await runner.RunAsync(shutdown.Token).ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Load test cancelled.");
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Load test failed: {exception.Message}");
            return 1;
        }
    }
}

internal static class LoopbackAcceptance
{
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        var acceptedClients = new List<TcpClient>();
        using var listenerCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        listener.Start(512);
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task acceptTask = AcceptLoopAsync(
            listener,
            acceptedClients,
            listenerCancellation.Token);

        string output = Path.Combine(
            Path.GetTempPath(),
            "nosgm-load-test-selftest-" + Guid.NewGuid().ToString("N"));

        var options = new LoadTestOptions
        {
            Host = "127.0.0.1",
            Port = port,
            Scenario = LoadScenario.Tcp,
            Stages = [25, 100, 250],
            RampPerSecond = 500,
            HoldSeconds = 1,
            ConnectTimeoutMilliseconds = 3000,
            ReadTimeoutMilliseconds = 1000,
            OutputDirectory = output,
            ProcessNames = [],
            SelfTest = true
        };

        try
        {
            await using var runner = new LoadRunner(options);
            await runner.RunAsync(cancellationToken).ConfigureAwait(false);

            StageResult? final = runner.Results.LastOrDefault();
            if (final == null || final.Connected != 250 || final.Failed != 0)
            {
                throw new InvalidOperationException(
                    "Loopback acceptance did not hold all 250 expected TCP clients.");
            }

            string jsonPath = Path.Combine(output, "load-test.json");
            string csvPath = Path.Combine(output, "load-test.csv");
            if (!File.Exists(jsonPath) || !File.Exists(csvPath))
            {
                throw new InvalidOperationException(
                    "Loopback acceptance did not produce both JSON and CSV reports.");
            }

            Console.WriteLine(
                "NosGM Load Test codec checks and loopback acceptance passed with 250 concurrent clients.");
        }
        finally
        {
            await listenerCancellation.CancelAsync().ConfigureAwait(false);
            listener.Stop();

            try
            {
                await acceptTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException) when (listenerCancellation.IsCancellationRequested)
            {
            }

            lock (acceptedClients)
            {
                foreach (TcpClient client in acceptedClients)
                {
                    client.Dispose();
                }

                acceptedClients.Clear();
            }

            try
            {
                Directory.Delete(output, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static async Task AcceptLoopAsync(
        TcpListener listener,
        List<TcpClient> acceptedClients,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await listener
                .AcceptTcpClientAsync(cancellationToken)
                .ConfigureAwait(false);
            client.NoDelay = true;
            lock (acceptedClients)
            {
                acceptedClients.Add(client);
            }
        }
    }
}
