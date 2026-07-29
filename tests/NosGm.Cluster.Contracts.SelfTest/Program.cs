using NosGm.Cluster.Contracts.V1;
using NosGm.Cluster.Contracts.Authentication.V1;
using NosGm.Cluster.Contracts.Communication.V1;
using WireAuthenticationResultCode =
    NosGm.Cluster.Wire.V1.AuthenticationResultCode;
using WireCommunicationResultCode =
    NosGm.Cluster.Wire.V1.CommunicationResultCode;
using WireClusterNodeRole = NosGm.Cluster.Wire.V1.ClusterNodeRole;
using WireClusterService = NosGm.Cluster.Wire.V1.ClusterService;
using WireAccountRequest = NosGm.Cluster.Wire.V1.AccountRequest;
using WireAccountSessionRequest = NosGm.Cluster.Wire.V1.AccountSessionRequest;
using WireCharacterWorldRequest = NosGm.Cluster.Wire.V1.CharacterWorldRequest;
using WireConnectAccountRequest = NosGm.Cluster.Wire.V1.ConnectAccountRequest;
using WireConsumeAuthTicketRequest =
    NosGm.Cluster.Wire.V1.ConsumeAuthTicketRequest;
using WireConsumeWorldPermitRequest =
    NosGm.Cluster.Wire.V1.ConsumeWorldPermitRequest;
using WireDisconnectAccountRequest =
    NosGm.Cluster.Wire.V1.DisconnectAccountRequest;
using WireIssueAuthTicketRequest =
    NosGm.Cluster.Wire.V1.IssueAuthTicketRequest;
using WireIssueWorldPermitRequest =
    NosGm.Cluster.Wire.V1.IssueWorldPermitRequest;
using WireListWorldServersRequest =
    NosGm.Cluster.Wire.V1.ListWorldServersRequest;
using WireProtocolVersion = NosGm.Cluster.Wire.V1.ProtocolVersion;
using WireRegisterAccountLoginRequest =
    NosGm.Cluster.Wire.V1.RegisterAccountLoginRequest;
using WireRegisterWorldServerRequest =
    NosGm.Cluster.Wire.V1.RegisterWorldServerRequest;
using WireRequestContext = NosGm.Cluster.Wire.V1.RequestContext;
using WireRevokeWorldPermitRequest =
    NosGm.Cluster.Wire.V1.RevokeWorldPermitRequest;
using WireWorldRequest = NosGm.Cluster.Wire.V1.WorldRequest;
using WireWorldServerRegistration =
    NosGm.Cluster.Wire.V1.WorldServerRegistration;

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException(
            $"{name}: expected '{expected}', received '{actual}'.");
    }

    Console.WriteLine($"[PASS] {name}");
}

static WireRequestContext CreateWireContext(
    WireClusterNodeRole role,
    WireClusterService service = WireClusterService.Authentication) =>
    new()
    {
        Version = new WireProtocolVersion
        {
            Major = ClusterContractVersion.Current.Major,
            Minor = ClusterContractVersion.Current.Minor
        },
        RequestId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
        IssuedAtUnixTimeMs = 1_800_000_000_000,
        DeadlineUnixTimeMs =
            1_800_000_000_000 +
            ClusterProtocolLimits.DefaultDeadlineMilliseconds,
        CallerRole = role,
        RequestedService = service,
        CallerInstanceId = "self-test"
    };

const long issuedAt = 1_800_000_000_000;
var valid = new ClusterRequestContext
{
    Version = ClusterContractVersion.Current,
    RequestId = "11111111-2222-3333-4444-555555555555",
    IssuedAtUnixTimeMilliseconds = issuedAt,
    DeadlineUnixTimeMilliseconds =
        issuedAt + ClusterProtocolLimits.DefaultDeadlineMilliseconds,
    CallerRole = ClusterNodeRole.Login,
    RequestedService = ClusterService.Authentication,
    CallerInstanceId = "login-es-1"
};

