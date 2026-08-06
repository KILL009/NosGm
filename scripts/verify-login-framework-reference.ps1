param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$rootPath = (Resolve-Path -LiteralPath $root).Path.TrimEnd('\') + '\'
$programRoot = Join-Path $root "Data/NosGm.Program"
$requiredProjects = @(
    "Data/NosGm.Program/NosGm.Login/NosGm.Login.csproj",
    "Data/NosGm.Program/NosGm.Parser/NosGm.Parser.csproj",
    "Data/NosGm.Program/NosGm.World/NosGm.World.csproj"
)
$expectedFramework = "v4.8.1"
$expectedBootstrapper = ".NETFramework,Version=v4.8.1"

function Get-RepositoryRelativePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = (Resolve-Path -LiteralPath $Path).Path
    if (-not $fullPath.StartsWith($rootPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes the repository root: $fullPath"
    }

    return $fullPath.Substring($rootPath.Length).Replace('\', '/')
}

foreach ($relativePath in $requiredProjects) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required classic executable project is missing: $relativePath"
    }
}

$projects = @(Get-ChildItem -LiteralPath $programRoot -Recurse -Filter *.csproj -File |
    ForEach-Object {
        [xml]$xml = Get-Content -LiteralPath $_.FullName -Raw
        $namespace = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $namespace.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")
        $bootstrapperEnabled = $xml.SelectSingleNode("//msb:BootstrapperEnabled", $namespace)
        if ($null -ne $bootstrapperEnabled -and $bootstrapperEnabled.InnerText -eq "true") {
            [pscustomobject]@{
                File = $_
                Xml = $xml
                Namespace = $namespace
            }
        }
    })

if ($projects.Count -lt $requiredProjects.Count) {
    throw "Expected at least $($requiredProjects.Count) classic executable projects with BootstrapperEnabled=true; found $($projects.Count)."
}

$discoveredRelativePaths = @($projects | ForEach-Object {
    Get-RepositoryRelativePath -Path $_.File.FullName
})
$missingRequired = @($requiredProjects | Where-Object { $_ -notin $discoveredRelativePaths })
if ($missingRequired.Count -gt 0) {
    throw "Required projects are not covered by bootstrapper validation: $($missingRequired -join ', ')"
}

foreach ($entry in $projects) {
    $project = $entry.Xml
    $namespace = $entry.Namespace
    $relativePath = Get-RepositoryRelativePath -Path $entry.File.FullName
    $name = [IO.Path]::GetFileNameWithoutExtension($entry.File.Name)

    $outputType = $project.SelectSingleNode("//msb:OutputType", $namespace)
    if ($null -eq $outputType -or $outputType.InnerText -notin @("Exe", "WinExe")) {
        throw "$relativePath enables a bootstrapper but is not an executable project."
    }

    $targetFramework = $project.SelectSingleNode("//msb:TargetFrameworkVersion", $namespace)
    if ($null -eq $targetFramework -or $targetFramework.InnerText -ne $expectedFramework) {
        $actual = if ($null -eq $targetFramework) { "<missing>" } else { $targetFramework.InnerText }
        throw "$relativePath must target .NET Framework $expectedFramework; found $actual."
    }

    $systemReferences = $project.SelectNodes("//msb:Reference[@Include='System']", $namespace)
    if ($systemReferences.Count -ne 1) {
        throw "$relativePath must declare exactly one System framework reference; found $($systemReferences.Count)."
    }
    if ($null -ne $systemReferences[0].SelectSingleNode("msb:HintPath", $namespace)) {
        throw "$relativePath must resolve System from the installed targeting pack, not HintPath."
    }

    $forbiddenHintPaths = @($project.SelectNodes("//msb:HintPath", $namespace) |
        Where-Object {
            $_.InnerText -match '(?i)Program Files|Reference Assemblies|\\\.NETFramework\\v[0-9]|/\.NETFramework/v[0-9]'
        })
    if ($forbiddenHintPaths.Count -gt 0) {
        $details = ($forbiddenHintPaths | ForEach-Object { $_.InnerText }) -join ", "
        throw "$relativePath contains machine-specific framework HintPath entries: $details"
    }

    $frameworkBootstrappers = $project.SelectNodes(
        "//msb:BootstrapperPackage[starts-with(@Include, '.NETFramework,Version=')]",
        $namespace)
    if ($frameworkBootstrappers.Count -ne 1) {
        throw "$relativePath must declare exactly one .NET Framework bootstrapper; found $($frameworkBootstrappers.Count)."
    }

    $bootstrapper = $frameworkBootstrappers[0]
    if ($bootstrapper.Include -ne $expectedBootstrapper) {
        throw "$relativePath bootstrapper must be $expectedBootstrapper; found $($bootstrapper.Include)."
    }

    $installNode = $bootstrapper.SelectSingleNode("msb:Install", $namespace)
    if ($null -eq $installNode -or $installNode.InnerText -ne "true") {
        throw "$relativePath must enable installation of the .NET Framework 4.8.1 prerequisite."
    }

    $productName = $bootstrapper.SelectSingleNode("msb:ProductName", $namespace)
    if ($null -eq $productName -or $productName.InnerText -notmatch '4\.8\.1') {
        throw "$relativePath bootstrapper ProductName must advertise .NET Framework 4.8.1."
    }

    Write-Host "[PASS] $name targets and deploys .NET Framework 4.8.1 without machine-specific framework paths."
}

Write-Host "[PASS] Validated $($projects.Count) classic executable bootstrapper project(s)."
