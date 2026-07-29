[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [switch]$TrustRootCertificate
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "Local NosGM authentication certificates can only be created on Windows."
}

foreach ($commandName in @(
    "New-SelfSignedCertificate",
    "Export-Certificate",
    "Export-PfxCertificate",
    "Import-Certificate"
)) {
    if (-not (Get-Command $commandName -ErrorAction SilentlyContinue)) {
        throw "$commandName is unavailable. Install the Windows PKI PowerShell module."
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

Protect-OutputDirectory -Path $outputRoot

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

    $rootCertificate = New-SelfSignedCertificate `
        -Type Custom `
        -Subject "CN=NosGM Local Authentication Root $bundleId" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyAlgorithm RSA `
        -KeyLength 3072 `
        -HashAlgorithm SHA256 `
        -KeyExportPolicy Exportable `
        -KeyUsage CertSign, CRLSign, DigitalSignature `
        -TextExtension @("2.5.29.19={critical}{text}ca=true&pathlength=0") `
        -NotAfter $rootNotAfter
    $createdCertificates.Add($rootCertificate)

    $serverCertificate = New-SelfSignedCertificate `
        -Type Custom `
        -Subject "CN=NosGM Local Authentication Server" `
        -Signer $rootCertificate `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyAlgorithm RSA `
        -KeyLength 3072 `
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
        $certificate = New-SelfSignedCertificate `
            -Type Custom `
            -Subject "CN=NosGM Local Authentication $role" `
            -Signer $rootCertificate `
            -CertStoreLocation "Cert:\CurrentUser\My" `
            -KeyAlgorithm RSA `
            -KeyLength 3072 `
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
        Import-Certificate `
            -FilePath $rootCertificatePath `
            -CertStoreLocation "Cert:\CurrentUser\Root" | Out-Null
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
