using Microsoft.Extensions.Configuration;
using Grpc.Core;
using NosGm.Authentication.Client;
using NosGm.Authentication.Server;
using NosGm.Authentication.Server.Security;
using NosGm.Authentication.Server.State;
using NosGm.Cluster.Contracts.Authentication.Runtime;
using NosGm.Cluster.Contracts.V1;
using WireNodeRole = NosGm.Cluster.Wire.V1.ClusterNodeRole;

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException(
            $"{name}: expected '{expected}', received '{actual}'.");
    }

    Console.WriteLine($"[PASS] {name}");
}

static void AssertThrows<TException>(Action action, string name)
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

static string ReadRequiredEnvironment(string variableName)
{
    string value = Environment.GetEnvironmentVariable(variableName);
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException(
            $"Live gRPC acceptance requires {variableName}.");
    }

    return value;
}

static AuthenticationGrpcClientOptions LoadLiveClientOptions(
    ClusterNodeRole role,
    string roleName)
{
    string prefix =
        "NOSGM_AUTH_GRPC_LIVE_" + roleName.ToUpperInvariant();
    return AuthenticationGrpcClientOptions.Load(
        role,
        variableName => variableName switch
        {
            AuthenticationGrpcClientOptions.AddressVariable =>
                ReadRequiredEnvironment(
                    AuthenticationGrpcClientOptions.AddressVariable),
            AuthenticationGrpcClientOptions.CertificatePathVariable =>
                ReadRequiredEnvironment(prefix + "_CERT_PATH"),
            AuthenticationGrpcClientOptions.CertificatePasswordVariable =>
                Environment.GetEnvironmentVariable(
                    prefix + "_CERT_PASSWORD") ?? string.Empty,
            AuthenticationGrpcClientOptions.CallerInstanceIdVariable =>
                "acceptance-" + roleName.ToLowerInvariant() + "-1",
            AuthenticationGrpcClientOptions.DeadlineVariable => "10000",
            AuthenticationGrpcClientOptions.WireModeVariable =>
                Environment.GetEnvironmentVariable(
                    AuthenticationGrpcClientOptions.WireModeVariable),
            _ => null
        });
}

static async Task AssertPermissionDeniedAsync(
    Func<Task> action,
    string name)
{
    try
    {
        await action();
    }
    catch (RpcException exception)
        when (exception.StatusCode == StatusCode.PermissionDenied)
    {
        Console.WriteLine($"[PASS] {name}");
        return;
    }

    throw new InvalidOperationException(
        $"{name}: expected a permission-denied gRPC response.");
}