AssertEqual(
    ClusterContractValidationError.None,
    ClusterContractValidator.Validate(valid),
    "Valid request context");

valid.Version = new ClusterContractVersion(2, 0);
AssertEqual(
    ClusterContractValidationError.UnsupportedVersion,
    ClusterContractValidator.Validate(valid),
    "Future major versions fail closed");
valid.Version = ClusterContractVersion.Current;

valid.RequestId = "not-a-request-id";
AssertEqual(
    ClusterContractValidationError.InvalidRequestId,
    ClusterContractValidator.Validate(valid),
    "Request IDs are canonical GUIDs");
valid.RequestId = "11111111-2222-3333-4444-555555555555";

valid.CallerRole = (ClusterNodeRole)128;
AssertEqual(
    ClusterContractValidationError.InvalidCallerRole,
    ClusterContractValidator.Validate(valid),
    "Unknown caller roles fail closed");
valid.CallerRole = ClusterNodeRole.World;

valid.RequestedService = (ClusterService)99;
AssertEqual(
    ClusterContractValidationError.InvalidService,
    ClusterContractValidator.Validate(valid),
    "Unknown services fail closed");
valid.RequestedService = ClusterService.Communication;

valid.DeadlineUnixTimeMilliseconds =
    issuedAt + ClusterProtocolLimits.MaxDeadlineMilliseconds + 1;
AssertEqual(
    ClusterContractValidationError.InvalidDeadline,
    ClusterContractValidator.Validate(valid),
    "Excessive deadlines fail closed");
valid.DeadlineUnixTimeMilliseconds =
    issuedAt + ClusterProtocolLimits.DefaultDeadlineMilliseconds;

AssertEqual(
    ClusterContractValidationError.None,
    ClusterContractValidator.ValidatePayloadLength(
        ClusterProtocolLimits.MaxInboundMessageBytes),
    "Maximum inbound payload is accepted");
AssertEqual(
    ClusterContractValidationError.PayloadTooLarge,
    ClusterContractValidator.ValidatePayloadLength(
        ClusterProtocolLimits.MaxInboundMessageBytes + 1),
    "Oversized inbound payload is rejected");
AssertEqual(
    ClusterContractValidationError.NegativePayloadLength,
    ClusterContractValidator.ValidatePayloadLength(-1),
    "Negative payload length is rejected");

AssertEqual("1.0", ClusterContractVersion.Current.ToString(), "Version format");

var generatedWireVersion = new WireProtocolVersion
{
    Major = ClusterContractVersion.Current.Major,
    Minor = ClusterContractVersion.Current.Minor
};
AssertEqual((uint)1, generatedWireVersion.Major, "Protobuf code generation");
AssertEqual((uint)0, generatedWireVersion.Minor, "Protobuf minor version");

var issueTicket = new WireIssueAuthTicketRequest
{
    Context = CreateWireContext(WireClusterNodeRole.AuthBridge),
    AccountName = "contract-test",
    AuthorizationCode = "11111111-2222-3333-4444-555555555555",
    InstallationId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    CountryId = 5
};
AssertEqual(
    AuthenticationContractValidationError.None,
    GameforgeAuthenticationContractValidator.Validate(issueTicket),
    "AuthBridge may issue a bounded Gameforge ticket");

issueTicket.Context.CallerRole = WireClusterNodeRole.World;
AssertEqual(
    AuthenticationContractValidationError.InvalidCallerRole,
    GameforgeAuthenticationContractValidator.Validate(issueTicket),
    "World cannot issue Gameforge tickets");
issueTicket.Context.CallerRole = WireClusterNodeRole.AuthBridge;

issueTicket.AccountName = "contract test";
AssertEqual(
    AuthenticationContractValidationError.InvalidAccountName,
    GameforgeAuthenticationContractValidator.Validate(issueTicket),
    "Account names with whitespace fail closed");
