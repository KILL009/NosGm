[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$acceptancePath = Join-Path $root "scripts\test-configuration-grpc-shadow-local.ps1"
if (-not (Test-Path -LiteralPath $acceptancePath -PathType Leaf)) {
    throw "Configuration shadow acceptance script is missing."
}

$tokens = $null
$parseErrors = $null
[System.Management.Automation.Language.Parser]::ParseFile(
    $acceptancePath,
    [ref]$tokens,
    [ref]$parseErrors) | Out-Null
if ($null -ne $parseErrors -and $parseErrors.Count -gt 0) {
    $messages = @($parseErrors | ForEach-Object {
        "$($_.Extent.StartLineNumber):$($_.Extent.StartColumnNumber) $($_.Message)"
    })
    throw "Configuration shadow acceptance PowerShell syntax failed: $($messages -join '; ')"
}

$content = [System.IO.File]::ReadAllText($acceptancePath)
foreach ($required in @(
    '[switch]$TrustRootForCurrentUser',
    'Cert:\CurrentUser\Root',
    '$installedRootByTest = $false',
    'Import-Certificate',
    'Remove-Item -LiteralPath $trustedRootStorePath -Force',
    '<TargetFramework>net481</TargetFramework>',
    '/p:NosGmLegacyBuild=true',
    'GrpcClusterConfigurationTransport',
    'ClusterNodeRole.World',
    'baseline.Generation + 1UL',
    'duplicate preserves generation',
    'reconnect generation',
    'changed generation',
    '$modes.Add("GRPCWEB")',
    '$modes.Add("HTTP2")',
    'Restore-ProcessEnvironment',
    'Stop-Process -Id $runtimeProcess.Id -Force'
)) {
    if ($content.IndexOf($required, [StringComparison]::Ordinal) -lt 0) {
        throw "Configuration shadow acceptance is missing '$required'."
    }
}

foreach ($forbidden in @(
    'Cert:\LocalMachine\Root',
    'EnvironmentVariableTarget.User',
    'EnvironmentVariableTarget.Machine',
    'DangerousAcceptAnyServerCertificateValidator',
    'MasterAuthKey'
)) {
    if ($content.IndexOf($forbidden, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Configuration shadow acceptance contains forbidden text '$forbidden'."
    }
}

$importIndex = $content.IndexOf('Import-Certificate', [StringComparison]::Ordinal)
$cleanupIndex = $content.LastIndexOf('Remove-Item -LiteralPath $trustedRootStorePath -Force', [StringComparison]::Ordinal)
if ($importIndex -lt 0 -or $cleanupIndex -le $importIndex) {
    throw "Temporary current-user root cleanup must remain after certificate import."
}

Write-Host "[PASS] Configuration shadow acceptance PowerShell syntax is valid." -ForegroundColor Green
Write-Host "[PASS] Acceptance uses a generated net481 World client over the real gRPC transport." -ForegroundColor Green
Write-Host "[PASS] Acceptance covers seed, reconnect, duplicate idempotency and changed generation." -ForegroundColor Green
Write-Host "[PASS] Windows trust changes are current-user scoped, explicit and cleaned up." -ForegroundColor Green
Write-Host "[PASS] Acceptance never persists process secrets or installs LocalMachine trust." -ForegroundColor Green
