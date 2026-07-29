[CmdletBinding()]
param(
    [switch]$InventoryOnly
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Resolve-DotNetHost {
    $candidates = New-Object System.Collections.Generic.List[string]
    $command = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        $candidates.Add($command.Source)
    }

    if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles)) {
        $candidates.Add((Join-Path $env:ProgramFiles "dotnet\dotnet.exe"))
    }

    if (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
        $candidates.Add((Join-Path $env:LOCALAPPDATA "NosGM\dotnet10\dotnet.exe"))
        # Older NosGM bootstrap scripts used this directory name even when a
        # newer SDK was installed side by side into the same dotnet host.
        $candidates.Add((Join-Path $env:LOCALAPPDATA "NosGM\dotnet9\dotnet.exe"))
        $candidates.Add((Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"))
    }

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    throw ".NET was not found. Install the stable .NET 10 x64 SDK and open a new PowerShell window."
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Write-Host ("[dotnet] " + ($Arguments -join " ")) -ForegroundColor Cyan
    & $script:dotnetHost @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet failed with exit code $LASTEXITCODE."
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedText
    )

    $fullPath = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Expected file was not found: $RelativePath"
    }

    $content = [System.IO.File]::ReadAllText($fullPath)
    if (-not $content.Contains($ExpectedText)) {
        throw "$RelativePath does not contain the expected target '$ExpectedText'."
    }
}

$script:dotnetHost = Resolve-DotNetHost
Write-Host "[FOUND] $script:dotnetHost" -ForegroundColor Green

$installedSdks = & $script:dotnetHost --list-sdks
if ($LASTEXITCODE -ne 0) {
    throw "Unable to list installed .NET SDKs."
}

$installedSdks | ForEach-Object { Write-Host "[SDK] $_" }
if (-not ($installedSdks | Where-Object { $_ -match "^10\.0\." })) {
    throw "A stable .NET 10 SDK is required. Installed SDKs did not include 10.0.x."
}

$explicitNet10Projects = @(
    "Launcher\src\NosGM.Launcher\NosGM.Launcher.csproj",
    "Launcher\src\NosGM.ManifestBuilder\NosGM.ManifestBuilder.csproj",
    "Launcher\src\NosGM.SteamAuthStub\NosGM.SteamAuthStub.csproj",
    "Launcher\src\NosGM.Updater.Core\NosGM.Updater.Core.csproj",
    "Launcher\tests\NosGM.GameforgePipe.SelfTest\NosGM.GameforgePipe.SelfTest.csproj",
    "Launcher\tests\NosGM.SteamClient.SelfTest\NosGM.SteamClient.SelfTest.csproj",
    "Launcher\tests\NosGM.Updater.SelfTest\NosGM.Updater.SelfTest.csproj",
    "Tools\NosGM.ClientThemeEditor\NosGM.ClientThemeEditor.csproj",
    "Tools\NosGM.DataUpdater\NosGM.DataUpdater.csproj",
    "Tools\NosGM.PacketCatalog\NosGM.PacketCatalog.csproj",
    "Tools\NosGM.ResourceExplorer\NosGM.ResourceExplorer.csproj",
    "Tools\NosGM.TimeSpaceParser\NosGM.TimeSpaceParser.csproj"
)

foreach ($project in $explicitNet10Projects) {
    Assert-FileContains -RelativePath $project -ExpectedText "<TargetFramework>net10.0"
    Write-Host "[PASS] $project targets .NET 10." -ForegroundColor Green
}

Assert-FileContains -RelativePath "Web\Directory.Build.props" -ExpectedText "<TargetFramework>net10.0</TargetFramework>"
Write-Host "[PASS] All three Web projects inherit .NET 10." -ForegroundColor Green

$allProjects = Get-ChildItem -LiteralPath $repositoryRoot -Filter *.csproj -File -Recurse
$legacyCount = 0
$net10Count = 0
$deferredModern = New-Object System.Collections.Generic.List[string]

foreach ($project in $allProjects) {
    $content = [System.IO.File]::ReadAllText($project.FullName)
    if ($content.Contains("<TargetFrameworkVersion>v4.8.1</TargetFrameworkVersion>")) {
        $legacyCount++
    }
    elseif ($content -match "<TargetFramework>net10\.0(?:-windows)?</TargetFramework>") {
        $net10Count++
    }
    elseif ($content -match "<TargetFramework>([^<]+)</TargetFramework>") {
        $deferredModern.Add(("{0} ({1})" -f $project.FullName.Substring($repositoryRoot.Length + 1), $Matches[1]))
    }
}

Write-Host "[INVENTORY] Projects: $($allProjects.Count); explicit .NET 10: $net10Count; .NET Framework 4.8.1: $legacyCount." -ForegroundColor Yellow
Write-Host "[INVENTORY] Web projects inheriting .NET 10: 3." -ForegroundColor Yellow
foreach ($project in $deferredModern) {
    Write-Host "[DEFERRED] $project" -ForegroundColor Yellow
}

if ($allProjects.Count -ne 45) {
    throw "Project inventory changed: expected 45 projects but found $($allProjects.Count). Review the migration matrix."
}

if ($legacyCount -ne 28) {
    throw "Legacy inventory changed: expected 28 .NET Framework 4.8.1 projects but found $legacyCount."
}

if ($InventoryOnly) {
    Write-Host "NosGM .NET 10 foundation inventory passed." -ForegroundColor Green
    exit 0
}

Push-Location $repositoryRoot
try {
    Invoke-DotNet -Arguments @("build", "Web\NosGM.Web.sln", "-c", "Release", "--nologo")

    $toolProjects = Get-ChildItem -LiteralPath (Join-Path $repositoryRoot "Tools") -Filter *.csproj -File -Recurse |
        Sort-Object FullName
    foreach ($toolProject in $toolProjects) {
        Invoke-DotNet -Arguments @("build", $toolProject.FullName, "-c", "Release", "--nologo")
    }

    if ($env:OS -eq "Windows_NT") {
        Invoke-DotNet -Arguments @("build", "Launcher\NosGM.Launcher.sln", "-c", "Release", "--nologo")
    }
    else {
        Write-Host "[SKIP] Launcher build requires Windows and its NativeAOT x86 toolchain." -ForegroundColor Yellow
    }
}
finally {
    Pop-Location
}

Write-Host "NosGM .NET 10 foundation build passed." -ForegroundColor Green
