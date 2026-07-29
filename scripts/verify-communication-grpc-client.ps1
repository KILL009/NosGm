[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot =
    [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Read-RepositoryFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $fullPath = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Expected communication client file was not found: $RelativePath"
    }

    return [System.IO.File]::ReadAllText($fullPath)
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Content,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedText,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if (-not $Content.Contains($ExpectedText)) {
        throw "$Name is missing '$ExpectedText'."
    }

    Write-Host "[PASS] $Name" -ForegroundColor Green
}

function Assert-NotContains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Content,

        [Parameter(Mandatory = $true)]
        [string]$ForbiddenText,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if ($Content.Contains($ForbiddenText)) {
        throw "$Name contains forbidden text '$ForbiddenText'."
    }

    Write-Host "[PASS] $Name" -ForegroundColor Green
}

$project = Read-RepositoryFile `
    "Data\NosGm.Authentication.Client\NosGm.Authentication.Client.csproj"
$masterProject = Read-RepositoryFile `
    "Data\NosGm.Master.Library\NosGm.Master.Library.csproj"
$contracts = Read-RepositoryFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationTransportContracts.cs"
$mode = Read-RepositoryFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationTransportMode.cs"
$router = Read-RepositoryFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationTransportRouter.cs"
$grpc = Read-RepositoryFile `
    "Data\NosGm.Authentication.Client\Communication\GrpcClusterCommunicationTransport.cs"
$scs = Read-RepositoryFile `
    "Data\NosGm.Master.Library\Client\ScsClusterCommunicationTransport.cs"
$selfTest = Read-RepositoryFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationTransportSelfTest.cs"
$legacyClient = Read-RepositoryFile `
    "Data\NosGm.Master.Library\Client\CommunicationServiceClient.cs"
$pulseHandler = Read-RepositoryFile `
    "Data\NosGm.Handler\PacketHandler\Basic\PulsePacketHandler.cs"
$selectHandler = Read-RepositoryFile `
    "Data\NosGm.Handler\PacketHandler\CharScreen\SelectCharacterPacketHandler.cs"

Assert-Contains $project `
    '<TargetFrameworks Condition="''$(NosGmLegacyBuild)'' != ''true''">net481;net10.0</TargetFrameworks>' `
    "Communication client bridge targets net481 and .NET 10"
Assert-Contains $project 'Grpc.Net.Client.Web' `
    "Communication client includes the Windows 10 gRPC-Web transport"
Assert-Contains $project 'System.Net.Http.WinHttpHandler' `
    "Legacy callers retain native HTTP/2 support where Windows allows it"
Assert-Contains $masterProject `
    '<Compile Include="Client\ScsClusterCommunicationTransport.cs" />' `
    "The legacy Master library compiles its typed SCS adapter"

Assert-Contains $contracts 'public interface IClusterCommunicationTransport' `
    "Communication exposes a typed transport abstraction"
