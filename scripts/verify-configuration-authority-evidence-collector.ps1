[CmdletBinding()]
param(
    [string]$CollectorPath = "scripts/collect-configuration-authority-evidence.ps1",
    [string]$DocumentationPath = "docs/configuration-grpc-slice.md"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

function Read-RepoFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = Join-Path $root $Path
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Required repository file does not exist: $Path"
    }
    return Get-Content -LiteralPath $fullPath -Raw
}

function Require {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Needle,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw $Message
    }
}

function Forbid {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Needle,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw $Message
    }
}

$collector = Read-RepoFile $CollectorPath
$documentation = Read-RepoFile $DocumentationPath

foreach ($required in @(
    '[ValidateSet("Qualification", "LiveEffects")]',
    '[ValidateSet(3)]',
    '[CONFIG_GRPC_AUTHORITY_STATE]',
    '[CONFIG_GRPC_PARITY]',
    'Evidence must belong to exactly one World process generation.',
    'Terminal Configuration parity evidence is present.',
    'Get-QualifyingParityRuntimes',
    '$record.ScsLive -ne $record.GrpcLive',
    '$record.ScsLive -ne $record.Matched',
    '$record.Evicted -ne 0',
    '$latest.Active -ceq $latest.Recovered',
    '$qualifiedIds -cnotcontains $latest.Active',
    'No effect-authorized recovered typed-ingress state was found.',
    '$_.State -ceq "RolledBack"',
    '$_.DuplicateSuppressed -ge 2',
    '$_.StreamEnds -gt $active.StreamEnds',
    'configuration-authority-qualification',
    'configuration-authority-live-effects',
    'Write-EvidenceReceiptAtomically',
    '[IO.FileMode]::CreateNew',
    '[IO.FileShare]::None',
    'ConvertTo-Json -Depth 6',
    'Cross-process evidence was not rejected.',
    'Terminal parity evidence was not rejected.',
    'Live evidence without duplicate suppression was not rejected.',
    'Atomic receipt creation did not reject an existing destination.',
    'Atomic receipt collision changed the original receipt.',
    'Configuration authority evidence collector self-test passed.'
)) {
    Require $collector $required `
        "Configuration evidence collector contract is missing '$required'."
}

foreach ($forbidden in @(
    'Get-CimInstance Win32_Process',
    'Win32_Process -Filter',
    'GetEnvironmentVariables',
    'Copy-Item -LiteralPath $path',
    'Remove-Item -LiteralPath $OutputPath',
    'sourcePath =',
    'sourcePaths =',
    'rawLine =',
    'rawLines =',
    '[ValidateRange(3, 16)]',
    '[IO.File]::WriteAllText('
)) {
    Forbid $collector $forbidden `
        "Configuration evidence collector contains forbidden behavior '$forbidden'."
}

foreach ($requiredDocumentation in @(
    './scripts/collect-configuration-authority-evidence.ps1 -Mode Qualification',
    './scripts/collect-configuration-authority-evidence.ps1 -Mode LiveEffects',
    'one World process generation',
    'three distinct parity',
    'fourth activation runtime',
    'does not authorize SCS removal'
)) {
    Require $documentation $requiredDocumentation `
        "Configuration evidence documentation is missing '$requiredDocumentation'."
}

& (Join-Path $root $CollectorPath) -Mode Qualification -SelfTest

Write-Host "Configuration authority evidence collector contract passed." -ForegroundColor Green
