[CmdletBinding()]
param(
    [switch]$SkipLauncher,
    [switch]$SkipAuthenticationRuntime,
    [switch]$SkipClean
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "The NosGM local stack build requires Windows."
}

$root = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $root "NosGm.sln"
$launcherSolutionPath = Join-Path $root "Launcher\NosGM.Launcher.sln"
$authenticationProjectPath = Join-Path $root "Data\NosGm.Program\NosGm.Authentication.Server\NosGm.Authentication.Server.csproj"
$authenticationOutputPath = Join-Path $root "bin\Release\Authentication"

function Resolve-MSBuild {
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return [System.IO.Path]::GetFullPath($command.Source)
    }

    $programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
    $vswhere = Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path -LiteralPath $vswhere -PathType Leaf) {
        $resolved = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
        if (-not [string]::IsNullOrWhiteSpace($resolved)) {
            return [System.IO.Path]::GetFullPath($resolved)
        }
    }

    throw "MSBuild was not found. Install Visual Studio Build Tools 2022 with the .NET Framework build tools workload."
}

function Resolve-DotNet10 {
    $candidatePaths = New-Object System.Collections.Generic.List[string]
    $command = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        $candidatePaths.Add([string]$command.Source)
    }

    foreach ($directory in @(
        $env:DOTNET_ROOT,
        (Join-Path $env:LOCALAPPDATA "NosGM\dotnet10"),
        (Join-Path $env:ProgramFiles "dotnet"),
        (Join-Path ${env:ProgramFiles(x86)} "dotnet")
    )) {
        if (-not [string]::IsNullOrWhiteSpace($directory)) {
            $candidatePaths.Add((Join-Path $directory "dotnet.exe"))
        }
    }

    foreach ($candidatePath in @($candidatePaths | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidatePath -PathType Leaf)) {
            continue
        }

        $installedSdks = & $candidatePath --list-sdks 2>$null
        if ($LASTEXITCODE -eq 0 -and @($installedSdks | Where-Object { $_ -match '^10\.' }).Count -gt 0) {
            return [System.IO.Path]::GetFullPath($candidatePath)
        }
    }

    throw ".NET 10 SDK was not found. Install the stable .NET 10 x64 SDK and open a new PowerShell window."
}

function Resolve-Net9SdkImports {
    param([Parameter(Mandatory = $true)][string]$DotNetExecutable)

    $matches = New-Object System.Collections.Generic.List[object]
    foreach ($sdkLine in (& $DotNetExecutable --list-sdks 2>$null)) {
        if ($sdkLine -notmatch '^(9\.0\.[0-9]+)\s+\[(.+)\]$') {
            continue
        }

        $version = [Version]$Matches[1]
        $sdkBase = $Matches[2]
        $sdkPath = Join-Path (Join-Path $sdkBase $version.ToString()) "Sdks"
        if (Test-Path -LiteralPath (Join-Path $sdkPath "Microsoft.NET.Sdk\Sdk") -PathType Container) {
            $matches.Add([pscustomobject]@{
                Version = $version
                Path = [System.IO.Path]::GetFullPath($sdkPath)
            })
        }
    }

    $selected = $matches | Sort-Object Version -Descending | Select-Object -First 1
    if ($null -eq $selected) {
        throw ".NET 9 compatibility SDK was not found. Install it with: winget install --id Microsoft.DotNet.SDK.9 --exact --source winget"
    }

    return $selected
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)][string]$Description,
        [Parameter(Mandatory = $true)][scriptblock]$Command
    )

    Write-Host "[BUILD] $Description" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

$dotnet = Resolve-DotNet10
$msbuild = Resolve-MSBuild
$net9Sdk = Resolve-Net9SdkImports -DotNetExecutable $dotnet

if (-not $SkipClean) {
    & (Join-Path $PSScriptRoot "clear-local-restore-state.ps1")
}

$previousDotNetRoot = $env:DOTNET_ROOT
$previousMSBuildSdksPath = $env:MSBuildSDKsPath
$previousWorkloadResolver = $env:MSBuildEnableWorkloadResolver

