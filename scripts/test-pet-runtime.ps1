param(
    [string]$WorldLog = ""
)

$ErrorActionPreference = "Stop"

Write-Host "Pet runtime acceptance checklist"
Write-Host "1. Use a pet whose level is below the owner level."
Write-Host "2. Let a hostile monster attack the owner."
Write-Host "3. Confirm the pet attacks without a repeated manual order."
Write-Host "4. Confirm the monster can switch target and damage the pet."
Write-Host "5. Kill several monsters while the pet is active and alive."
Write-Host "6. Confirm the pet Experience value and percentage increase."
Write-Host "7. Confirm level-up occurs while pet level is below owner level."

if ([string]::IsNullOrWhiteSpace($WorldLog)) {
    Write-Host "No World log was supplied. Checklist printed only."
    exit 0
}

if (-not (Test-Path -LiteralPath $WorldLog -PathType Leaf)) {
    throw "World log was not found: $WorldLog"
}

$interesting = Select-String -Path $WorldLog -Pattern "MATE_AI_ERROR|sc_p |pst 2 |su 2 |st 2 "
if ($interesting) {
    Write-Host "Relevant pet diagnostics:"
    $interesting | Select-Object -Last 100 | ForEach-Object { Write-Host $_.Line }
} else {
    Write-Warning "No pet diagnostics were found in the supplied World log."
}
