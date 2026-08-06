param(
    [string]$Source = "NosGm.sln",
    [string]$Destination = "artifacts/NosGm.Legacy.generated.sln"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $root $Source
$destinationPath = Join-Path $root $Destination

if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Source solution was not found: $sourcePath"
}

$sourceFullPath = [IO.Path]::GetFullPath($sourcePath)
$destinationFullPath = [IO.Path]::GetFullPath($destinationPath)
$rootFullPath = [IO.Path]::GetFullPath($root).TrimEnd('\')
$rootPrefix = $rootFullPath + '\'
if (-not $destinationFullPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Generated legacy solution must remain inside the repository: $destinationFullPath"
}
if ([string]::Equals($sourceFullPath, $destinationFullPath, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Source and destination solutions must be different files."
}

$boundaryProjects = @(
    [pscustomobject]@{
        Name = "NosTale.Module.Bazaar"
        Path = "Data\NosGm.Program\NosTale.Module.Bazaar\NosTale.Module.Bazaar.csproj"
        Guid = "B6E096AC-3141-46DA-8CE6-545D7776D9D8"
    },
    [pscustomobject]@{
        Name = "NosTale.Modules"
        Path = "Data\NosGm.Program\NosTale.Modules\NosTale.Modules.csproj"
        Guid = "D1EC765C-EA41-4DC6-944D-0DB37157DEE3"
    }
)

$requiredLegacyProjects = @(
    "Data\NosGm.Program\NosGm.Login\NosGm.Login.csproj",
    "Data\NosGm.Program\NosGm.Master.Server\NosGm.Master.Server.csproj",
    "Data\NosGm.Program\NosGm.Parser\NosGm.Parser.csproj",
    "Data\NosGm.Program\NosGm.World\NosGm.World.csproj"
)

function Get-ProjectFrameworks {
    param([Parameter(Mandatory = $true)][string]$ProjectPath)

    [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw
    $values = New-Object System.Collections.Generic.List[string]
    $nodes = $project.SelectNodes(
        "//*[local-name()='TargetFramework' or local-name()='TargetFrameworks' or local-name()='TargetFrameworkVersion']")
    foreach ($node in $nodes) {
        foreach ($value in ($node.InnerText -split ';')) {
            $normalized = $value.Trim()
            if (-not [string]::IsNullOrWhiteSpace($normalized) -and -not $values.Contains($normalized)) {
                $values.Add($normalized)
            }
        }
    }

    return @($values)
}

function Test-Net8ConsumableFramework {
    param([Parameter(Mandatory = $true)][string]$Framework)

    $normalized = $Framework.Trim().ToLowerInvariant()
    return $normalized -eq "net8.0" -or
        $normalized -eq "netstandard2.0" -or
        $normalized -eq "netstandard2.1"
}

function Get-RelativeDirectoryPrefix {
    param(
        [Parameter(Mandatory = $true)][string]$FromDirectory,
        [Parameter(Mandatory = $true)][string]$ToDirectory
    )

    $from = [Uri]([IO.Path]::GetFullPath($FromDirectory).TrimEnd('\') + '\')
    $to = [Uri]([IO.Path]::GetFullPath($ToDirectory).TrimEnd('\') + '\')
    $relative = [Uri]::UnescapeDataString($from.MakeRelativeUri($to).ToString())
    return $relative.Replace('/', '\')
}

foreach ($boundaryProject in $boundaryProjects) {
    $projectPath = Join-Path $root $boundaryProject.Path
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        throw "Boundary project was not found: $($boundaryProject.Path)"
    }

    $frameworks = @(Get-ProjectFrameworks -ProjectPath $projectPath)
    if ($frameworks.Count -ne 1 -or $frameworks[0] -ne "net8.0") {
        throw "$($boundaryProject.Path) must remain an isolated net8.0 project; found: $($frameworks -join ', ')."
    }

    [xml]$project = Get-Content -LiteralPath $projectPath -Raw
    $projectReferences = @($project.SelectNodes("//*[local-name()='ProjectReference']/@Include"))
    if ($projectReferences.Count -eq 0) {
        throw "$($boundaryProject.Path) no longer exposes project references; review whether the legacy boundary is still needed."
    }

    $incompatibleReferences = New-Object System.Collections.Generic.List[string]
    foreach ($reference in $projectReferences) {
        $referencePath = [IO.Path]::GetFullPath(
            (Join-Path (Split-Path -Parent $projectPath) $reference.Value))
        if (-not (Test-Path -LiteralPath $referencePath -PathType Leaf)) {
            throw "$($boundaryProject.Path) references a missing project: $($reference.Value)"
        }

        $referenceFrameworks = @(Get-ProjectFrameworks -ProjectPath $referencePath)
        $hasCompatibleTarget = $false
        foreach ($framework in $referenceFrameworks) {
            if (Test-Net8ConsumableFramework -Framework $framework) {
                $hasCompatibleTarget = $true
                break
            }
        }
        if (-not $hasCompatibleTarget) {
            $incompatibleReferences.Add(
                "$($reference.Value) [$($referenceFrameworks -join ';')]")
        }
    }

    if ($incompatibleReferences.Count -eq 0) {
        throw "$($boundaryProject.Path) no longer references an incompatible framework. Remove it from the isolation list instead of hiding a compatible project."
    }

    Write-Host "[BOUNDARY] $($boundaryProject.Name) net8.0 cannot join the legacy graph because of:"
    $incompatibleReferences | ForEach-Object { Write-Host "  - $_" }
}

$solution = [IO.File]::ReadAllText($sourceFullPath)
$sourceProjectCount = [regex]::Matches($solution, '(?m)^Project\(').Count
if ($sourceProjectCount -lt ($boundaryProjects.Count + $requiredLegacyProjects.Count)) {
    throw "Source solution contains too few projects to construct a safe legacy graph."
}

foreach ($boundaryProject in $boundaryProjects) {
    $name = [regex]::Escape($boundaryProject.Name)
    $path = [regex]::Escape($boundaryProject.Path)
    $guid = [regex]::Escape($boundaryProject.Guid)
    $projectBlockPattern =
        '(?ms)^Project\("[^\"]+"\)\s*=\s*"' + $name + '",\s*"' + $path + '",\s*"\{' + $guid + '\}"\r?\nEndProject\r?\n'
    $matches = [regex]::Matches($solution, $projectBlockPattern)
    if ($matches.Count -ne 1) {
        throw "Expected exactly one solution entry for $($boundaryProject.Name); found $($matches.Count)."
    }

    $solution = [regex]::Replace($solution, $projectBlockPattern, "")
    $guidLinePattern = '(?mi)^[^\r\n]*\{' + $guid + '\}[^\r\n]*(?:\r?\n|$)'
    $solution = [regex]::Replace($solution, $guidLinePattern, "")
}

foreach ($boundaryProject in $boundaryProjects) {
    if ($solution.IndexOf($boundaryProject.Name, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $solution.IndexOf($boundaryProject.Path, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $solution.IndexOf($boundaryProject.Guid, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Generated solution still contains boundary project metadata for $($boundaryProject.Name)."
    }
}

foreach ($requiredLegacyProject in $requiredLegacyProjects) {
    if ($solution.IndexOf($requiredLegacyProject, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Generated solution lost required server project: $requiredLegacyProject"
    }
}

$generatedProjectCount = [regex]::Matches($solution, '(?m)^Project\(').Count
$expectedProjectCount = $sourceProjectCount - $boundaryProjects.Count
if ($generatedProjectCount -ne $expectedProjectCount) {
    throw "Generated solution project count is $generatedProjectCount; expected $expectedProjectCount."
}

$destinationDirectory = Split-Path -Parent $destinationFullPath
if (-not (Test-Path -LiteralPath $destinationDirectory -PathType Container)) {
    New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
}

$repositoryPrefix = Get-RelativeDirectoryPrefix `
    -FromDirectory $destinationDirectory `
    -ToDirectory $rootFullPath
$projectLinePattern = [regex]::new(
    '(?m)^(?<Prefix>Project\("[^\"]+"\)\s*=\s*"[^\"]+",\s*")(?<Path>[^\"]+\.csproj)(?<Suffix>",\s*"\{[^\}]+\}")(?<Carriage>\r?)$',
    [Text.RegularExpressions.RegexOptions]::IgnoreCase)
$solution = $projectLinePattern.Replace($solution, {
    param($match)
    $projectPath = $match.Groups['Path'].Value
    if ([IO.Path]::IsPathRooted($projectPath)) {
        throw "Source solution contains an absolute project path: $projectPath"
    }

    return $match.Groups['Prefix'].Value +
        $repositoryPrefix +
        $projectPath +
        $match.Groups['Suffix'].Value +
        $match.Groups['Carriage'].Value
})

$rebasedRequiredProjects = @($requiredLegacyProjects | ForEach-Object { $repositoryPrefix + $_ })
foreach ($requiredLegacyProject in $rebasedRequiredProjects) {
    if ($solution.IndexOf($requiredLegacyProject, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Generated solution failed to rebase required project path: $requiredLegacyProject"
    }
}

$utf8Bom = New-Object System.Text.UTF8Encoding($true)
[IO.File]::WriteAllText($destinationFullPath, $solution, $utf8Bom)

Write-Host "[PASS] Generated legacy solution: $destinationFullPath"
Write-Host "[PASS] Rebased project paths through: $repositoryPrefix"
Write-Host "[PASS] Preserved $generatedProjectCount projects and isolated exactly $($boundaryProjects.Count) incompatible net8.0 module projects."
