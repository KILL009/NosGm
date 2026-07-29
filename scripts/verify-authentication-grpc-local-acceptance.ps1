[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$repositoryRoot =
    [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Read-RepositoryFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $fullPath = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Expected file was not found: $RelativePath"
    }
    return [System.IO.File]::ReadAllText($fullPath)
}

function Require {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not $Content.Contains($Expected)) {
        throw "$Name is missing '$Expected'."
    }
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

function Forbid {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Forbidden,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($Content.Contains($Forbidden)) {
        throw "$Name contains forbidden text '$Forbidden'."
    }
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

function Assert-PowerShellSyntax {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $tokens = $null
    $parseErrors = $null
    $fullPath = Join-Path $repositoryRoot $RelativePath
    [System.Management.Automation.Language.Parser]::ParseFile(
        $fullPath,
        [ref]$tokens,
        [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and $parseErrors.Count -gt 0) {
        $messages = @($parseErrors | ForEach-Object {
            "$($_.Extent.StartLineNumber):$($_.Extent.StartColumnNumber) $($_.Message)"
        })
        throw "PowerShell syntax failed for $RelativePath`: $($messages -join '; ')"
    }

    Write-Host "[PASS] PowerShell syntax: $RelativePath" `
        -ForegroundColor Green
}

foreach ($scriptPath in @(
    "scripts\new-local-authentication-certificates.ps1",
    "scripts\start-modern-login-local.ps1",
    "scripts\test-authentication-grpc-local.ps1",
    "scripts\test-modern-login-readiness.ps1"
)) {
    Assert-PowerShellSyntax -RelativePath $scriptPath
}

$generator = Read-RepositoryFile `
    "scripts\new-local-authentication-certificates.ps1"
$acceptance = Read-RepositoryFile `
    "scripts\test-authentication-grpc-local.ps1"
$startup = Read-RepositoryFile `
    "scripts\start-modern-login-local.ps1"
$readiness = Read-RepositoryFile `
    "scripts\test-modern-login-readiness.ps1"
$selfTest = Read-RepositoryFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\Program.cs"
$documentation = Read-RepositoryFile `
    "docs\scs-transport-migration.md"

Require $generator "New-SecureRandomPassword" `
    "Local PKCS#12 passwords use cryptographic randomness"
Require $generator "Export-Clixml" `
    "Local PKCS#12 passwords are persisted only through current-user DPAPI"
Require $generator "SetAccessRuleProtection(`$true, `$false)" `
    "Local certificate directory disables inherited ACLs"
Require $generator "Cert:\CurrentUser\Root" `
    "Development root trust is scoped to the current Windows user"
Require $generator "if (`$TrustRootCertificate)" `
    "Development root installation requires an explicit switch"
Require $generator "1.3.6.1.5.5.7.3.1" `
    "Server certificate receives only the server-authentication EKU"
Require $generator "1.3.6.1.5.5.7.3.2" `
    "Role certificates receive only the client-authentication EKU"
Require $generator "IPAddress=127.0.0.1" `
    "Server certificate contains the loopback IP SAN"
Require $generator "foreach (`$role in @(`"AuthBridge`", `"Login`", `"World`"))" `
    "Three distinct role certificates are generated"
Forbid $generator "PfxPassword =" `
    "The public manifest never contains a plaintext PKCS#12 password"

Require $startup '[ValidateSet("SCS", "GRPC")]' `
    "Local startup keeps one explicit authentication selector"
Require $startup '[string]$AuthenticationTransport = "SCS"' `
    "SCS remains the local startup default"
Require $startup "Windows 11 or Windows Server 2019 or later" `
    "Explicit gRPC startup fails early on unsupported Windows versions"
Require $startup "AuthenticationGrpc" `
    "Explicit gRPC startup records the authentication runtime"
Require $startup "ProcessEnvironment" `
    "Each child receives a separately scoped environment"
Require $startup "NOSGM_AUTH_GRPC_CLIENT_CERT_PATH" `
    "Caller certificate path is passed only through process memory"
Require $startup "authbridge-local-1" `
    "Master AuthBridge gets its own caller identity"
Require $startup "login-local-1" `
    "Login gets its own caller identity"
Require $startup "world-local-1" `
    "World gets its own caller identity"
Require $startup '$authenticationRuntimeEnvironment.Clear()' `
    "Authentication server plaintext environment values are cleared"
Forbid $startup 'EnvironmentVariableTarget.User' `
    "Startup never persists authentication values in the user environment"
Forbid $startup 'EnvironmentVariableTarget.Machine' `
    "Startup never persists authentication values in the machine environment"
Forbid $startup "DangerousAcceptAnyServerCertificateValidator" `
    "Startup provides no certificate-validation bypass"

Require $acceptance "Wait-AuthenticationRuntime" `
    "Live acceptance waits for the real Kestrel listener"
Require $acceptance "Import-Clixml" `
    "Live acceptance uses the protected credential bundle"
Require $acceptance "& dotnet `$selfTestAssembly --live" `
    "Live acceptance executes the networked self-test mode"
Require $acceptance "Restore-ProcessEnvironment" `
    "Live acceptance restores all temporary environment values"

Require $selfTest "RunLiveGrpcAcceptanceAsync" `
    "Self-test exposes a live network acceptance mode"
Require $selfTest "Live second Login stage preserves the SessionID" `
    "Live acceptance protects stable SessionID reuse"
Require $selfTest "Live World permit cannot be replayed" `
    "Live acceptance protects one-use World permits"
Require $selfTest "StatusCode.PermissionDenied" `
    "Live acceptance verifies certificate-role authorization"

Require $readiness "Port.AuthenticationGrpc" `
    "Readiness verifies the optional gRPC runtime port"
Require $documentation "new-local-authentication-certificates.ps1" `
    "Migration documentation explains local certificate provisioning"
Require $documentation "test-authentication-grpc-local.ps1" `
    "Migration documentation exposes the live mTLS acceptance command"

Write-Host `
    "NosGM local gRPC certificate, process-isolation and live-acceptance contracts passed." `
    -ForegroundColor Green