issueTicket.AccountName = "contract-test";

issueTicket.AuthorizationCode = "not-an-authorized-shape";
AssertEqual(
    AuthenticationContractValidationError.InvalidAuthorizationCode,
    GameforgeAuthenticationContractValidator.Validate(issueTicket),
    "Malformed authorization material fails closed");
issueTicket.AuthorizationCode =
    "11111111-2222-3333-4444-555555555555";

issueTicket.CountryId =
    AuthenticationContractLimits.MaxCountryId + 1;
AssertEqual(
    AuthenticationContractValidationError.InvalidCountryId,
    GameforgeAuthenticationContractValidator.Validate(issueTicket),
    "Only the ten regional country IDs are accepted");
issueTicket.CountryId = 5;

issueTicket.InstallationId = Guid.Empty.ToString("D");
AssertEqual(
    AuthenticationContractValidationError.InvalidInstallationId,
    GameforgeAuthenticationContractValidator.Validate(issueTicket),
    "Empty installation IDs fail closed");

var consumeTicket = new WireConsumeAuthTicketRequest
{
    Context = CreateWireContext(WireClusterNodeRole.Login),
    AuthorizationCode =
        "31313131313131312D323232322D333333332D343434342D353535353535353535353535",
    InstallationId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    CountryId = 0,
    ProposedSessionId = 50219
};
AssertEqual(
    AuthenticationContractValidationError.None,
    GameforgeAuthenticationContractValidator.Validate(consumeTicket),
    "Login may consume a bound ticket");
consumeTicket.ProposedSessionId = 0;
AssertEqual(
    AuthenticationContractValidationError.InvalidSessionId,
    GameforgeAuthenticationContractValidator.Validate(consumeTicket),
    "Ticket consumption requires a positive session ID");

var issuePermit = new WireIssueWorldPermitRequest
{
    Context = CreateWireContext(WireClusterNodeRole.Login),
    AccountId = 42,
    SessionId = 50219,
    IpAddress = "127.0.0.1"
};
AssertEqual(
    AuthenticationContractValidationError.None,
    GameforgeAuthenticationContractValidator.Validate(issuePermit),
    "Login may issue a World permit");
issuePermit.IpAddress = "not-an-ip";
AssertEqual(
    AuthenticationContractValidationError.InvalidIpAddress,
    GameforgeAuthenticationContractValidator.Validate(issuePermit),
    "World permit IP bindings are validated");

var consumePermit = new WireConsumeWorldPermitRequest
{
    Context = CreateWireContext(WireClusterNodeRole.World),
    AccountId = 42,
    SessionId = 50219,
    IpAddress = string.Empty
};
AssertEqual(
    AuthenticationContractValidationError.None,
    GameforgeAuthenticationContractValidator.Validate(consumePermit),
    "World may consume an unbound one-use permit");
consumePermit.Context.CallerRole = WireClusterNodeRole.Login;
AssertEqual(
    AuthenticationContractValidationError.InvalidCallerRole,
    GameforgeAuthenticationContractValidator.Validate(consumePermit),
    "Login cannot consume a World permit");

var revokePermit = new WireRevokeWorldPermitRequest
{
    Context = CreateWireContext(WireClusterNodeRole.Login),
    AccountId = 42,
    SessionId = 50219
};
AssertEqual(
    AuthenticationContractValidationError.None,
    GameforgeAuthenticationContractValidator.Validate(revokePermit),
    "Login may revoke a World permit");
revokePermit.AccountId = 0;
AssertEqual(
    AuthenticationContractValidationError.InvalidAccountId,
    GameforgeAuthenticationContractValidator.Validate(revokePermit),
    "Permit operations require a positive account ID");

AssertEqual(
    WireAuthenticationResultCode.Success,
    WireAuthenticationResultCode.Success,
    "Authentication result enum code generation");