static async Task RunLiveGrpcAcceptanceAsync()
{
    using var authBridge =
        new GrpcGameforgeAuthenticationTransport(
            LoadLiveClientOptions(
                ClusterNodeRole.AuthBridge,
                "AuthBridge"));
    using var login =
        new GrpcGameforgeAuthenticationTransport(
            LoadLiveClientOptions(ClusterNodeRole.Login, "Login"));
    using var world =
        new GrpcGameforgeAuthenticationTransport(
            LoadLiveClientOptions(ClusterNodeRole.World, "World"));
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));

    string authorizationCode = Guid.NewGuid().ToString("D");
    string installationId = Guid.NewGuid().ToString("D");
    const uint countryId = 5;

    AssertEqual(
        AuthenticationTransportResultCode.Success,
        await authBridge.IssueAuthTicketAsync(
            "grpc-acceptance",
            authorizationCode,
            installationId,
            countryId,
            timeout.Token),
        "Live AuthBridge certificate issues a ticket through mTLS");

    await AssertPermissionDeniedAsync(
        async () =>
        {
            await authBridge.ConsumeAuthTicketAsync(
                authorizationCode,
                installationId,
                countryId,
                61000,
                timeout.Token);
        },
        "Live role policy rejects AuthBridge ticket consumption");

    AuthenticationTicketConsumptionResult first =
        await login.ConsumeAuthTicketAsync(
            authorizationCode,
            installationId,
            countryId,
            61001,
            timeout.Token);
    AuthenticationTicketConsumptionResult second =
        await login.ConsumeAuthTicketAsync(
            authorizationCode,
            installationId,
            countryId,
            61002,
            timeout.Token);
    AuthenticationTicketConsumptionResult third =
        await login.ConsumeAuthTicketAsync(
            authorizationCode,
            installationId,
            countryId,
            61003,
            timeout.Token);
    AuthenticationTicketConsumptionResult exhausted =
        await login.ConsumeAuthTicketAsync(
            authorizationCode,
            installationId,
            countryId,
            61004,
            timeout.Token);

    AssertEqual(
        AuthenticationTransportResultCode.Success,
        first.Result,
        "Live Login certificate consumes the first ticket stage");
    AssertEqual(
        first.SessionId,
        second.SessionId,
        "Live second Login stage preserves the SessionID");
    AssertEqual(
        first.SessionId,
        third.SessionId,
        "Live third Login stage preserves the SessionID");
    AssertEqual(
        AuthenticationTransportResultCode.NotFoundOrExpired,
        exhausted.Result,
        "Live ticket is exhausted after exactly three consumptions");

    const long accountId = 9100001;
    AssertEqual(
        AuthenticationTransportResultCode.Success,
        await login.IssueWorldPermitAsync(
            accountId,
            first.SessionId,
            "127.0.0.1",
            timeout.Token),
        "Live Login certificate issues a World permit");
    AssertEqual(
        AuthenticationTransportResultCode.Success,
        await world.ConsumeWorldPermitAsync(
            accountId,
            first.SessionId,
            "127.0.0.1",
            timeout.Token),
        "Live World certificate consumes its permit");
    AssertEqual(
        AuthenticationTransportResultCode.NotFoundOrExpired,
        await world.ConsumeWorldPermitAsync(
            accountId,
            first.SessionId,
            "127.0.0.1",
            timeout.Token),
        "Live World permit cannot be replayed");

    AssertEqual(
        AuthenticationTransportResultCode.Success,
        await login.IssueWorldPermitAsync(
            accountId + 1,
            first.SessionId + 1,
            string.Empty,
            timeout.Token),
        "Live Login certificate issues the revocation test permit");
    AssertEqual(
        AuthenticationTransportResultCode.Success,
        await login.RevokeWorldPermitAsync(
            accountId + 1,
            first.SessionId + 1,
            timeout.Token),
        "Live Login certificate revokes a World permit");
    AssertEqual(
        AuthenticationTransportResultCode.NotFoundOrExpired,
        await world.ConsumeWorldPermitAsync(
            accountId + 1,
            first.SessionId + 1,
            "127.0.0.1",
            timeout.Token),
        "A revoked live World permit cannot be consumed");

    Console.WriteLine(
        "Live NosGM authentication gRPC acceptance completed successfully.");
}

AssertEqual(
    AuthenticationTransportMode.Scs,
    AuthenticationTransportModeParser.ParseOrDefault(null),
    "SCS remains the default authentication transport");
AssertEqual(
    AuthenticationTransportMode.Grpc,
    AuthenticationTransportModeParser.ParseOrDefault("grpc"),
    "gRPC requires an explicit transport selection");
AssertThrows<InvalidOperationException>(
    () => AuthenticationTransportModeParser.ParseOrDefault("automatic"),
    "Unknown transport values fail closed");
AssertEqual(
    "NOSGM_AUTH_TRANSPORT",
    AuthenticationTransportModeParser.EnvironmentVariableName,
    "All authentication callers share one explicit transport selector");

string absoluteClientCertificate =
    Path.GetFullPath("authentication-client-self-test.pfx");
var clientValues = new Dictionary<string, string>
{
    [AuthenticationGrpcClientOptions.CertificatePathVariable] =
        absoluteClientCertificate,
    [AuthenticationGrpcClientOptions.CallerInstanceIdVariable] =
        "login-self-test-1"
};
AuthenticationGrpcClientOptions clientOptions =
    AuthenticationGrpcClientOptions.Load(
        ClusterNodeRole.Login,
        name => clientValues.TryGetValue(name, out string value)
            ? value
            : null);
AssertEqual(
    new Uri(AuthenticationGrpcClientOptions.DefaultAddress),
    clientOptions.Address,
    "The gRPC caller defaults to the loopback HTTPS origin");
