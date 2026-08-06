param(
    [switch]$VerifyRestoredFiles
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$modernProjects = @(
    @{
        Path = "Data/NosGm.ChickenAPI/ChickenAPI.Events/ChickenAPI.Events.csproj"
        Packages = @{
            "log4net" = "3.3.2"
        }
    },
    @{
        Path = "Data/NosGm.ChickenAPI/ChickenAPI.Plugins/ChickenAPI.Plugins.csproj"
        Packages = @{
            "Autofac" = "9.3.2"
            "log4net" = "3.3.2"
            "Microsoft.Bcl.AsyncInterfaces" = "10.0.10"
            "System.Threading.Tasks.Extensions" = "4.6.3"
        }
    }
)

function New-MsBuildNamespaceManager([xml]$xml) {
    $namespace = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $namespace.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")
    return $namespace
}

foreach ($project in $modernProjects) {
    $path = Join-Path $root $project.Path
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required project was not found: $($project.Path)"
    }

    [xml]$xml = Get-Content -LiteralPath $path -Raw
    $namespace = New-MsBuildNamespaceManager $xml
    $legacyPackagePaths = $xml.SelectNodes(
        "//msb:HintPath[contains(translate(text(), 'PACKAGES', 'packages'), '\packages\')]",
        $namespace)
    if ($legacyPackagePaths.Count -gt 0) {
        $paths = ($legacyPackagePaths | ForEach-Object { $_.InnerText }) -join ", "
        throw "$($project.Path) still depends on repository-local package paths: $paths"
    }

    foreach ($packageName in $project.Packages.Keys) {
        $expectedVersion = $project.Packages[$packageName]
        $packageReferences = $xml.SelectNodes(
            "//msb:PackageReference[@Include='$packageName']",
            $namespace)
        if ($packageReferences.Count -ne 1) {
            throw "$($project.Path) must contain exactly one PackageReference for $packageName; found $($packageReferences.Count)."
        }

        $packageReference = $packageReferences[0]
        $actualVersion = $packageReference.GetAttribute("Version")
        if ([string]::IsNullOrWhiteSpace($actualVersion)) {
            $versionNode = $packageReference.SelectSingleNode("msb:Version", $namespace)
            $actualVersion = if ($null -eq $versionNode) { "" } else { $versionNode.InnerText }
        }

        if (-not [string]::Equals($actualVersion, $expectedVersion, [StringComparison]::Ordinal)) {
            throw "$($project.Path) uses $packageName $actualVersion; expected $expectedVersion."
        }

        $assemblyReferences = $xml.SelectNodes(
            "//msb:Reference[@Include='$packageName']",
            $namespace)
        if ($assemblyReferences.Count -ne 0) {
            throw "$($project.Path) mixes PackageReference and assembly Reference for $packageName."
        }

        Write-Host "[PASS] $($project.Path): PackageReference $packageName $actualVersion"
    }
}

$solutionPath = Join-Path $root "NosGm.sln"
if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
    throw "NosGm.sln was not found."
}

$solutionText = Get-Content -LiteralPath $solutionPath -Raw
$projectPattern = [regex]::new(
    'Project\("[^\"]+"\)\s*=\s*"[^\"]+",\s*"(?<Path>[^\"]+\.csproj)"',
    [Text.RegularExpressions.RegexOptions]::IgnoreCase)
$solutionProjects = @($projectPattern.Matches($solutionText) | ForEach-Object {
    $_.Groups["Path"].Value
})
if ($solutionProjects.Count -eq 0) {
    throw "NosGm.sln did not expose any C# projects."
}

$packageFolderPattern = [regex]::new(
    '(?:^|[\\/])packages[\\/](?<Folder>[^\\/\)\s]+)(?:[\\/]|$)',
    [Text.RegularExpressions.RegexOptions]::IgnoreCase)
$relativePackageFolderPattern = [regex]::new(
    '(?<Relative>(?:\.\.[\\/])+packages[\\/](?<Folder>[^\\/\)\s]+))',
    [Text.RegularExpressions.RegexOptions]::IgnoreCase)
$invalidReferences = New-Object System.Collections.Generic.List[string]
$legacyReferenceCount = 0