foreach ($method in @(
    'RegisterAccountLoginAsync',
    'IsAccountSessionRegisteredAsync',
    'IsLoginPermittedAsync',
    'IsAccountConnectedAsync',
    'ConnectAccountAsync',
    'DisconnectAccountAsync',
    'PulseAccountAsync',
    'ConnectCharacterAsync',
    'DisconnectCharacterAsync',
    'RegisterWorldServerAsync',
    'UnregisterWorldServerAsync',
    'ListWorldServersAsync'
)) {
    Assert-Contains $contracts $method `
        "Communication bridge includes $method"
}
Assert-NotContains $contracts 'object[]' `
    "Communication bridge contains no dynamic CLR invocation payload"

Assert-Contains $mode 'NOSGM_COMMUNICATION_TRANSPORT' `
    "All communication callers share one explicit selector"
Assert-Contains $mode 'return CommunicationTransportMode.Scs;' `
    "SCS remains the default communication rollback transport"
Assert-Contains $mode 'return CommunicationTransportMode.Grpc;' `
    "gRPC requires explicit communication selection"
Assert-Contains $mode 'must be SCS or GRPC' `
    "Unknown communication transport values fail closed"

Assert-Contains $router '_selectedTransport' `
    "The communication router selects exactly one transport"
Assert-NotContains $router 'catch (' `
    "Communication transport failures are never automatically retried"
Assert-NotContains $router 'Task.WhenAll' `
    "Communication side effects are never mirrored"
Assert-NotContains $router 'ContinueWith' `
    "Communication dispatch has no hidden fallback continuation"

Assert-Contains $grpc 'WireV1.ClusterCommunication' `
    "The client uses generated typed communication stubs"
Assert-Contains $grpc `
    'RequestedService = WireV1.ClusterService.Communication' `
    "Communication requests use the correct service context"
Assert-Contains $grpc 'deadline: deadline' `
    "Every communication call has a transport deadline"
Assert-Contains $grpc 'GrpcWebMode.GrpcWeb' `
    "Windows 10 callers use binary gRPC-Web"
Assert-Contains $grpc 'ClientCertificates.Add(certificate)' `
    "Communication callers present their mTLS identity"
Assert-Contains $grpc 'X509KeyStorageFlags.UserKeySet' `
    "Windows Schannel receives a current-user private key"
Assert-Contains $grpc 'X509KeyStorageFlags.EphemeralKeySet' `
    "Non-Windows callers retain ephemeral private keys"
Assert-Contains $grpc 'ClusterNodeRole.Login' `
    "Login is an allowed communication caller"
Assert-Contains $grpc 'ClusterNodeRole.World' `
    "World is an allowed communication caller"
Assert-NotContains $grpc 'DangerousAcceptAnyServerCertificateValidator' `
    "Communication callers retain certificate validation"
Assert-NotContains $grpc 'Task.WhenAll' `
    "The gRPC client never duplicates communication calls"
Assert-NotContains $grpc 'Retry' `
    "Stateful communication calls have no automatic retry policy"

Assert-Contains $scs 'IClusterCommunicationTransport' `
    "The legacy SCS path implements the same typed transport abstraction"
Assert-Contains $scs 'Func<ICommunicationService>' `
    "The SCS adapter resolves only the existing connected service proxy"
Assert-Contains $scs '_serviceProxy().RegisterAccountLogin' `
    "Account registration is routed through the typed SCS adapter"
Assert-Contains $scs '_serviceProxy().ConnectAccount' `
    "World attachment is routed through the typed SCS adapter"
Assert-Contains $scs '_serviceProxy().RegisterWorldServer' `
    "World registration is routed through the typed SCS adapter"
Assert-Contains $scs `
    'Legacy SCS returns a rendered NsTeST packet' `
    "The SCS adapter refuses to reinterpret client packet bytes as typed state"
Assert-NotContains $scs 'catch (' `
    "The SCS adapter does not swallow transport failures"
Assert-NotContains $scs 'Task.WhenAll' `
    "The SCS adapter never mirrors calls"

Assert-Contains $selfTest `
    'Communication defaults to the SCS rollback transport' `
    "The self-test covers the default rollback path"
Assert-Contains $selfTest `
    'Communication gRPC failure is not retried through SCS' `
    "The self-test proves failures do not cross transports"
Assert-Contains $selfTest `
    'The selected failing transport is called exactly once' `
    "The self-test proves one stateful dispatch"

Assert-Contains $legacyClient 'CommunicationTransportModeParser.ParseEnvironment()' `
    "Production callers parse one communication selector before connecting"
Assert-Contains $legacyClient 'new ScsClusterCommunicationTransport' `
    "Production SCS calls use the typed adapter"
Assert-Contains $legacyClient 'new CommunicationTransportRouter' `
    "Production calls enter the single-transport router"
Assert-Contains $legacyClient `
    'Communication gRPC cutover is blocked until callback, cross-server, and administrative state slices are migrated together.' `
    "Partial gRPC activation fails before creating split-brain state"
Assert-NotContains $legacyClient 'new GrpcClusterCommunicationTransport' `
    "Production gRPC communication remains blocked until coordinated cutover"
Assert-Contains $legacyClient 'PulseAccount(long accountId, int sessionId)' `
    "The client exposes tuple-bound World pulses"
Assert-Contains $legacyClient 'long accountId,' `
    "The client exposes account-bound character coordination overloads"
Assert-Contains $legacyClient 'NormalizeNsTeSTPacketLayout' `
    "Exact NsTeST rendering remains inside the Login-facing adapter"

Assert-Contains $pulseHandler 'Session.SessionId' `
    "World pulses preserve the stable SessionID"
Assert-Contains $selectHandler 'Session.Account.AccountId' `
    "Character selection carries its authenticated account identity"
Assert-Contains $selectHandler 'Session.SessionId' `
    "Character selection carries its stable SessionID"

Write-Host `
    "NosGM communication transport abstraction, guarded SCS integration and split-brain prevention contract passed." `
    -ForegroundColor Green
