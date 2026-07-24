[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$ManifestUri,

    [Parameter(Mandatory = $true)]
    [string]$ContentBaseUri,

    [Parameter(Mandatory = $true)]
    [string]$KeyId,

    [Parameter(Mandatory = $true)]
    [string]$PublicKeyPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$launcherRoot = Split-Path -Parent $scriptDirectory
$repositoryRoot = Split-Path -Parent $launcherRoot
$solution = Join-Path $launcherRoot 'NosGM.Launcher.sln'
$builderProject = Join-Path $launcherRoot 'src/NosGM.ManifestBuilder/NosGM.ManifestBuilder.csproj'
$launcherProject = Join-Path $launcherRoot 'src/NosGM.Launcher/NosGM.Launcher.csproj'
$generatedChannel = Join-Path $launcherRoot 'src/NosGM.Launcher/TrustedChannel.Generated.cs'
$publicKey = [IO.Path]::GetFullPath($PublicKeyPath)
$output = [IO.Path]::GetFullPath($OutputDirectory)
$work = "$output.work.$([Guid]::NewGuid().ToString('N'))"

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Command,
        [Parameter(Mandatory = $true)][string]$Description
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path $publicKey -PathType Leaf)) {
    throw "Public key file does not exist: $publicKey"
}

$keyText = Get-Content $publicKey -Raw
if ($keyText.Contains('PRIVATE KEY', [StringComparison]::Ordinal)) {
    throw 'The publish pipeline accepts only the public release key.'
}

if (Test-Path $output) {
    throw "Output directory already exists: $output"
}

if (Test-Path $generatedChannel) {
    Remove-Item $generatedChannel -Force
}

$trackedChanges = @(& git -C $repositoryRoot status --porcelain --untracked-files=no)
if ($LASTEXITCODE -ne 0) {
    throw 'Could not inspect Git working-tree state.'
}
if ($trackedChanges.Count -gt 0) {
    throw "Launcher packages must be built from a clean tracked working tree.`n$($trackedChanges -join [Environment]::NewLine)"
}

$sourceCommit = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceCommit -notmatch '^[0-9a-f]{40}$') {
    throw 'Could not resolve the exact source commit.'
}

try {
    New-Item -ItemType Directory -Path $work -Force | Out-Null

    Invoke-Checked { dotnet restore $solution } 'Launcher restore'
    Invoke-Checked {
        dotnet run --project $builderProject --configuration Release --no-restore -- channel `
            --manifest-uri $ManifestUri `
            --content-base-uri $ContentBaseUri `
            --key-id $KeyId `
            --public-key $publicKey `
            --output $generatedChannel
    } 'Trusted channel generation'

    $fingerprint = (& dotnet run --project $builderProject --configuration Release --no-build -- fingerprint --public-key $publicKey).Trim()
    if ($LASTEXITCODE -ne 0 -or $fingerprint -notmatch '^[0-9A-F]{64}$') {
        throw 'Could not calculate the P-256 public-key fingerprint.'
    }

    $informationalVersion = "$Version+$sourceCommit"
    Invoke-Checked {
        dotnet publish $launcherProject `
            --configuration Release `
            --runtime $Runtime `
            --self-contained true `
            --output $work `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:DebugType=None `
            -p:DebugSymbols=false `
            -p:Deterministic=true `
            -p:ContinuousIntegrationBuild=true `
            -p:Version=$Version `
            -p:InformationalVersion=$informationalVersion
    } 'Launcher publish'

    Copy-Item (Join-Path $launcherRoot 'LICENSE') (Join-Path $work 'LICENSE.txt')
    Copy-Item (Join-Path $launcherRoot 'NOTICE.md') (Join-Path $work 'LAUNCHER-NOTICE.md')
    Copy-Item (Join-Path $launcherRoot 'README.md') (Join-Path $work 'README.md')
    Copy-Item (Join-Path $repositoryRoot 'AUTHORS.md') (Join-Path $work 'NOSGM-AUTHORS.md')
    Copy-Item (Join-Path $repositoryRoot 'NOTICE.md') (Join-Path $work 'NOSGM-NOTICE.md')
    Copy-Item (Join-Path $repositoryRoot 'THIRD_PARTY_NOTICES.md') (Join-Path $work 'THIRD_PARTY_NOTICES.md')

    [IO.File]::WriteAllText(
        (Join-Path $work 'SOURCE_COMMIT.txt'),
        $sourceCommit + [Environment]::NewLine,
        [Text.UTF8Encoding]::new($false))

    $packageFiles = @(Get-ChildItem $work -File -Recurse | Sort-Object FullName | ForEach-Object {
        $relative = [IO.Path]::GetRelativePath($work, $_.FullName).Replace('\', '/')
        [ordered]@{
            path = $relative
            size = $_.Length
            sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
        }
    })

    $releaseInfo = [ordered]@{
        schemaVersion = 1
        product = 'NosGM Launcher'
        version = $Version
        sourceCommit = $sourceCommit
        runtime = $Runtime
        selfContained = $true
        keyId = $KeyId
        publicKeyFingerprint = $fingerprint
        manifestUri = $ManifestUri
        contentBaseUri = $ContentBaseUri
        files = $packageFiles
    }
    $json = $releaseInfo | ConvertTo-Json -Depth 8
    [IO.File]::WriteAllText(
        (Join-Path $work 'release-info.json'),
        $json + [Environment]::NewLine,
        [Text.UTF8Encoding]::new($false))

    & (Join-Path $scriptDirectory 'verify-launcher-package.ps1') -PackageDirectory $work

    New-Item -ItemType Directory -Path (Split-Path -Parent $output) -Force | Out-Null
    Move-Item $work $output
    Write-Host "NosGM Launcher package created: $output"
    Write-Host "Source commit: $sourceCommit"
    Write-Host "Public-key fingerprint: $fingerprint"
}
finally {
    if (Test-Path $generatedChannel) {
        Remove-Item $generatedChannel -Force
    }
    if (Test-Path $work) {
        Remove-Item $work -Recurse -Force
    }
}