AssertEqual(
    ClusterNodeRole.Login,
    clientOptions.CallerRole,
    "The caller role is fixed by process code");
AssertEqual(
    ClusterProtocolLimits.DefaultDeadlineMilliseconds,
    clientOptions.DeadlineMilliseconds,
    "Every gRPC caller gets a bounded default deadline");
AssertEqual(
    AuthenticationGrpcWireMode.Http2,
    clientOptions.WireMode,
    "Native HTTP/2 remains the default gRPC wire mode");

var grpcWebClientValues =
    new Dictionary<string, string>(clientValues)
    {
        [AuthenticationGrpcClientOptions.WireModeVariable] = "GRPCWEB"
    };
AssertEqual(
    AuthenticationGrpcWireMode.GrpcWeb,
    AuthenticationGrpcClientOptions.Load(
        ClusterNodeRole.Login,
        name => grpcWebClientValues.TryGetValue(name, out string value)
            ? value
            : null).WireMode,
    "gRPC-Web requires an explicit wire-mode selection");
grpcWebClientValues[AuthenticationGrpcClientOptions.WireModeVariable] =
    "automatic";
AssertThrows<InvalidOperationException>(
    () => AuthenticationGrpcClientOptions.Load(
        ClusterNodeRole.Login,
        name => grpcWebClientValues.TryGetValue(name, out string value)
            ? value
            : null),
    "Unknown gRPC wire modes fail closed");

var remoteClientValues =
    new Dictionary<string, string>(clientValues)
    {
        [AuthenticationGrpcClientOptions.AddressVariable] =
            "https://authentication.example.invalid:7443"
    };
AssertThrows<InvalidOperationException>(
    () => AuthenticationGrpcClientOptions.Load(
        ClusterNodeRole.Login,
        name => remoteClientValues.TryGetValue(name, out string value)
            ? value
            : null),
    "Authentication gRPC callers cannot leave loopback");
AssertThrows<InvalidOperationException>(
    () => AuthenticationGrpcClientOptions.Load(
        ClusterNodeRole.Master,
        name => clientValues.TryGetValue(name, out string value)
            ? value
            : null),
    "A Master certificate cannot impersonate an allowed caller role");

var scs = new RecordingTransport();
var grpc = new RecordingTransport();
var scsRouter = new AuthenticationTransportRouter(
    AuthenticationTransportMode.Scs,
    scs,
    grpc);
await scsRouter.RevokeWorldPermitAsync(7, 11, CancellationToken.None);
AssertEqual(1, scs.Calls, "SCS selection dispatches only to SCS");
AssertEqual(0, grpc.Calls, "SCS selection never mirrors to gRPC");

var failingGrpc = new RecordingTransport
{
    Failure = new InvalidOperationException("selected transport failed")
};
var backupScs = new RecordingTransport();
var grpcRouter = new AuthenticationTransportRouter(
    AuthenticationTransportMode.Grpc,
    backupScs,
    failingGrpc);
try
{
    await grpcRouter.RevokeWorldPermitAsync(
        7,
        11,
        CancellationToken.None);
    throw new InvalidOperationException(
        "Selected transport failure was unexpectedly swallowed.");
}
catch (InvalidOperationException exception)
    when (exception.Message == "selected transport failed")
{
    Console.WriteLine(
        "[PASS] gRPC failure is not retried through the stateful SCS path");
}

AssertEqual(
    0,
    backupScs.Calls,
    "No automatic SCS fallback occurs after gRPC dispatch");
AssertThrows<InvalidOperationException>(
    () => new AuthenticationTransportRouter(
        AuthenticationTransportMode.Grpc,
        new RecordingTransport(),
        null),
    "Missing selected transport fails before dispatch");

string authBridgeFingerprint = new('A', 64);
string loginFingerprint = new('B', 64);
string worldFingerprint = new('C', 64);
var configurationValues = new Dictionary<string, string>
{
    [AuthenticationServerOptions.CertificatePathVariable] =
        Path.GetFullPath("authentication-self-test.pfx"),
    [AuthenticationServerOptions.AuthBridgeFingerprintsVariable] =
        authBridgeFingerprint,
    [AuthenticationServerOptions.LoginFingerprintsVariable] =
        loginFingerprint,
    [AuthenticationServerOptions.WorldFingerprintsVariable] =
        worldFingerprint
};
AuthenticationServerOptions options =
    AuthenticationServerOptions.Load(
        new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build());
