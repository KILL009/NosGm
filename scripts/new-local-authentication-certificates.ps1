[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [switch]$TrustRootCertificate,
    [switch]$ManagedCertificateGenerator,
    [ValidateSet(2048, 3072, 4096)]
    [int]$KeyLength = 3072
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "Local NosGM authentication certificates can only be created on Windows."
}

if (-not $ManagedCertificateGenerator) {
    foreach ($commandName in @(
        "New-SelfSignedCertificate",
        "Export-Certificate",
        "Export-PfxCertificate"
    )) {
        if (-not (Get-Command $commandName -ErrorAction SilentlyContinue)) {
            throw "$commandName is unavailable. Install the Windows PKI PowerShell module."
        }
    }
}

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $root "artifacts\authentication-grpc-local"
}
$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)

if (Test-Path -LiteralPath $outputRoot) {
    if (@(Get-ChildItem -LiteralPath $outputRoot -Force).Count -gt 0) {
        throw "The certificate output directory is not empty: $outputRoot. Rotate or remove the previous local bundle explicitly before creating another one."
    }
}
else {
    New-Item -ItemType Directory -Path $outputRoot | Out-Null
}

function Protect-OutputDirectory {
    param([Parameter(Mandatory = $true)][string]$Path)

    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    try {
        $security = New-Object Security.AccessControl.DirectorySecurity
        $security.SetAccessRuleProtection($true, $false)
        $inheritance =
            [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor
            [Security.AccessControl.InheritanceFlags]::ObjectInherit
        $rule = New-Object Security.AccessControl.FileSystemAccessRule(
            $identity.User,
            [Security.AccessControl.FileSystemRights]::FullControl,
            $inheritance,
            [Security.AccessControl.PropagationFlags]::None,
            [Security.AccessControl.AccessControlType]::Allow)
        $security.SetAccessRule($rule)
        Set-Acl -LiteralPath $Path -AclObject $security
    }
    finally {
        $identity.Dispose()
    }
}

function New-SecureRandomPassword {
    $alphabet =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789-_"
    $bytes = New-Object byte[] 48
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    $password = New-Object Security.SecureString
    try {
        $generator.GetBytes($bytes)
        foreach ($value in $bytes) {
            $password.AppendChar($alphabet[[int]$value % $alphabet.Length])
        }
        $password.MakeReadOnly()
        return $password
    }
    finally {
        $generator.Dispose()
        [Array]::Clear($bytes, 0, $bytes.Length)
    }
}

function Get-Sha256Fingerprint {
    param(
        [Parameter(Mandatory = $true)]
        [Security.Cryptography.X509Certificates.X509Certificate2]$Certificate
    )

    $sha256 = [Security.Cryptography.SHA256]::Create()
    $hash = $null
    try {
        $hash = $sha256.ComputeHash($Certificate.RawData)
        return ([BitConverter]::ToString($hash)).Replace("-", "")
    }
    finally {
        $sha256.Dispose()
        if ($null -ne $hash) {
            [Array]::Clear($hash, 0, $hash.Length)
        }
    }
}

function Remove-CertificateFromPersonalStore {
    param(
        [Parameter(Mandatory = $true)]
        [Security.Cryptography.X509Certificates.X509Certificate2]$Certificate
    )

    $certificatePath =
        "Cert:\CurrentUser\My\" + $Certificate.Thumbprint
    if (Test-Path -LiteralPath $certificatePath) {
        Remove-Item -LiteralPath $certificatePath -Force
    }
}

function Install-CurrentUserTrustedRoot {
    param([Parameter(Mandatory = $true)][string]$CertificatePath)

    $certificate =
        [Security.Cryptography.X509Certificates.X509Certificate2]::new(
            $CertificatePath)
    $store =
        [Security.Cryptography.X509Certificates.X509Store]::new(
            [Security.Cryptography.X509Certificates.StoreName]::Root,
            [Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser)
    try {
        $store.Open(
            [Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        $store.Add($certificate)
    }
    finally {
        $store.Close()
        $store.Dispose()
        $certificate.Dispose()
    }
}

Protect-OutputDirectory -Path $outputRoot

if ($ManagedCertificateGenerator) {
    if ($env:GITHUB_ACTIONS -ne "true") {
        throw "The managed certificate generator is restricted to the isolated GitHub Actions acceptance job."
    }

    $dotnetCommand = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if (-not $dotnetCommand) {
        throw "dotnet.exe is unavailable for the managed CI certificate generator."
    }
    $generatorAssembly = Join-Path `
        $root `
        "tests\NosGm.Authentication.Runtime.SelfTest\bin\Release\net10.0\NosGm.Authentication.Runtime.SelfTest.dll"
    if (-not (Test-Path -LiteralPath $generatorAssembly -PathType Leaf)) {
        throw "The authentication runtime self-test must be built in Release before generating the isolated CI certificate bundle."
    }

    Write-Host "[CERT] Creating the isolated CI bundle with managed .NET cryptography."
    $payloadJson = & $dotnetCommand.Source `
        $generatorAssembly `
        "--generate-ci-certificate-bundle" `
        $outputRoot `
        ([string]$KeyLength)
    if ($LASTEXITCODE -ne 0) {
        throw "The managed CI certificate generator failed."
    }

    $payload = $payloadJson | Select-Object -Last 1 | ConvertFrom-Json
    try {
        $passwords = [pscustomobject]@{
            SchemaVersion = 1
            Server = ConvertTo-SecureString `
                ([string]$payload.Passwords.Server) `
                -AsPlainText `
                -Force
            AuthBridge = ConvertTo-SecureString `
                ([string]$payload.Passwords.AuthBridge) `
                -AsPlainText `
                -Force
            Login = ConvertTo-SecureString `
                ([string]$payload.Passwords.Login) `
                -AsPlainText `
                -Force
            World = ConvertTo-SecureString `
                ([string]$payload.Passwords.World) `
                -AsPlainText `
                -Force
        }
        $passwords |
            Export-Clixml -LiteralPath ([string]$payload.CredentialsPath)

        if ($TrustRootCertificate) {
            throw "The isolated CI certificate bundle uses file-scoped root pinning and must not modify the Windows trust store."
        }

        Write-Host `
            "[ISOLATED] The ephemeral CI root will be pinned by absolute file path." `
            -ForegroundColor Green
        Write-Host "Manifest: $($payload.ManifestPath)"
        Write-Output ([string]$payload.ManifestPath)
    }
    finally {
        foreach ($name in @("Server", "AuthBridge", "Login", "World")) {
            $payload.Passwords.$name = $null
        }
        $payloadJson = $null
        $payload = $null
    }
    return
}

$createdCertificates =
    New-Object System.Collections.Generic.List[object]
$createdFiles =
    New-Object System.Collections.Generic.List[string]
$rootCertificate = $null
$trustedRootInstalled = $false

try {
    $bundleId = [Guid]::NewGuid().ToString("N")
    $notAfter = (Get-Date).ToUniversalTime().AddYears(2)
    $rootNotAfter = (Get-Date).ToUniversalTime().AddYears(5)

    Write-Host "[CERT] Creating the NosGM authentication root."
    $rootCertificate = New-SelfSignedCertificate `
        -Type Custom `
        -Subject "CN=NosGM Local Authentication Root $bundleId" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyAlgorithm RSA `
        -KeyLength $KeyLength `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -KeyUsage CertSign, CRLSign, DigitalSignature `
        -TextExtension @("2.5.29.19={critical}{text}ca=true&pathlength=0") `
        -NotAfter $rootNotAfter
    $createdCertificates.Add($rootCertificate)

    Write-Host "[CERT] Creating the loopback server identity."
    $serverCertificate = New-SelfSignedCertificate `
        -Type Custom `
        -Subject "CN=NosGM Local Authentication Server" `
        -Signer $rootCertificate `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyAlgorithm RSA `
        -KeyLength $KeyLength `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -KeyUsage DigitalSignature, KeyEncipherment `
        -TextExtension @(
            "2.5.29.37={text}1.3.6.1.5.5.7.3.1",
            "2.5.29.17={text}DNS=localhost&IPAddress=127.0.0.1"
        ) `
        -NotAfter $notAfter
    $createdCertificates.Add($serverCertificate)

    $clientCertificates = @{}
    foreach ($role in @("AuthBridge", "Login", "World")) {
        Write-Host "[CERT] Creating the $role client identity."
        $certificate = New-SelfSignedCertificate `
            -Type Custom `
            -Subject "CN=NosGM Local Authentication $role" `
            -Signer $rootCertificate `
            -CertStoreLocation "Cert:\CurrentUser\My" `
            -KeyAlgorithm RSA `
            -KeyLength $KeyLength `
            -HashAlgorithm SHA256 `
            -KeyExportPolicy Exportable `
            -KeyUsage DigitalSignature `
            -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
            -NotAfter $notAfter
        $createdCertificates.Add($certificate)
        $clientCertificates[$role] = $certificate
    }

    $passwords = [pscustomobject]@{
        SchemaVersion = 1
        Server = New-SecureRandomPassword
        AuthBridge = New-SecureRandomPassword
        Login = New-SecureRandomPassword
        World = New-SecureRandomPassword
    }

    $rootCertificatePath =
        Join-Path $outputRoot "nosgm-authentication-root.cer"
    Export-Certificate `
        -Cert $rootCertificate `
        -FilePath $rootCertificatePath `
        -Type CERT `
        -Force | Out-Null
    $createdFiles.Add($rootCertificatePath)

    $serverCertificatePath =
        Join-Path $outputRoot "nosgm-authentication-server.pfx"
    Export-PfxCertificate `
        -Cert $serverCertificate `
        -FilePath $serverCertificatePath `
        -Password $passwords.Server `
        -ChainOption EndEntityCertOnly `
        -Force | Out-Null
    $createdFiles.Add($serverCertificatePath)

    $clientManifest = [ordered]@{}
    foreach ($role in @("AuthBridge", "Login", "World")) {
        $fileName =
            "nosgm-authentication-" + $role.ToLowerInvariant() + ".pfx"
        $certificatePath = Join-Path $outputRoot $fileName
        Export-PfxCertificate `
            -Cert $clientCertificates[$role] `
            -FilePath $certificatePath `
            -Password $passwords.$role `
            -ChainOption EndEntityCertOnly `
            -Force | Out-Null
        $createdFiles.Add($certificatePath)
        $clientManifest[$role] = [ordered]@{
            CertificatePath = $certificatePath
            Sha256 = Get-Sha256Fingerprint `
                -Certificate $clientCertificates[$role]
        }
    }

    $credentialsPath =
        Join-Path $outputRoot "credentials.dpapi.clixml"
    $passwords | Export-Clixml -LiteralPath $credentialsPath
    $createdFiles.Add($credentialsPath)

    $manifestPath = Join-Path $outputRoot "manifest.json"
    $manifest = [ordered]@{
        SchemaVersion = 1
        BundleId = $bundleId
        CreatedAtUtc = [DateTime]::UtcNow.ToString("O")
        ExpiresAtUtc = $notAfter.ToString("O")
        RootCertificatePath = $rootCertificatePath
        RootCertificateThumbprint = $rootCertificate.Thumbprint
        ServerCertificatePath = $serverCertificatePath
        ServerCertificateSha256 =
            Get-Sha256Fingerprint -Certificate $serverCertificate
        Clients = $clientManifest
        CredentialsPath = $credentialsPath
    }
    $manifest |
        ConvertTo-Json -Depth 6 |
        Set-Content -LiteralPath $manifestPath -Encoding UTF8
    $createdFiles.Add($manifestPath)

    if ($TrustRootCertificate) {
        Install-CurrentUserTrustedRoot `
            -CertificatePath $rootCertificatePath
        $trustedRootInstalled = $true
        Write-Host `
            "[TRUSTED] Installed the public NosGM development root in Cert:\CurrentUser\Root." `
            -ForegroundColor Green
    }
    else {
        Write-Warning "The development root was not trusted. Re-run Import-Certificate for '$rootCertificatePath' in Cert:\CurrentUser\Root before starting gRPC."
    }

    Write-Host ""
    Write-Host "NosGM local authentication certificate bundle created." `
        -ForegroundColor Green
    Write-Host "Manifest: $manifestPath"
    Write-Host "Private keys and DPAPI-protected passwords remain under the current-user-only output directory."
    Write-Output $manifestPath
}
catch {
    if ($trustedRootInstalled -and $null -ne $rootCertificate) {
        $trustedRootPath =
            "Cert:\CurrentUser\Root\" + $rootCertificate.Thumbprint
        Remove-Item `
            -LiteralPath $trustedRootPath `
            -Force `
            -ErrorAction SilentlyContinue
    }
    foreach ($filePath in $createdFiles) {
        Remove-Item -LiteralPath $filePath -Force -ErrorAction SilentlyContinue
    }
    throw
}
finally {
    foreach ($certificate in $createdCertificates) {
        Remove-CertificateFromPersonalStore -Certificate $certificate
        $certificate.Dispose()
    }
}
