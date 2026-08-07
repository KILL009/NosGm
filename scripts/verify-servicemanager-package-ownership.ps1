param(
    [switch]$VerifyRestoredFiles
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$projectRelativePath = "Data/NosGm.ServiceManager/NosGm.ServiceManager.csproj"
$manifestRelativePath = "Data/NosGm.ServiceManager/packages.config"
$nestedManifestRelativePath = "Data/NosGm.ServiceManager/LogService/MongoDB/Lib/packages.config"
$projectPath = Join-Path $root $projectRelativePath
$manifestPath = Join-Path $root $manifestRelativePath
$nestedManifestPath = Join-Path $root $nestedManifestRelativePath

foreach ($path in @($projectPath, $manifestPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required ServiceManager file is missing: $path"
    }
}

if (Test-Path -LiteralPath $nestedManifestPath -PathType Leaf) {
    throw "ServiceManager must use one authoritative packages.config; remove $nestedManifestRelativePath."
}

[xml]$project = Get-Content -LiteralPath $projectPath -Raw
[xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw
$namespace = New-Object System.Xml.XmlNamespaceManager($project.NameTable)
$namespace.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")

$targetFramework = $project.SelectSingleNode("//msb:TargetFrameworkVersion", $namespace)
if ($null -eq $targetFramework -or $targetFramework.InnerText -ne "v4.8.1") {
    throw "ServiceManager must target .NET Framework v4.8.1."
}

$rawProject = Get-Content -LiteralPath $projectPath -Raw
if ($rawProject.Contains("..\..\..\packages\", [StringComparison]::OrdinalIgnoreCase)) {
    throw "ServiceManager still escapes above the repository when resolving packages."
}
if ($rawProject.Contains("NosTale.Configuration", [StringComparison]::OrdinalIgnoreCase)) {
    throw "ServiceManager still references the obsolete NosTale.Configuration project name."
}

$configurationReference = $project.SelectSingleNode(
    "//msb:ProjectReference[@Include='..\NosGm.Configuration\NosGm.Configuration.csproj']",
    $namespace)
if ($null -eq $configurationReference) {
    throw "ServiceManager must reference Data/NosGm.Configuration/NosGm.Configuration.csproj."
}
$configurationPath = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $projectPath) $configurationReference.Include))
if (-not (Test-Path -LiteralPath $configurationPath -PathType Leaf)) {
    throw "Resolved configuration project does not exist: $configurationPath"
}

$obsoleteConfigurationSources = New-Object System.Collections.Generic.List[string]
$compileItems = $project.SelectNodes("//msb:Compile/@Include", $namespace)
foreach ($compileItem in $compileItems) {
    $sourcePath = Join-Path (Split-Path -Parent $projectPath) $compileItem.Value
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "Compiled ServiceManager source is missing: $($compileItem.Value)"
    }

    $source = Get-Content -LiteralPath $sourcePath -Raw
    if ($source -match '(?m)^\s*using\s+NosTale\.Configuration\s*;') {
        $obsoleteConfigurationSources.Add($compileItem.Value)
    }
}
if ($obsoleteConfigurationSources.Count -gt 0) {
    throw "Compiled ServiceManager sources still import obsolete NosTale.Configuration: $(($obsoleteConfigurationSources -join ', '))"
}

$packages = @($manifest.packages.package)
if ($packages.Count -eq 0) {
    throw "ServiceManager packages.config is empty."
}
$duplicateIds = $packages |
    Group-Object -Property id |
    Where-Object Count -gt 1
if ($duplicateIds) {
    throw "Duplicate package declarations: $(($duplicateIds.Name -join ', '))"
}
$invalidFrameworks = $packages | Where-Object { $_.targetFramework -ne "net481" }
if ($invalidFrameworks) {
    throw "All ServiceManager package entries must target net481: $(($invalidFrameworks.id -join ', '))"
}

$packageFolders = @{}
foreach ($package in $packages) {
    $folder = "$($package.id).$($package.version)"
    $packageFolders[$folder] = $package
}

$packagePaths = New-Object System.Collections.Generic.List[string]
$hintPaths = $project.SelectNodes("//msb:HintPath", $namespace)
foreach ($node in $hintPaths) {
    $packagePaths.Add($node.InnerText)
}
$analyzers = $project.SelectNodes("//msb:Analyzer/@Include", $namespace)
foreach ($node in $analyzers) {
    if ($node.Value -match '(?i)packages\\') {
        $packagePaths.Add($node.Value)
    }
}
$imports = $project.SelectNodes("//msb:Import/@Project", $namespace)
foreach ($node in $imports) {
    if ($node.Value -match '(?i)packages\\') {
        $packagePaths.Add($node.Value)
    }
}

if ($packagePaths.Count -eq 0) {
    throw "ServiceManager has no package-backed paths to validate."
}

$missingOwners = New-Object System.Collections.Generic.List[string]
$invalidRoots = New-Object System.Collections.Generic.List[string]
foreach ($relativePath in $packagePaths) {
    if (-not $relativePath.StartsWith("..\..\packages\", [StringComparison]::OrdinalIgnoreCase)) {
        $invalidRoots.Add($relativePath)
        continue
    }

    $match = [regex]::Match($relativePath, '(?i)^\.\.\\\.\.\\packages\\([^\\]+)\\')
    if (-not $match.Success) {
        $invalidRoots.Add($relativePath)
        continue
    }

    $folder = $match.Groups[1].Value
    if (-not $packageFolders.ContainsKey($folder)) {
        $missingOwners.Add($folder)
    }

    if ($VerifyRestoredFiles) {
        $resolvedPath = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $projectPath) $relativePath))
        if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
            throw "Restored package file is missing: $relativePath -> $resolvedPath"
        }
    }
}

if ($invalidRoots.Count -gt 0) {
    throw "Package paths must use ..\..\packages: $(($invalidRoots | Sort-Object -Unique) -join ', ')"
}
if ($missingOwners.Count -gt 0) {
    throw "Package paths without an exact packages.config owner: $(($missingOwners | Sort-Object -Unique) -join ', ')"
}

$duplicatePrivateReferences = $project.SelectNodes("//msb:Reference[count(msb:Private) > 1]", $namespace)
if ($duplicatePrivateReferences.Count -gt 0) {
    throw "References contain duplicate Private metadata: $(($duplicatePrivateReferences.Include -join ', '))"
}

Write-Host "[PASS] ServiceManager package paths remain inside the repository."
Write-Host "[PASS] Every package-backed path has an exact packages.config owner."
Write-Host "[PASS] Package declarations are unique and target .NET Framework 4.8.1."
Write-Host "[PASS] The configuration project reference resolves to NosGm.Configuration."
Write-Host "[PASS] Compiled ServiceManager sources use the NosGm.Configuration namespace."
if ($VerifyRestoredFiles) {
    Write-Host "[PASS] Every package-backed file exists after restore."
}
