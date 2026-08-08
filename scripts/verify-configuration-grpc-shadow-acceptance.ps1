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
    '<TargetFramework>net481</TargetFramework>',
    '/p:NosGmLegacyBuild=true',
    'GrpcClusterConfigurationTransport',
    'ClusterNodeRole.World',
    'baseline.Generation + 1UL',
    'duplicate preserves generation',
    'reconnect generation',
    'changed generation',
    'NOSGM_AUTH_GRPC_TRUSTED_ROOT_CERT_PATH = $rootCertificatePath',
    'NOSGM_AUTH_GRPC_WIRE_MODE"] = "HTTP2"',
    'requires native HTTP/2 on Windows 11 or Windows Server 2019',
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
    './scripts/invoke-configuration-grpc-shadow-acceptance-bounded.ps1 -TotalTimeoutSeconds 420'
)) {
    if ($workflowContent.IndexOf($required, [StringComparison]::Ordinal) -lt 0) {
        throw "Configuration shadow acceptance workflow is missing '$required'."
    }
}

foreach ($forbidden in @(
    'Cert:\LocalMachine\Root',
    'Cert:\CurrentUser\Root',
    'Import-Certificate',
    'certutil.exe',
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

Write-Host "[PASS] Configuration shadow acceptance PowerShell syntax is valid." -ForegroundColor Green
Write-Host "[PASS] Acceptance uses a generated net481 World client over the real gRPC transport." -ForegroundColor Green
Write-Host "[PASS] Acceptance covers seed, reconnect, duplicate idempotency and changed generation." -ForegroundColor Green
Write-Host "[PASS] Per-operation client/build waits remain bounded." -ForegroundColor Green
Write-Host "[PASS] External supervisor enforces a 420-second total acceptance budget." -ForegroundColor Green
Write-Host "[PASS] Supervisor process-tree cleanup is itself bounded." -ForegroundColor Green
Write-Host "[PASS] Workflow keeps a 10-minute hard limit with headroom for trust cleanup." -ForegroundColor Green
Write-Host "[PASS] CI exercises only native net481 HTTP/2 while the production GRPCWEB fallback remains compiled." -ForegroundColor Green
Write-Host "[PASS] Acceptance uses strict file-scoped root trust and never mutates Windows certificate stores." -ForegroundColor Green
Write-Host "[PASS] Acceptance never persists process secrets or installs LocalMachine trust." -ForegroundColor Green
