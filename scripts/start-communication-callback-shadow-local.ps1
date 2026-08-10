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

$authorityScript =
    Join-Path $PSScriptRoot "start-penaltyrefresh-grpc-authority-local.ps1"
if (-not (Test-Path -LiteralPath $authorityScript -PathType Leaf)) {
    throw "The final PenaltyRefresh gRPC authority startup script is missing: $authorityScript"
}

Write-Host "[COMPAT] PenaltyRefresh has completed its callback cutover." -ForegroundColor Yellow
Write-Host "[COMPAT] This command now starts PenaltyRefresh=gRPC authority while the remaining callbacks stay SCS-authoritative with typed observation." -ForegroundColor Yellow

$parameters = @{
    SkipBuild = $SkipBuild
    SkipLauncher = $SkipLauncher
    SkipPortalBuild = $SkipPortalBuild
    ConfigureUrlAcl = $ConfigureUrlAcl
    EnableConfigurationRuntimeControl = $EnableConfigurationRuntimeControl
    AuthenticationGrpcWireMode = $AuthenticationGrpcWireMode
    AuthenticationGrpcPort = $AuthenticationGrpcPort
    StartupTimeoutSeconds = $StartupTimeoutSeconds
    WorldPort = $WorldPort
    BridgePort = $BridgePort
    PortalPort = $PortalPort
}
if (-not [string]::IsNullOrWhiteSpace($AuthenticationCertificateManifest)) {
    $parameters["AuthenticationCertificateManifest"] =
        $AuthenticationCertificateManifest
}

& $authorityScript @parameters
