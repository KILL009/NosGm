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
    param([string]$TargetsPath, [string]$Include, [string]$ProjectCondition)

    [xml]$xml = Get-Content -LiteralPath $TargetsPath -Raw
    $project = $xml.DocumentElement
    $namespace = $project.NamespaceURI
    $existing = @($project.ItemGroup | ForEach-Object { @($_.Compile) }) |
        Where-Object { $_ -and $_.Include -eq $Include } | Select-Object -First 1
    if ($null -ne $existing) { return }

    $group = $null
    if (-not [string]::IsNullOrWhiteSpace($ProjectCondition)) {
        $group = @($project.ItemGroup) | Where-Object { $_.Condition -eq $ProjectCondition } | Select-Object -First 1
    }
    if ($null -eq $group) {
        $group = if ([string]::IsNullOrEmpty($namespace)) { $xml.CreateElement('ItemGroup') } else { $xml.CreateElement('ItemGroup', $namespace) }
        if (-not [string]::IsNullOrWhiteSpace($ProjectCondition)) { $group.SetAttribute('Condition', $ProjectCondition) }
        $project.AppendChild($group) | Out-Null
    }

    $compile = if ([string]::IsNullOrEmpty($namespace)) { $xml.CreateElement('Compile') } else { $xml.CreateElement('Compile', $namespace) }
    $compile.SetAttribute('Include', $Include)
    $group.AppendChild($compile) | Out-Null
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
$handlerDir = Join-Path $root 'Data\NosGm.Handler\PacketHandler\Basic'
$worldDir = Join-Path $root 'Data\NosGm.Program\NosGm.World'
$rootTargets = Join-Path $root 'Directory.Build.targets'
$worldTargets = Join-Path $worldDir 'Directory.Build.targets'
foreach ($path in @($handlerDir, $worldDir, $rootTargets, $worldTargets)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Current NosGM path not found: $path" }
}

$mallSource = Resolve-SourceFile 'MallPacketHandler.cs' 'Data\NosGm.Handler\PacketHandler\Basic\MallPacketHandler.cs'
$deliverySource = Resolve-SourceFile 'ShopDeliveryWorker.cs' 'Data\NosGm.Program\NosGm.World\ShopDeliveryWorker.cs'
$entrySource = Resolve-SourceFile 'PortalWorldEntryPoint.cs' 'Data\NosGm.Program\NosGm.World\PortalWorldEntryPoint.cs'

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
foreach ($path in @($rootTargets, $worldTargets)) { Copy-Item -LiteralPath $path -Destination "$path.portal-v2-$stamp.bak" -Force }
Copy-Item -LiteralPath $mallSource -Destination (Join-Path $handlerDir 'MallPacketHandler.cs') -Force
Copy-Item -LiteralPath $deliverySource -Destination (Join-Path $worldDir 'ShopDeliveryWorker.cs') -Force
Copy-Item -LiteralPath $entrySource -Destination (Join-Path $worldDir 'PortalWorldEntryPoint.cs') -Force

Ensure-CompileItem -TargetsPath $rootTargets -Include '$(MSBuildThisFileDirectory)Data\NosGm.Handler\PacketHandler\Basic\MallPacketHandler.cs' -ProjectCondition "'`$(MSBuildProjectName)' == 'NosGm.Handler'"
Ensure-CompileItem -TargetsPath $worldTargets -Include '$(MSBuildProjectDirectory)\ShopDeliveryWorker.cs'
Ensure-CompileItem -TargetsPath $worldTargets -Include '$(MSBuildProjectDirectory)\PortalWorldEntryPoint.cs'
Ensure-WorldStartupObject -TargetsPath $worldTargets

Write-Host 'NosMall Portal 2.0 integration installed for the current NosGm.* project layout.'
Write-Host 'No SQL password or ticket secret was written to app.config.'
Write-Host 'Configure NOSGM_SHOP_ENABLED, NOSGM_SHOP_URL, NOSGM_SHOP_TICKET_SECRET, NOSGM_SHOP_SQL_CONNECTION_STRING and NOSGM_SHOP_SYSTEM_SENDER_ID in the World Server environment.'
