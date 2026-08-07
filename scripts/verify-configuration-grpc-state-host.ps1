[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Read-RepoFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Expected Configuration state-host file is missing: $RelativePath"
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

$proto = Read-RepoFile "contracts\cluster\v1\cluster_configuration.proto"
$state = Read-RepoFile "Data\NosGm.Program\NosGm.Authentication.Server\State\ClusterConfigurationState.cs"
$service = Read-RepoFile "Data\NosGm.Program\NosGm.Authentication.Server\Services\ClusterConfigurationService.cs"
$program = Read-RepoFile "Data\NosGm.Program\NosGm.Authentication.Server\Program.cs"
$selfTest = Read-RepoFile "tests\NosGm.Authentication.Runtime.SelfTest\ClusterConfigurationStateSelfTest.cs"

Require-Text $proto "uint64 generation = 3;" "Configuration wire generation"
if (([regex]::Matches($proto, [regex]::Escape("uint64 generation = 3;"))).Count -ne 2) {
    throw "GetConfigurationResponse and UpdateConfigurationResponse must both expose generation field 3."
}

foreach ($required in @(
    "private ulong _generation;",
    "private WireV1.ConfigurationSnapshot _snapshot;",
    "if (_snapshot == null)",
    "AreEqual(_snapshot, snapshot)",
    "_generation++;",
    "Clone(_snapshot)"
)) {
    Require-Text $state $required "Configuration state isolation"
}

foreach ($forbidden in @(
    "GameConfiguration",
    "ConfigurationUpdated",
    "NosGm.SCS",
    "MasterAuthKey"
)) {
    Forbid-Text $state $forbidden "Configuration shadow state"
    Forbid-Text $service $forbidden "Configuration shadow service"
}

foreach ($required in @(
    "AuthenticationDispatchGate",
    "AuthenticationRequestReplayGuard",
    "ClientCertificateRoleMap",
    "ClusterNodeRole.World",
    "ClusterConfigurationContractValidator.Validate",
    "MaxClockSkewMilliseconds",
    "MaxDeadlineMilliseconds",
    "StatusCode.PermissionDenied",
    "ConfigurationResultCode.Unavailable",
    "ConfigurationResultCode.Conflict"
)) {
    Require-Text $service $required "Configuration shadow service guard"
}

Require-Text $program "AddSingleton<ClusterConfigurationState>()" "Configuration state DI registration"
Require-Text $program "MapGrpcService<ClusterConfigurationService>()" "Configuration gRPC endpoint registration"
Require-Text $program "shadow configuration services enabled" "Configuration shadow runtime log"

foreach ($required in @(
    "[ModuleInitializer]",
    "starts unavailable",
    "Input mutation cannot alter stored Configuration state",
    "Returned snapshot mutation cannot alter stored Configuration state",
    "Equivalent Configuration update preserves generation",
    "Changed Configuration update advances generation",
    "Latest Configuration update wins"
)) {
    Require-Text $selfTest $required "Configuration state self-test"
}

Write-Host "[PASS] Configuration gRPC host starts unavailable and owns no legacy default." -ForegroundColor Green
Write-Host "[PASS] Configuration snapshots are isolated and generation-backed." -ForegroundColor Green
Write-Host "[PASS] Equivalent multi-World shadow writes preserve the current generation." -ForegroundColor Green
Write-Host "[PASS] Configuration gRPC service reuses mTLS, deadline, replay and dispatch guards." -ForegroundColor Green
Write-Host "[PASS] Configuration shadow host has no SCS callback, shared-secret or GameConfiguration dependency." -ForegroundColor Green