AssertEqual(
    AuthenticationServerOptions.DefaultPort,
    options.Port,
    "Authentication runtime uses the bounded default loopback port");
var certificateRoles = new ClientCertificateRoleMap(options);
AssertEqual(
    true,
    certificateRoles.TryResolveFingerprint(
        authBridgeFingerprint,
        out WireNodeRole authBridgeRole),
    "Configured AuthBridge certificate is recognized");
AssertEqual(
    WireNodeRole.AuthBridge,
    authBridgeRole,
    "AuthBridge certificate receives only its intended role");
AssertEqual(
    false,
    certificateRoles.TryResolveFingerprint(
        new string('D', 64),
        out _),
    "Unknown certificate fingerprints fail closed");

var reusedCertificateValues =
    new Dictionary<string, string>(configurationValues)
    {
        [AuthenticationServerOptions.WorldFingerprintsVariable] =
            loginFingerprint
    };
AssertThrows<InvalidOperationException>(
    () => AuthenticationServerOptions.Load(
        new ConfigurationBuilder()
            .AddInMemoryCollection(reusedCertificateValues)
            .Build()),
    "A client certificate cannot be reused across service roles");

var time = new MutableTimeProvider(
    new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
var state = new GameforgeAuthenticationState(time);
const string authorizationCode =
    "11111111-2222-3333-4444-555555555555";
Guid installationId =
    Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

AssertEqual(
    AuthenticationTransportResultCode.Success,
    state.TryIssueTicket(
        "runtime-test",
        authorizationCode,
        installationId,
        5,
        TimeSpan.FromMinutes(2)),
    "A valid Gameforge ticket is issued");
AssertEqual(
    AuthenticationTransportResultCode.Conflict,
    state.TryIssueTicket(
        "runtime-test",
        authorizationCode,
        installationId,
        5,
        TimeSpan.FromMinutes(2)),
    "Duplicate ticket issue is rejected");

AuthenticationTicketConsumptionResult first =
    state.TryConsumeTicket(
        authorizationCode,
        installationId,
        5,
        50219);
AuthenticationTicketConsumptionResult second =
    state.TryConsumeTicket(
        authorizationCode,
        installationId,
        5,
        99999);
AuthenticationTicketConsumptionResult third =
    state.TryConsumeTicket(
        authorizationCode,
        installationId,
        5,
        77777);
AssertEqual(
    AuthenticationTransportResultCode.Success,
    first.Result,
    "First ticket consumption succeeds");
AssertEqual(1, first.ConsumptionNumber, "First consumption is numbered");
AssertEqual(
    first.SessionId,
    second.SessionId,
    "Second consumption preserves the first stable SessionID");
AssertEqual(
    first.SessionId,
    third.SessionId,
    "Third consumption preserves the first stable SessionID");
AssertEqual(
    AuthenticationTransportResultCode.NotFoundOrExpired,
    state.TryConsumeTicket(
        authorizationCode,
        installationId,
        5,
        50219).Result,
    "Ticket is removed after exactly three consumptions");

const string mismatchedAuthorizationCode =
    "22222222-3333-4444-5555-666666666666";
state.TryIssueTicket(
    "mismatch-test",
    mismatchedAuthorizationCode,
    installationId,
    2,
    TimeSpan.FromMinutes(2));
AssertEqual(
    AuthenticationTransportResultCode.NotFoundOrExpired,
    state.TryConsumeTicket(
        mismatchedAuthorizationCode,
        Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
        2,
        50220).Result,
    "Installation mismatch removes the bound ticket");
AssertEqual(
    AuthenticationTransportResultCode.NotFoundOrExpired,
    state.TryConsumeTicket(
        mismatchedAuthorizationCode,
        installationId,
        2,
        50220).Result,
    "Removed mismatched ticket cannot be retried");

AssertEqual(
    AuthenticationTransportResultCode.Success,
    state.TryIssuePermit(
        42,
        50219,
        "127.0.0.1",
        TimeSpan.FromMinutes(2)),
    "Login issues one World permit");
AssertEqual(
    AuthenticationTransportResultCode.NotFoundOrExpired,
    state.TryConsumePermit(42, 50219, "127.0.0.2"),
    "IP mismatch fails the one-use World permit");
AssertEqual(
    AuthenticationTransportResultCode.NotFoundOrExpired,
    state.TryConsumePermit(42, 50219, "127.0.0.1"),
    "Failed IP binding cannot replay the consumed permit");

state.TryIssuePermit(
    43,
    50220,
    string.Empty,
    TimeSpan.FromMinutes(2));
AssertEqual(
    AuthenticationTransportResultCode.Success,
    state.TryConsumePermit(43, 50220, "203.0.113.10"),
    "An intentionally unbound World permit remains compatible");
AssertEqual(
    AuthenticationTransportResultCode.NotFoundOrExpired,
    state.TryConsumePermit(43, 50220, "203.0.113.10"),
    "World permits are consumed exactly once");

const string expiringAuthorizationCode =
    "33333333-4444-5555-6666-777777777777";
state.TryIssueTicket(
    "expiry-test",
    expiringAuthorizationCode,
    installationId,
    0,
    TimeSpan.FromSeconds(15));
time.Advance(TimeSpan.FromSeconds(16));
AssertEqual(
    AuthenticationTransportResultCode.NotFoundOrExpired,
    state.TryConsumeTicket(
        expiringAuthorizationCode,
        installationId,
        0,
        50221).Result,
    "Expired tickets fail closed");

var replayGuard = new AuthenticationRequestReplayGuard();
long now = time.GetUtcNow().ToUnixTimeMilliseconds();
string requestId = Guid.NewGuid().ToString("D");
AssertEqual(
    true,
    replayGuard.TryAccept(
        requestId,
        now + ClusterProtocolLimits.DefaultDeadlineMilliseconds,
        now),
    "First bounded request ID is accepted");
AssertEqual(
    false,
    replayGuard.TryAccept(
        requestId,
        now + ClusterProtocolLimits.DefaultDeadlineMilliseconds,
        now),
    "Duplicate request ID is rejected");

if (args.Contains("--live", StringComparer.Ordinal))
{
    await RunLiveGrpcAcceptanceAsync();
}

Console.WriteLine(
    "Authentication runtime self-test completed successfully.");

internal sealed class MutableTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public MutableTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }

    public void Advance(TimeSpan duration)
    {
        _utcNow = _utcNow.Add(duration);
    }
}

