using System.Runtime.CompilerServices;
using NosGm.Communication.Client;

internal static class CommunicationTransportSelfTest
{
    [ModuleInitializer]
    public static void Run()
    {
        AssertEqual(
            CommunicationTransportMode.Scs,
            CommunicationTransportModeParser.ParseOrDefault(null),
            "Communication defaults to the SCS rollback transport");
        AssertEqual(
            CommunicationTransportMode.Scs,
            CommunicationTransportModeParser.ParseOrDefault("scs"),
            "Communication explicitly selects SCS");
        AssertEqual(
            CommunicationTransportMode.Grpc,
            CommunicationTransportModeParser.ParseOrDefault("GRPC"),
            "Communication explicitly selects gRPC");
        AssertThrows<InvalidOperationException>(
            () => CommunicationTransportModeParser.ParseOrDefault("automatic"),
            "Unknown communication transport values fail closed");
        AssertEqual(
            "NOSGM_COMMUNICATION_TRANSPORT",
            CommunicationTransportModeParser.EnvironmentVariableName,
            "Communication callers share one selector variable");

        var scs = new RecordingCommunicationTransport();
        var grpc = new RecordingCommunicationTransport();
        var scsRouter = new CommunicationTransportRouter(
            CommunicationTransportMode.Scs,
            scs,
            grpc);
        CommunicationTransportResultCode scsResult =
            scsRouter.RegisterAccountLoginAsync(
                    42,
                    50219,
                    "127.0.0.1",
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        AssertEqual(
            CommunicationTransportResultCode.Success,
            scsResult,
            "SCS communication selection returns the selected result");
        AssertEqual(1, scs.Calls, "SCS selection dispatches only to SCS");
        AssertEqual(0, grpc.Calls, "SCS selection never mirrors to gRPC");

        var backupScs = new RecordingCommunicationTransport();
        var failingGrpc = new RecordingCommunicationTransport
        {
            Failure = new InvalidOperationException(
                "selected communication transport failed")
        };
        var grpcRouter = new CommunicationTransportRouter(
            CommunicationTransportMode.Grpc,
            backupScs,
            failingGrpc);
        try
        {
            grpcRouter.DisconnectAccountAsync(
                    42,
                    50219,
                    true,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            throw new InvalidOperationException(
                "Selected communication failure was unexpectedly swallowed.");
        }
        catch (InvalidOperationException exception)
            when (exception.Message ==
                  "selected communication transport failed")
        {
            Console.WriteLine(
                "[PASS] Communication gRPC failure is not retried through SCS");
        }

        AssertEqual(
            0,
            backupScs.Calls,
            "Communication never falls back after a stateful gRPC dispatch");
        AssertEqual(
            1,
            failingGrpc.Calls,
            "The selected failing transport is called exactly once");
        AssertThrows<InvalidOperationException>(
            () => new CommunicationTransportRouter(
                CommunicationTransportMode.Grpc,
                new RecordingCommunicationTransport(),
                null),
            "A missing selected communication transport fails before dispatch");

        Console.WriteLine(
            "[PASS] Communication transport router self-test");
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', received '{actual}'.");
        }

        Console.WriteLine($"[PASS] {name}");
    }

    private static void AssertThrows<TException>(Action action, string name)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            Console.WriteLine($"[PASS] {name}");
            return;
        }

        throw new InvalidOperationException(
            $"{name}: expected {typeof(TException).Name}.");
    }

    private sealed class RecordingCommunicationTransport
        : IClusterCommunicationTransport
    {
        public int Calls { get; private set; }

        public Exception Failure { get; init; }

        public Task<CommunicationTransportResultCode> RegisterAccountLoginAsync(
            long accountId,
            int sessionId,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            return RecordResult();
        }

        public Task<CommunicationBooleanResult>
            IsAccountSessionRegisteredAsync(
                long accountId,
                int sessionId,
                CancellationToken cancellationToken)
        {
            RecordCall();
            return Task.FromResult(new CommunicationBooleanResult
            {
                Result = CommunicationTransportResultCode.Success,
                Value = true
            });
        }

        public Task<CommunicationBooleanResult> IsLoginPermittedAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            RecordCall();
            return Task.FromResult(new CommunicationBooleanResult
            {
                Result = CommunicationTransportResultCode.Success,
                Value = true
            });
        }

        public Task<CommunicationBooleanResult> IsAccountConnectedAsync(
            long accountId,
            CancellationToken cancellationToken)
        {
            RecordCall();
            return Task.FromResult(new CommunicationBooleanResult
            {
                Result = CommunicationTransportResultCode.Success,
                Value = true
            });
        }

        public Task<CommunicationTransportResultCode> ConnectAccountAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            return RecordResult();
        }

        public Task<CommunicationTransportResultCode> DisconnectAccountAsync(
            long accountId,
            int sessionId,
            bool preserveSessionRegistration,
            CancellationToken cancellationToken)
        {
            return RecordResult();
        }

        public Task<CommunicationTransportResultCode> PulseAccountAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            return RecordResult();
        }

        public Task<CommunicationTransportResultCode> ConnectCharacterAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            long characterId,
            CancellationToken cancellationToken)
        {
            return RecordResult();
        }

        public Task<CommunicationTransportResultCode> DisconnectCharacterAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            long characterId,
            CancellationToken cancellationToken)
        {
            return RecordResult();
        }

        public Task<CommunicationWorldRegistrationResult>
            RegisterWorldServerAsync(
                Guid worldId,
                string endpointIp,
                int endpointPort,
                int accountLimit,
                string worldGroup,
                CancellationToken cancellationToken)
        {
            RecordCall();
            return Task.FromResult(new CommunicationWorldRegistrationResult
            {
                Result = CommunicationTransportResultCode.Success,
                ChannelId = 1
            });
        }

        public Task<CommunicationTransportResultCode> UnregisterWorldServerAsync(
            Guid worldId,
            CancellationToken cancellationToken)
        {
            return RecordResult();
        }

        public Task<CommunicationWorldListResult> ListWorldServersAsync(
            CancellationToken cancellationToken)
        {
            RecordCall();
            return Task.FromResult(new CommunicationWorldListResult
            {
                Result = CommunicationTransportResultCode.Success
            });
        }

        private Task<CommunicationTransportResultCode> RecordResult()
        {
            RecordCall();
            return Task.FromResult(CommunicationTransportResultCode.Success);
        }

        private void RecordCall()
        {
            Calls++;
            if (Failure != null)
            {
                throw Failure;
            }
        }
    }
}
