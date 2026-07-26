param(
    [string]$ProjectPath = "Data/NosGm.Program/NosGm.World/NosGm.World.csproj"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ProjectPath)) {
    throw "World project not found: $ProjectPath"
}

[xml]$project = Get-Content -LiteralPath $ProjectPath -Raw
$namespace = New-Object System.Xml.XmlNamespaceManager($project.NameTable)
$namespace.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")

$releaseGroup = $project.SelectSingleNode(
    "//msb:PropertyGroup[contains(@Condition, 'Release|AnyCPU')]",
    $namespace)

if ($null -eq $releaseGroup) {
    throw "Release|AnyCPU property group was not found in $ProjectPath"
}

$platformTarget = [string]$releaseGroup.PlatformTarget
$prefer32Bit = [string]$releaseGroup.Prefer32Bit

if ($platformTarget -ne "x64") {
    throw "World Release must target x64. Current PlatformTarget='$platformTarget'."
}

if ($prefer32Bit -ne "false") {
    throw "World Release must set Prefer32Bit=false. Current Prefer32Bit='$prefer32Bit'."
}

Write-Host "World Release runtime verified: PlatformTarget=x64, Prefer32Bit=false."
