using Microsoft.Extensions.Configuration;
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