var registerLogin = new WireRegisterAccountLoginRequest
{
    Context = CreateWireContext(
        WireClusterNodeRole.Login,
        WireClusterService.Communication),
    AccountId = 42,
    SessionId = 50219,
    IpAddress = "127.0.0.1"
};
AssertEqual(
    CommunicationContractValidationError.None,
    ClusterCommunicationContractValidator.Validate(registerLogin),
    "Login may register a typed account session");
registerLogin.Context.CallerRole = WireClusterNodeRole.World;
AssertEqual(
    CommunicationContractValidationError.InvalidCallerRole,
    ClusterCommunicationContractValidator.Validate(registerLogin),
    "World cannot create Login account registrations");
registerLogin.Context.CallerRole = WireClusterNodeRole.Login;
registerLogin.IpAddress = "not-an-ip";
AssertEqual(
    CommunicationContractValidationError.InvalidIpAddress,
    ClusterCommunicationContractValidator.Validate(registerLogin),
    "Account registrations require a canonical IP address");
registerLogin.IpAddress = "127.0.0.1";

var accountSession = new WireAccountSessionRequest
{
    Context = CreateWireContext(
        WireClusterNodeRole.Login,
        WireClusterService.Communication),
    AccountId = 42,
    SessionId = 50219
};
AssertEqual(
    CommunicationContractValidationError.None,
    ClusterCommunicationContractValidator.ValidateAccountSessionRegistered(
        accountSession),
    "Login may query its exact account/session registration");
accountSession.Context.CallerRole = WireClusterNodeRole.World;
AssertEqual(
    CommunicationContractValidationError.None,
    ClusterCommunicationContractValidator.ValidateLoginPermitted(accountSession),
    "World may verify a login handoff");
accountSession.Context.CallerRole = WireClusterNodeRole.Login;
AssertEqual(
    CommunicationContractValidationError.InvalidCallerRole,
    ClusterCommunicationContractValidator.ValidateLoginPermitted(accountSession),
    "Login cannot impersonate World login-permission checks");

var accountRequest = new WireAccountRequest
{
    Context = CreateWireContext(
        WireClusterNodeRole.Login,
        WireClusterService.Communication),
    AccountId = 42
};
AssertEqual(
    CommunicationContractValidationError.None,
    ClusterCommunicationContractValidator.ValidateAccountConnected(accountRequest),
    "Login may query whether an account is attached to World");
accountRequest.Context.CallerRole = WireClusterNodeRole.World;
AssertEqual(
    CommunicationContractValidationError.None,
    ClusterCommunicationContractValidator.ValidatePulse(accountRequest),
    "World may pulse an active account");

var connectAccount = new WireConnectAccountRequest
{
    Context = CreateWireContext(
        WireClusterNodeRole.World,
        WireClusterService.Communication),
    WorldId = "11111111-2222-3333-4444-555555555555",
    AccountId = 42,
    SessionId = 50219
};
AssertEqual(
    CommunicationContractValidationError.None,
    ClusterCommunicationContractValidator.Validate(connectAccount),
    "World may atomically attach an exact account/session tuple");
connectAccount.WorldId = Guid.Empty.ToString("D");
AssertEqual(
    CommunicationContractValidationError.InvalidWorldId,
    ClusterCommunicationContractValidator.Validate(connectAccount),
    "Empty World IDs fail closed");

var disconnectAccount = new WireDisconnectAccountRequest
{
    Context = CreateWireContext(
        WireClusterNodeRole.World,
        WireClusterService.Communication),
    AccountId = 42,
    SessionId = 50219,
    PreserveSessionRegistration = true
};
AssertEqual(
    CommunicationContractValidationError.None,
    ClusterCommunicationContractValidator.Validate(disconnectAccount),
    "World may preserve an exact Gameforge session during reselection");