try {
    $env:DOTNET_ROOT = Split-Path -Parent $dotnet
    $env:MSBuildSDKsPath = $net9Sdk.Path
    $env:MSBuildEnableWorkloadResolver = "false"

    Write-Host "[BUILD] Legacy SDK imports: .NET $($net9Sdk.Version)" -ForegroundColor DarkCyan
    Write-Host "[BUILD] Legacy intermediate assets: obj\legacy\" -ForegroundColor DarkCyan

    Invoke-CheckedCommand -Description "Restoring legacy server projects for net481 / x64" -Command {
        & $msbuild $solutionPath `
            /t:Restore `
            /m `
            /nologo `
            /nr:false `
            /v:minimal `
            /p:RestorePackagesConfig=true `
            /p:MSBuildEnableWorkloadResolver=false `
            /p:NosGmLegacyBuild=true `
            /p:Configuration=Release `
            /p:Platform=x64
    }

    Invoke-CheckedCommand -Description "Building legacy server Release / x64" -Command {
        & $msbuild $solutionPath `
            /t:Build `
            /m `
            /nologo `
            /nr:false `
            /v:minimal `
            /p:MSBuildEnableWorkloadResolver=false `
            /p:NosGmLegacyBuild=true `
            /p:Configuration=Release `
            /p:Platform=x64
    }
}
finally {
    $env:MSBuildEnableWorkloadResolver = $previousWorkloadResolver
    $env:MSBuildSDKsPath = $previousMSBuildSdksPath
    $env:DOTNET_ROOT = $previousDotNetRoot
}

# Recreate normal net481/net10 assets after the isolated legacy restore so
# Visual Studio design-time builds do not consume the net481-only asset graph.
$dualTargetProjects = @(
    Get-ChildItem -LiteralPath (Join-Path $root "Data") -Filter *.csproj -File -Recurse |
        Where-Object {
            Select-String -LiteralPath $_.FullName -Pattern 'NosGmLegacyBuild' -Quiet
        }
)
foreach ($project in $dualTargetProjects) {
    Invoke-CheckedCommand -Description "Restoring modern assets: $($project.Name)" -Command {
        & $dotnet restore $project.FullName --nologo --property:NosGmLegacyBuild=false
    }
}

if (-not $SkipLauncher) {
    Invoke-CheckedCommand -Description "Restoring NosGM Launcher" -Command {
        & $dotnet restore $launcherSolutionPath --nologo
    }
    Invoke-CheckedCommand -Description "Building NosGM Launcher Release" -Command {
        & $dotnet build $launcherSolutionPath --configuration Release --no-restore --nologo
    }
}

if (-not $SkipAuthenticationRuntime) {
    Invoke-CheckedCommand -Description "Publishing .NET 10 authentication runtime" -Command {
        & $dotnet publish $authenticationProjectPath `
            --configuration Release `
            --output $authenticationOutputPath `
            --nologo
    }
}

$requiredOutputs = @(
    (Join-Path $root "bin\Release\Master\NosGm.Master.Server.exe"),
    (Join-Path $root "bin\Release\World\NosGm.World.exe"),
    (Join-Path $root "bin\Release\Login\NosGm.Login.exe")
)
if (-not $SkipLauncher) {
    $requiredOutputs += Join-Path $root "Launcher\src\NosGM.Launcher\bin\Release\net10.0-windows\NosGM.Launcher.exe"
}
if (-not $SkipAuthenticationRuntime) {
    $requiredOutputs += Join-Path $authenticationOutputPath "NosGm.Authentication.Server.dll"
}

foreach ($output in $requiredOutputs) {
    if (-not (Test-Path -LiteralPath $output -PathType Leaf)) {
        throw "Expected build output was not produced: $output"
    }
    Write-Host "[FOUND] $output" -ForegroundColor Green
}

Write-Host ""
Write-Host "NosGM local build completed successfully." -ForegroundColor Green
Write-Host "Start without rebuilding with:"
Write-Host "  ./scripts/start-modern-login-local.ps1 -SkipBuild"
Write-Host "For Windows 10 gRPC-Web:"
Write-Host "  ./scripts/start-modern-login-local.ps1 -SkipBuild -AuthenticationTransport GRPC -AuthenticationGrpcWireMode GRPCWEB"