foreach ($relativeProjectPath in $solutionProjects) {
    $projectPath = Join-Path $root ($relativeProjectPath -replace '\\', [IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        $invalidReferences.Add("Solution project was not found: $relativeProjectPath")
        continue
    }

    [xml]$projectXml = Get-Content -LiteralPath $projectPath -Raw
    $namespace = New-MsBuildNamespaceManager $projectXml
    $projectDirectory = Split-Path -Parent $projectPath
    $hintPaths = @($projectXml.SelectNodes(
        "//msb:HintPath[contains(translate(text(), 'PACKAGES', 'packages'), 'packages')]",
        $namespace) | Where-Object {
            $packageFolderPattern.IsMatch($_.InnerText)
        })

    $slowCheetahTargetPath = Join-Path $projectDirectory "Properties/SlowCheetah/SlowCheetah.Transforms.targets"
    $slowCheetahToolsPathNode = $projectXml.SelectSingleNode("//msb:SlowCheetahToolsPath", $namespace)
    $hasActiveSlowCheetah =
        $null -ne $slowCheetahToolsPathNode -and
        (Test-Path -LiteralPath $slowCheetahTargetPath -PathType Leaf)

    if ($hintPaths.Count -eq 0 -and -not $hasActiveSlowCheetah) {
        continue
    }

    $packagesConfigPath = Join-Path $projectDirectory "packages.config"
    if (-not (Test-Path -LiteralPath $packagesConfigPath -PathType Leaf)) {
        $invalidReferences.Add(
            "$relativeProjectPath uses repository-local package assets but has no packages.config.")
        continue
    }

    [xml]$packagesXml = Get-Content -LiteralPath $packagesConfigPath -Raw
    $declaredPackages = @($packagesXml.packages.package)
    $duplicatePackageIds = @($declaredPackages |
        Group-Object -Property id |
        Where-Object Count -gt 1)
    if ($duplicatePackageIds.Count -gt 0) {
        $invalidReferences.Add(
            "$relativeProjectPath packages.config contains duplicate ids: $(($duplicatePackageIds.Name -join ', ')).")
    }

    foreach ($hintPathNode in $hintPaths) {
        $legacyReferenceCount++
        $hintPath = $hintPathNode.InnerText.Trim()
        $folderMatch = $packageFolderPattern.Match($hintPath)
        if (-not $folderMatch.Success) {
            $invalidReferences.Add("$relativeProjectPath contains an unreadable package HintPath: $hintPath")
            continue
        }

        $packageFolder = $folderMatch.Groups["Folder"].Value
        $matches = @($declaredPackages | Where-Object {
            $declaredFolder = "$([string]$_.id).$([string]$_.version)"
            [string]::Equals($declaredFolder, $packageFolder, [StringComparison]::OrdinalIgnoreCase)
        })
        if ($matches.Count -ne 1) {
            $invalidReferences.Add(
                "$relativeProjectPath points to package folder $packageFolder, but packages.config declares it $($matches.Count) time(s).")
            continue
        }

        if ($VerifyRestoredFiles) {
            $resolvedHintPath = [IO.Path]::GetFullPath((Join-Path $projectDirectory $hintPath))
            if (-not (Test-Path -LiteralPath $resolvedHintPath -PathType Leaf)) {
                $invalidReferences.Add(
                    "$relativeProjectPath restored package file is missing: $resolvedHintPath")
                continue
            }
        }

        Write-Host "[PASS] $relativeProjectPath: packages.config backs $packageFolder"
    }

    if ($hasActiveSlowCheetah) {
        $legacyReferenceCount++
        $toolsPath = $slowCheetahToolsPathNode.InnerText.Trim()
        $folderMatch = $packageFolderPattern.Match($toolsPath)
        if (-not $folderMatch.Success) {
            $invalidReferences.Add(
                "$relativeProjectPath contains an unreadable SlowCheetahToolsPath: $toolsPath")
            continue
        }

        $packageFolder = $folderMatch.Groups["Folder"].Value
        $matches = @($declaredPackages | Where-Object {
            $declaredFolder = "$([string]$_.id).$([string]$_.version)"
            [string]::Equals($declaredFolder, $packageFolder, [StringComparison]::OrdinalIgnoreCase)
        })
        if ($matches.Count -ne 1) {
            $invalidReferences.Add(
                "$relativeProjectPath loads SlowCheetah from $packageFolder, but packages.config declares it $($matches.Count) time(s).")
            continue
        }

        $slowCheetahImport = $projectXml.SelectSingleNode(
            "//msb:Import[@Label='SlowCheetah']",
            $namespace)
        if ($null -eq $slowCheetahImport) {
            $invalidReferences.Add(
                "$relativeProjectPath contains active SlowCheetah targets but no labeled SlowCheetah import.")
            continue
        }

        if ($VerifyRestoredFiles) {
            $relativeFolderMatch = $relativePackageFolderPattern.Match($toolsPath)
            if (-not $relativeFolderMatch.Success) {
                $invalidReferences.Add(
                    "$relativeProjectPath cannot resolve SlowCheetah package directory from: $toolsPath")
                continue
            }

            $packageDirectory = [IO.Path]::GetFullPath(
                (Join-Path $projectDirectory $relativeFolderMatch.Groups["Relative"].Value))
            $requiredTools = @(
                "Microsoft.Web.XmlTransform.dll",
                "SlowCheetah.NuGet.template.proj",
                "SlowCheetah.Transforms.targets",
                "SlowCheetah.Xdt.dll"
            )
            foreach ($requiredTool in $requiredTools) {
                $toolPath = Join-Path (Join-Path $packageDirectory "tools") $requiredTool
                if (-not (Test-Path -LiteralPath $toolPath -PathType Leaf)) {
                    $invalidReferences.Add(
                        "$relativeProjectPath restored SlowCheetah tool is missing: $toolPath")
                }
            }
        }

        Write-Host "[PASS] $relativeProjectPath: packages.config backs active $packageFolder build tools"
    }
}

if ($invalidReferences.Count -gt 0) {
    throw "Invalid legacy package references remain:`n$($invalidReferences -join [Environment]::NewLine)"
}

$mode = if ($VerifyRestoredFiles) { "declarations and restored files" } else { "declarations" }
Write-Host "Validated $legacyReferenceCount solution package-backed asset entries ($mode)."
Write-Host "ChickenAPI PackageReference declarations are unique, and classic solution binaries plus active build tools have exact packages.config ownership."