internal sealed class RecordingTransport
    : IGameforgeAuthenticationTransport
{
    public int Calls { get; private set; }

    public Exception Failure { get; init; }

    public Task<AuthenticationTransportResultCode> IssueAuthTicketAsync(
        string accountName,
        string authorizationCode,
        string installationId,
        uint countryId,
        CancellationToken cancellationToken)
    {
        RecordCall();
        return Task.FromResult(AuthenticationTransportResultCode.Success);
    }

    public Task<AuthenticationTicketConsumptionResult>
        ConsumeAuthTicketAsync(
            string authorizationCode,
            string installationId,
            uint countryId,
            int proposedSessionId,
            CancellationToken cancellationToken)
    {
        RecordCall();
        return Task.FromResult(
            new AuthenticationTicketConsumptionResult
            {
                Result = AuthenticationTransportResultCode.Success
            });
    }

    public Task<AuthenticationTransportResultCode> IssueWorldPermitAsync(
        long accountId,
        int sessionId,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        RecordCall();
        return Task.FromResult(AuthenticationTransportResultCode.Success);
    }

    public Task<AuthenticationTransportResultCode> ConsumeWorldPermitAsync(
        long accountId,
        int sessionId,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        RecordCall();
        return Task.FromResult(AuthenticationTransportResultCode.Success);
    }

    public Task<AuthenticationTransportResultCode> RevokeWorldPermitAsync(
        long accountId,
        int sessionId,
        CancellationToken cancellationToken)
    {
        RecordCall();
        return Task.FromResult(AuthenticationTransportResultCode.Success);
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
