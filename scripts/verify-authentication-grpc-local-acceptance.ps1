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
$managedGenerator = Read-RepositoryFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\LocalAuthenticationCertificateGenerator.cs"
$workflow = Read-RepositoryFile `
    ".github\workflows\dotnet10-foundation.yml"
$documentation = Read-RepositoryFile `
    "docs\scs-transport-migration.md"

Require $generator "New-SecureRandomPassword" `
    "Local PKCS#12 passwords use cryptographic randomness"
Require $generator '[int]$KeyLength = 3072' `
    "Operator certificates retain the stronger 3072-bit RSA default"
Require $generator "Export-Clixml" `
    "Local PKCS#12 passwords are persisted only through current-user DPAPI"
Require $generator "SetAccessRuleProtection(`$true, `$false)" `
    "Local certificate directory disables inherited ACLs"
Require $generator "Cert:\CurrentUser\Root" `
    "Development root trust is scoped to the current Windows user"
Require $generator "Install-CurrentUserTrustedRoot" `
    "Root installation uses the non-interactive current-user certificate-store API"
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
Require $generator '$env:GITHUB_ACTIONS -ne "true"' `
    "Managed certificate generation is restricted to isolated GitHub Actions"
Require $generator "--generate-ci-certificate-bundle" `
    "The CI path uses the managed .NET certificate generator"
Require $managedGenerator "RandomNumberGenerator.GetBytes" `
    "Managed CI PKCS#12 passwords and serials use cryptographic randomness"
Require $managedGenerator "SubjectAlternativeNameBuilder" `
    "Managed CI server identity contains an explicit SAN"
Require $managedGenerator "ServerAuthenticationOid" `
    "Managed CI server identity is restricted to server authentication"
Require $managedGenerator "ClientAuthenticationOid" `
    "Managed CI role identities are restricted to client authentication"
Require $managedGenerator "X509Certificate2Collection" `
    "Managed CI PFX files carry the issuing chain for Schannel selection"
Require $managedGenerator "File.WriteAllText(" `
    "Managed CI certificate manifest is written independently of transient output"
Forbid $managedGenerator "PfxPassword =" `
    "Managed CI certificate manifest has no plaintext password field"
Require $workflow "-ManagedCertificateGenerator -KeyLength 2048" `
    "Live CI acceptance avoids the blocking Windows PKI provider"
Require $workflow "-UseFileScopedRootTrust" `
    "Live CI acceptance never changes the runner trust store"

Require $startup '[ValidateSet("SCS", "GRPC")]' `
    "Local startup keeps one explicit authentication selector"
Require $startup '[string]$AuthenticationTransport = "SCS"' `
    "SCS remains the local startup default"
Require $startup '[ValidateSet("AUTO", "HTTP2", "GRPCWEB")]' `
    "Local startup exposes one explicit gRPC wire-mode selector"
Require $startup 'return "GRPCWEB"' `
    "Windows 10 selects gRPC-Web before any stateful call begins"
Require $startup "Windows 11 or Windows Server 2019 or later" `
    "An unsupported forced HTTP/2 selection fails early"
Require $startup "Resolve-DotNet10Executable" `
    "Local startup resolves PATH, global, and NosGM-local .NET 10 installations"
Require $startup "AuthenticationGrpc" `
    "Explicit gRPC startup records the authentication runtime"
Require $startup "ProcessEnvironment" `
    "Each child receives a separately scoped environment"
Require $startup "NOSGM_AUTH_GRPC_CLIENT_CERT_PATH" `
    "Caller certificate path is passed only through process memory"
Require $startup "NOSGM_AUTH_GRPC_WIRE_MODE" `
    "Caller wire mode is scoped to each selected gRPC process"
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
Require $acceptance "Resolve-DotNet10Executable" `
    "Live acceptance discovers the NosGM-local .NET 10 SDK"
Require $acceptance "& `$dotnetExecutable `$selfTestAssembly --live" `
    "Live acceptance executes the networked self-test mode"
Require $acceptance '"HTTP2"' `
    "Live acceptance exercises native HTTP/2"
Require $acceptance '"GRPCWEB"' `
    "Live acceptance exercises the Windows 10 gRPC-Web path"
Require $acceptance "Test-NetFrameworkGrpcHttp2Support" `
    "Live acceptance selects the same OS-compatible wire mode as the complete stack"
Require $acceptance "UseFileScopedRootTrust" `
    "Isolated acceptance supports explicit private-root pinning"
Require $acceptance "NOSGM_AUTH_GRPC_TRUSTED_ROOT_CERT_PATH" `
    "The isolated server and callers share one absolute root path"
Require $acceptance "Microsoft.AspNetCore.Server.Kestrel.Https" `
    "Isolated acceptance captures bounded TLS handshake diagnostics"
Require $acceptance "[SKIP] Native HTTP/2 is unavailable" `
    "Windows 10 acceptance does not fail before exercising its supported wire mode"
Forbid $acceptance "complete NosGM gRPC path requires" `
    "The isolated .NET 10 acceptance is not blocked on Windows 10"
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
Require $readiness "Authentication.GrpcWireMode" `
    "Readiness validates the preselected gRPC wire mode"
Require $documentation "new-local-authentication-certificates.ps1" `
    "Migration documentation explains local certificate provisioning"
Require $documentation "test-authentication-grpc-local.ps1" `
    "Migration documentation exposes the live mTLS acceptance command"
Require $documentation "GRPCWEB" `
    "Migration documentation explains Windows 10 compatibility"

Write-Host `
    "NosGM local gRPC certificate, process-isolation and live-acceptance contracts passed." `
    -ForegroundColor Green
