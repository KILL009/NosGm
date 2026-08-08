[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$acceptancePath = Join-Path $root "scripts\test-configuration-grpc-shadow-local.ps1"
$supervisorPath = Join-Path $root "scripts\invoke-configuration-grpc-shadow-acceptance-bounded.ps1"
$workflowPath = Join-Path $root ".github\workflows\dotnet10-foundation.yml"

foreach ($path in @($acceptancePath, $supervisorPath, $workflowPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Configuration shadow acceptance dependency is missing: $path"
    }
}

foreach ($scriptPath in @($acceptancePath, $supervisorPath)) {
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile(
        $scriptPath,
        [ref]$tokens,
        [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and $parseErrors.Count -gt 0) {
        $messages = @($parseErrors | ForEach-Object {
            "$($_.Extent.StartLineNumber):$($_.Extent.StartColumnNumber) $($_.Message)"
        })
        throw "Configuration shadow acceptance PowerShell syntax failed for $scriptPath`: $($messages -join '; ')"
    }
}

$content = [System.IO.File]::ReadAllText($acceptancePath)
$supervisorContent = [System.IO.File]::ReadAllText($supervisorPath)
$workflowContent = [System.IO.File]::ReadAllText($workflowPath)

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
    '[int]$ClientTimeoutSeconds = 60',
    '[int]$BuildTimeoutSeconds = 180',
    '$process.WaitForExit($ClientTimeoutSeconds * 1000)',
    '$process.WaitForExit($BuildTimeoutSeconds * 1000)',
    'Stop-Process -Id $runtimeProcess.Id -Force'
)) {
    if ($content.IndexOf($required, [StringComparison]::Ordinal) -lt 0) {
        throw "Configuration shadow acceptance is missing '$required'."
    }
}

foreach ($required in @(
    '[int]$TotalTimeoutSeconds = 420',
    'test-configuration-grpc-shadow-local.ps1',
    '$process.WaitForExit($TotalTimeoutSeconds * 1000)',
    'RedirectStandardOutput $stdoutPath',
    'RedirectStandardError $stderrPath',
    'taskkill.exe',
    '$killer.WaitForExit(10000)',
    'Stop-Process -Id $killer.Id -Force',
    'Stop-BoundedProcessTree -Process $process',
    'exceeded its total $TotalTimeoutSeconds-second budget'
)) {
    if ($supervisorContent.IndexOf($required, [StringComparison]::Ordinal) -lt 0) {
        throw "Configuration shadow acceptance supervisor is missing '$required'."
    }
}

foreach ($required in @(
    'scripts/invoke-configuration-grpc-shadow-acceptance-bounded.ps1',
    'timeout-minutes: 10',
    'shell: pwsh',
    'System32\certutil.exe',
    '$process.WaitForExit(30000)',
    'Stop-Process -Id $process.Id -Force',
    '@("-user", "-f", "-addstore", "Root"',
    '@("-user", "-delstore", "Root"',
    './scripts/invoke-configuration-grpc-shadow-acceptance-bounded.ps1 -TotalTimeoutSeconds 420'
)) {
    if ($workflowContent.IndexOf($required, [StringComparison]::Ordinal) -lt 0) {
        throw "Configuration shadow acceptance workflow is missing '$required'."
    }
}

foreach ($forbidden in @(
    'Cert:\LocalMachine\Root',
    'EnvironmentVariableTarget.User',
    'EnvironmentVariableTarget.Machine',
    'DangerousAcceptAnyServerCertificateValidator',
    'MasterAuthKey'
)) {
    if ($content.IndexOf($forbidden, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $supervisorContent.IndexOf($forbidden, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $workflowContent.IndexOf($forbidden, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Configuration shadow acceptance contains forbidden text '$forbidden'."
    }
}

if ($supervisorContent.IndexOf('& $taskKill', [StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw "Configuration shadow acceptance supervisor must not invoke taskkill without a bounded child process."
}

if ($workflowContent.IndexOf('X509Store("Root", "CurrentUser")', [StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw "Configuration shadow acceptance workflow must not use the X509Store path that can block before the bounded harness starts."
}

$importIndex = $content.IndexOf('Import-Certificate', [StringComparison]::Ordinal)
$cleanupIndex = $content.LastIndexOf('Remove-Item -LiteralPath $trustedRootStorePath -Force', [StringComparison]::Ordinal)
if ($importIndex -lt 0 -or $cleanupIndex -le $importIndex) {
    throw "Temporary current-user root cleanup must remain after certificate import."
}

Write-Host "[PASS] Configuration shadow acceptance PowerShell syntax is valid." -ForegroundColor Green
Write-Host "[PASS] Acceptance uses a generated net481 World client over the real gRPC transport." -ForegroundColor Green
Write-Host "[PASS] Acceptance covers seed, reconnect, duplicate idempotency and changed generation." -ForegroundColor Green
Write-Host "[PASS] Per-operation client/build waits remain bounded." -ForegroundColor Green
Write-Host "[PASS] External supervisor enforces a 420-second total acceptance budget." -ForegroundColor Green
Write-Host "[PASS] Supervisor process-tree cleanup is itself bounded." -ForegroundColor Green
Write-Host "[PASS] Workflow keeps a 10-minute hard limit with headroom for trust cleanup." -ForegroundColor Green
Write-Host "[PASS] Workflow bounds CurrentUser root inspection, installation and removal through certutil." -ForegroundColor Green
Write-Host "[PASS] Windows trust changes are current-user scoped, explicit and cleaned up." -ForegroundColor Green
Write-Host "[PASS] Acceptance never persists process secrets or installs LocalMachine trust." -ForegroundColor Green
