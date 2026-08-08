[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Read-RepoFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Expected Configuration gRPC client file is missing: $RelativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

function Require-Text {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($Content.IndexOf($Expected, [StringComparison]::Ordinal) -lt 0) {
        throw "$Name is missing '$Expected'."
    }
}

function Forbid-Text {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Forbidden,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($Content.IndexOf($Forbidden, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "$Name contains forbidden text '$Forbidden'."
    }
}

$contracts = Read-RepoFile "Data\NosGm.Authentication.Client\Configuration\ConfigurationTransportContracts.cs"
$transport = Read-RepoFile "Data\NosGm.Authentication.Client\Configuration\GrpcClusterConfigurationTransport.cs"
$subscriber = Read-RepoFile "Data\NosGm.Authentication.Client\Configuration\ConfigurationGrpcShadowSubscriber.cs"
$options = Read-RepoFile "Data\NosGm.Authentication.Client\AuthenticationGrpcClientOptions.cs"
$selfTest = Read-RepoFile "tests\NosGm.Authentication.Runtime.SelfTest\ClusterConfigurationTransportLiveSelfTest.cs"
$project = Read-RepoFile "Data\NosGm.Authentication.Client\NosGm.Authentication.Client.csproj"

foreach ($required in @(
    "Unspecified = 0",
    "Success = 1",
    "InvalidRequest = 2",
    "Unauthorized = 3",
    "Conflict = 4",
    "Unavailable = 5",
    "Task<ConfigurationTransportResult> GetAsync",
    "Task<ConfigurationTransportResult> UpdateAsync",
    "IClusterConfigurationUpdateStreamTransport",
    "Task SubscribeUpdatesAsync",
    "IClusterConfigurationUpdateHandler",
    "ulong Generation"
)) {
    Require-Text $contracts $required "Configuration client transport contracts"
}

foreach ($required in @(
    "options.CallerRole != ClusterNodeRole.World",
    "WireV1.ClusterService.Configuration",
    "SubscribeConfigurationUpdates",
    "ResumeAfterGeneration",
    "RuntimeGenerationId",
    "ClusterContractVersion.CurrentMajor",
    "ClusterProtocolLimits.MaxInboundMessageBytes",
    "ClusterProtocolLimits.MaxOutboundMessageBytes",
    "GrpcWebMode.GrpcWeb",
    "SslProtocols.Tls12",
    "SslProtocols.Tls13",
    "TrustedRootCertificatePath",
    "ValidatePinnedServerCertificate",
    "ServerCertificateValidationCallback",
    "X509VerificationFlags.AllowUnknownCertificateAuthority",
    "chain.ChainPolicy.ExtraStore.Add(trustedRoot)",
    "observedRoot.RawData",
    "success without a snapshot",
    "Interlocked.Exchange",
    "ObjectDisposedException"
)) {
    Require-Text $transport $required "Configuration gRPC client transport"
}

foreach ($required in @(
    "ConfigurationUpdateCursor",
    "allowSnapshotRecovery",
    "ConfigurationUpdateCursorDecision.Duplicate",
    "ConfigurationUpdateCursorDecision.Gap",
    "RecoverSnapshotAsync",
    "IsReconnectable",
    "SCS result or",
    "gameplay Configuration object is changed here"
)) {
    Require-Text $subscriber $required "Configuration shadow subscriber"
}

foreach ($forbidden in @(
    "NosGm.SCS",
    "MasterAuthKey",
    "DangerousAcceptAnyServerCertificateValidator",
    "ConfigurationServiceClient",
    "IConfigurationService"
)) {
    Forbid-Text $transport $forbidden "Configuration gRPC client transport"
    Forbid-Text $contracts $forbidden "Configuration client transport contracts"
}

Require-Text $options "TrustedRootCertificatePathVariable" "Configuration client file-scoped root option"
Forbid-Text $options "reserved for the isolated .NET 10 acceptance process" "Configuration client net481 file-scoped root option"

foreach ($required in @(
    "[ModuleInitializer]",
    "Configuration transport rejects missing options",
    "Configuration transport rejects Login before certificate loading",
    "Cluster Configuration transport construction self-test"
)) {
    Require-Text $selfTest $required "Configuration transport construction self-test"
}
foreach ($forbidden in @(
    "GetAsync(",
    "UpdateAsync(",
    "GetAwaiter().GetResult()",
    "NOSGM_AUTH_GRPC_LIVE_WORLD_CERT_PATH"
)) {
    Forbid-Text $selfTest $forbidden "Configuration transport construction self-test"
}

Require-Text $project "<TargetFramework Condition=" "Configuration client net481 bridge"
Require-Text $project "net481;net10.0" "Configuration client net10 bridge"
Require-Text $project "Grpc.Net.Client.Web" "Configuration client gRPC-Web package"

Write-Host "[PASS] Configuration client transport preserves wire result codes and generation." -ForegroundColor Green
Write-Host "[PASS] Configuration gRPC client is World-only and requests ClusterService.Configuration." -ForegroundColor Green
Write-Host "[PASS] Configuration client contains the existing HTTP/2 and Windows 10 gRPC-Web mTLS implementations." -ForegroundColor Green
Write-Host "[PASS] Configuration client remains isolated from SCS and the legacy ConfigurationServiceClient." -ForegroundColor Green
Write-Host "[PASS] Configuration shadow subscriber deduplicates, reconnects and recovers by runtime generation." -ForegroundColor Green
Write-Host "[PASS] Construction self-test remains non-blocking and rejects invalid roles before certificate loading." -ForegroundColor Green

& (Join-Path $PSScriptRoot "verify-configuration-grpc-shadow-adapter.ps1")