disconnectAccount.SessionId = 0;
AssertEqual(
    CommunicationContractValidationError.InvalidPreserveSessionRequest,
    ClusterCommunicationContractValidator.Validate(disconnectAccount),
    "Preservation cannot operate without an exact session ID");

var characterWorld = new WireCharacterWorldRequest
{
    Context = CreateWireContext(
        WireClusterNodeRole.World,
        WireClusterService.Communication),
    WorldId = "11111111-2222-3333-4444-555555555555",
    CharacterId = 10004
};
AssertEqual(
    CommunicationContractValidationError.None,
    ClusterCommunicationContractValidator.ValidateConnectCharacter(characterWorld),
    "World may attach a character through a typed request");
AssertEqual(
    CommunicationContractValidationError.None,
    ClusterCommunicationContractValidator.ValidateDisconnectCharacter(characterWorld),
    "World may detach a character through a typed request");
characterWorld.CharacterId = 0;
AssertEqual(
    CommunicationContractValidationError.InvalidCharacterId,
    ClusterCommunicationContractValidator.ValidateConnectCharacter(characterWorld),
    "Character coordination requires a positive character ID");

var registerWorld = new WireRegisterWorldServerRequest
{
    Context = CreateWireContext(
        WireClusterNodeRole.World,
        WireClusterService.Communication),
    World = new WireWorldServerRegistration
    {
        WorldId = "11111111-2222-3333-4444-555555555555",
        EndpointIp = "127.0.0.1",
        EndpointPort = 1337,
        AccountLimit = 100,
        WorldGroup = "S2-Sumeria"
    }
};
AssertEqual(
    CommunicationContractValidationError.None,
    ClusterCommunicationContractValidator.Validate(registerWorld),
    "World may register a bounded typed endpoint");
registerWorld.World.EndpointPort = 0;
AssertEqual(
    CommunicationContractValidationError.InvalidEndpointPort,
    ClusterCommunicationContractValidator.Validate(registerWorld),
    "World endpoint ports are range-checked");
registerWorld.World.EndpointPort = 1337;
registerWorld.World.WorldGroup = " S2-Sumeria";
AssertEqual(
    CommunicationContractValidationError.InvalidWorldGroup,
    ClusterCommunicationContractValidator.Validate(registerWorld),
    "World groups must be trimmed bounded text");

var unregisterWorld = new WireWorldRequest
{
    Context = CreateWireContext(
        WireClusterNodeRole.World,
        WireClusterService.Communication),
    WorldId = "11111111-2222-3333-4444-555555555555"
};
AssertEqual(
    CommunicationContractValidationError.None,
    ClusterCommunicationContractValidator.Validate(unregisterWorld),
    "World may unregister only its canonical identity");

var listWorlds = new WireListWorldServersRequest
{
    Context = CreateWireContext(
        WireClusterNodeRole.Login,
        WireClusterService.Communication),
    AccountId = 42
};
AssertEqual(
    CommunicationContractValidationError.None,
    ClusterCommunicationContractValidator.Validate(listWorlds),
    "Login may request typed world/channel state");
listWorlds.Context.CallerRole = WireClusterNodeRole.World;
AssertEqual(
    CommunicationContractValidationError.InvalidCallerRole,
    ClusterCommunicationContractValidator.Validate(listWorlds),
    "World cannot request Login's server-list projection");

AssertEqual(
    WireCommunicationResultCode.Success,
    WireCommunicationResultCode.Success,
    "Communication result enum code generation");

if (CommunicationContractLimits.MaxWorldsPerResponse > 1024)
{
    throw new InvalidOperationException(
        "The world-list contract must remain explicitly bounded.");
}

if (ClusterProtocolLimits.MaxInboundMessageBytes >= 128 * 1024 * 1024)
{
    throw new InvalidOperationException(
        "The new contract must not inherit SCS's 128 MiB frame allowance.");
}

Console.WriteLine("NosGM cluster contract self-test passed.");
