[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$searchRoots = @(
    (Join-Path $root "Data"),
    (Join-Path $root "Launcher"),
    (Join-Path $root "NosGm.SCS"),
    (Join-Path $root "Web")
) | Where-Object { Test-Path -LiteralPath $_ -PathType Container }

$removed = 0
foreach ($searchRoot in $searchRoots) {
    $objDirectories = @(
        Get-ChildItem `
            -LiteralPath $searchRoot `
            -Directory `
            -Filter obj `
            -Recurse `
            -Force `
            -ErrorAction SilentlyContinue
    )

    foreach ($objDirectory in $objDirectories) {
        Write-Host "[CLEAN] Removing stale intermediate state: $($objDirectory.FullName)"
        Remove-Item -LiteralPath $objDirectory.FullName -Recurse -Force
        $removed++
    }
}

Write-Host "[CLEAN] Removed $removed obj director$(if ($removed -eq 1) { 'y' } else { 'ies' })."
Write-Host "[CLEAN] Source files, packages, settings, certificates, artifacts and compiled bin outputs were not modified."
