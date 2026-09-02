param(
    [Parameter(Mandatory = $true)]
    [string]$NosGmRoot,
    [string]$IntegrationSourceRoot
)

$ErrorActionPreference = 'Stop'

function Resolve-SourceFile {
    param([string]$FileName, [string]$RepoRelativePath)

    if (-not [string]::IsNullOrWhiteSpace($IntegrationSourceRoot)) {
        $candidate = Join-Path $IntegrationSourceRoot $FileName
        if (Test-Path -LiteralPath $candidate) { return (Resolve-Path -LiteralPath $candidate).Path }
    }

    $besideScript = Join-Path $PSScriptRoot $FileName
    if (Test-Path -LiteralPath $besideScript) { return (Resolve-Path -LiteralPath $besideScript).Path }

    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
    $inRepo = Join-Path $repoRoot $RepoRelativePath
    if (Test-Path -LiteralPath $inRepo) { return (Resolve-Path -LiteralPath $inRepo).Path }

    throw "Integration source not found for $FileName. Pass -IntegrationSourceRoot or run this script from the NosGM repository."
}

function Ensure-CompileItem {
    param([string]$TargetsPath, [string]$Include)

    [xml]$xml = Get-Content -LiteralPath $TargetsPath -Raw
    $project = $xml.DocumentElement
    $namespace = $project.NamespaceURI
    $existing = @($project.ItemGroup | ForEach-Object { @($_.Compile) }) |
        Where-Object { $_ -and $_.Include -eq $Include } | Select-Object -First 1
    if ($null -ne $existing) { return }

    $group = if ([string]::IsNullOrEmpty($namespace)) { $xml.CreateElement('ItemGroup') } else { $xml.CreateElement('ItemGroup', $namespace) }
    $compile = if ([string]::IsNullOrEmpty($namespace)) { $xml.CreateElement('Compile') } else { $xml.CreateElement('Compile', $namespace) }
    $compile.SetAttribute('Include', $Include)
    $group.AppendChild($compile) | Out-Null
    $project.AppendChild($group) | Out-Null
    $xml.Save($TargetsPath)
}

function Ensure-WorldStartupObject {
    param([string]$TargetsPath)

    [xml]$xml = Get-Content -LiteralPath $TargetsPath -Raw
    $project = $xml.DocumentElement
    $namespace = $project.NamespaceURI
    $propertyGroup = @($project.PropertyGroup) | Select-Object -First 1
    if ($null -eq $propertyGroup) {
        $propertyGroup = if ([string]::IsNullOrEmpty($namespace)) { $xml.CreateElement('PropertyGroup') } else { $xml.CreateElement('PropertyGroup', $namespace) }
        $project.PrependChild($propertyGroup) | Out-Null
    }

    $startup = $propertyGroup.StartupObject
    if ($null -eq $startup) {
        $startup = if ([string]::IsNullOrEmpty($namespace)) { $xml.CreateElement('StartupObject') } else { $xml.CreateElement('StartupObject', $namespace) }
        $propertyGroup.AppendChild($startup) | Out-Null
    }
    $startup.InnerText = 'NosGm.World.PortalWorldEntryPoint'
    $xml.Save($TargetsPath)
}

$root = (Resolve-Path -LiteralPath $NosGmRoot).Path
$worldDir = Join-Path $root 'Data\NosGm.Program\NosGm.World'
$worldTargets = Join-Path $worldDir 'Directory.Build.targets'
if (-not (Test-Path -LiteralPath $worldDir)) { throw "Current NosGM World project not found: $worldDir" }
if (-not (Test-Path -LiteralPath $worldTargets)) { throw "World Directory.Build.targets not found: $worldTargets" }

$bridgeSource = Resolve-SourceFile 'PortalBridgeWorker.cs' 'Data\NosGm.Program\NosGm.World\PortalBridgeWorker.cs'
$entrySource = Resolve-SourceFile 'PortalWorldEntryPoint.cs' 'Data\NosGm.Program\NosGm.World\PortalWorldEntryPoint.cs'

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
Copy-Item -LiteralPath $worldTargets -Destination "$worldTargets.portal-v2-$stamp.bak" -Force
Copy-Item -LiteralPath $bridgeSource -Destination (Join-Path $worldDir 'PortalBridgeWorker.cs') -Force
Copy-Item -LiteralPath $entrySource -Destination (Join-Path $worldDir 'PortalWorldEntryPoint.cs') -Force

Ensure-CompileItem -TargetsPath $worldTargets -Include '$(MSBuildProjectDirectory)\PortalBridgeWorker.cs'
Ensure-CompileItem -TargetsPath $worldTargets -Include '$(MSBuildProjectDirectory)\PortalWorldEntryPoint.cs'
Ensure-WorldStartupObject -TargetsPath $worldTargets

Write-Host 'NosGM Portal 2.0 GM bridge integration installed for Data\NosGm.Program\NosGm.World.'
Write-Host 'The old Frostvein paths and PortalBridgeConnectionString app.config setting are not used.'
Write-Host 'Configure NOSGM_PORTAL_BRIDGE_ENABLED=true and NOSGM_PORTAL_SQL_CONNECTION_STRING outside source control.'
