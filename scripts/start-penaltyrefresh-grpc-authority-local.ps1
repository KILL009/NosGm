[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$SkipLauncher,
    [switch]$SkipPortalBuild,
    [switch]$ConfigureUrlAcl,
    [switch]$EnableConfigurationRuntimeControl,
    [ValidateSet("AUTO", "HTTP2", "GRPCWEB")]
    [string]$AuthenticationGrpcWireMode = "AUTO",
    [string]$AuthenticationCertificateManifest,
    [ValidateRange(1024, 65535)]
    [int]$AuthenticationGrpcPort = 7443,
    [ValidateRange(10, 180)]
    [int]$StartupTimeoutSeconds = 60,
    [ValidateRange(1, 65535)]
    [int]$WorldPort = 1337,
    [ValidateRange(1, 65535)]
    [int]$BridgePort = 8081,
    [ValidateRange(1024, 65535)]
    [int]$PortalPort = 5080
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "PenaltyRefresh gRPC authority startup requires Windows."
}

$root = Split-Path -Parent $PSScriptRoot
$startScript = Join-Path $PSScriptRoot "start-modern-login-local.ps1"
$statePath = Join-Path $root "artifacts\modern-login-local\processes.json"
if (-not (Test-Path -LiteralPath $startScript -PathType Leaf)) {
    throw "The normal NosGM local startup script is missing: $startScript"
}

$featureVariables = @(
    "NOSGM_COMMUNICATION_GRPC_USE_EXISTING_IDENTITY_FALLBACK",
    "NOSGM_COMMUNICATION_GRPC_CALLBACKS_ENABLED",
    "NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED",
    "NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_ENABLED"
)
$dedicatedIdentityVariables = @(
    "NOSGM_COMMUNICATION_GRPC_URL",
    "NOSGM_COMMUNICATION_GRPC_CLIENT_CERT_PATH",
    "NOSGM_COMMUNICATION_GRPC_CLIENT_CERT_PASSWORD",
    "NOSGM_COMMUNICATION_GRPC_TRUSTED_ROOT_CERT_PATH",
    "NOSGM_COMMUNICATION_GRPC_CALLER_INSTANCE_ID",
    "NOSGM_COMMUNICATION_GRPC_SETUP_DEADLINE_MILLISECONDS",
    "NOSGM_COMMUNICATION_GRPC_WIRE_MODE",
    "NOSGM_COMMUNICATION_GRPC_CALLBACK_CURSOR_PATH",
    "NOSGM_COMMUNICATION_GRPC_CALLBACK_RECONNECT_INITIAL_MILLISECONDS",
    "NOSGM_COMMUNICATION_GRPC_CALLBACK_RECONNECT_MAXIMUM_MILLISECONDS",
    "NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PATH",
    "NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PASSWORD",
    "NOSGM_COMMUNICATION_GRPC_MASTER_INSTANCE_ID",
    "NOSGM_COMMUNICATION_GRPC_DEADLINE_MILLISECONDS",
    "NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_QUEUE_CAPACITY",
    "NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_STOP_TIMEOUT_MILLISECONDS"
)

$managedVariables = @($featureVariables + $dedicatedIdentityVariables)
$previousEnvironment = @{}
foreach ($name in $managedVariables) {
    $previousEnvironment[$name] =
        [Environment]::GetEnvironmentVariable(
            $name,
            [EnvironmentVariableTarget]::Process)
}

try {
    foreach ($name in $dedicatedIdentityVariables) {
        [Environment]::SetEnvironmentVariable(
            $name,
            $null,
            [EnvironmentVariableTarget]::Process)
    }

    # Login and World subscribe through the role-separated identities already
    # created by the normal GRPC stack. PenaltyRefresh is applied directly by
    # the typed dispatcher; APPLY=false deliberately keeps every other callback
    # in observation-only mode until its own authority slice.
    [Environment]::SetEnvironmentVariable(
        "NOSGM_COMMUNICATION_GRPC_USE_EXISTING_IDENTITY_FALLBACK",
        "true",
        [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        "NOSGM_COMMUNICATION_GRPC_CALLBACKS_ENABLED",
        "true",
        [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        "NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED",
        "false",
        [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        "NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_ENABLED",
        "true",
        [EnvironmentVariableTarget]::Process)

    $startParameters = @{
        SkipBuild = $SkipBuild
        SkipLauncher = $SkipLauncher
        SkipPortalBuild = $SkipPortalBuild
        ConfigureUrlAcl = $ConfigureUrlAcl
        EnableConfigurationRuntimeControl = $EnableConfigurationRuntimeControl
        AuthenticationTransport = "GRPC"
        AuthenticationGrpcWireMode = $AuthenticationGrpcWireMode
        AuthenticationGrpcPort = $AuthenticationGrpcPort
        StartupTimeoutSeconds = $StartupTimeoutSeconds
        WorldPort = $WorldPort
        BridgePort = $BridgePort
        PortalPort = $PortalPort
    }
    if (-not [string]::IsNullOrWhiteSpace($AuthenticationCertificateManifest)) {
        $startParameters["AuthenticationCertificateManifest"] =
            $AuthenticationCertificateManifest
    }

    Write-Host "[PENALTY-GRPC] Starting final PenaltyRefresh authority stack." -ForegroundColor Cyan
    Write-Host "[PENALTY-GRPC] PenaltyRefresh=gRPC; remaining callbacks=SCS with typed observation." -ForegroundColor Yellow
    & $startScript @startParameters

    if (-not (Test-Path -LiteralPath $statePath -PathType Leaf)) {
        throw "The PenaltyRefresh authority stack started without writing runtime state."
    }

    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    if ($state.SchemaVersion -ne 2 -or $null -eq $state.Processes) {
        throw "The PenaltyRefresh authority stack produced an unsupported runtime state."
    }
    $state | Add-Member -NotePropertyName CommunicationCallbackMode -NotePropertyValue "PenaltyRefreshAuthority" -Force
    $state | Add-Member -NotePropertyName PenaltyRefreshCallbackAuthority -NotePropertyValue "gRPC" -Force
    $state | Add-Member -NotePropertyName PenaltyRefreshCallbackFallback -NotePropertyValue $null -Force
    $state | Add-Member -NotePropertyName RemainingCommunicationCallbackAuthority -NotePropertyValue "SCS" -Force
    $state | Add-Member -NotePropertyName CommunicationCallbackIdentitySource -NotePropertyValue "existing role-separated gRPC identities" -Force
    $state | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statePath -Encoding UTF8

    Write-Host ""
    Write-Host "PenaltyRefresh gRPC authority stack is running." -ForegroundColor Green
    Write-Host "PenaltyRefresh: typed gRPC only, no SCS callback fallback."
    Write-Host "Other Communication callbacks: existing SCS authority plus typed observation."
    Write-Host "Stop with: ./scripts/stop-modern-login-local.ps1"
}
finally {
    foreach ($name in $managedVariables) {
        [Environment]::SetEnvironmentVariable(
            $name,
            $previousEnvironment[$name],
            [EnvironmentVariableTarget]::Process)
    }
}
