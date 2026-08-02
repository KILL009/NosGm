param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required source file was not found: $relativePath"
    }

    return Get-Content -LiteralPath $path -Raw
}

function Assert-Contains([string]$source, [string]$expected, [string]$name) {
    if (-not $source.Contains($expected)) {
        throw "$name failed. Missing source contract: $expected"
    }

    Write-Host "[PASS] $name"
}

$hasTarget = Read-Source "Data/NosGm.GameObject/AI/Conditions/HasTargetCondition.cs"
$findTarget = Read-Source "Data/NosGm.GameObject/AI/Actions/FindTargetNode.cs"
$ownerTargeted = Read-Source "Data/NosGm.GameObject/AI/Conditions/OwnerIsTargetedCondition.cs"
$mateAttack = Read-Source "Data/NosGm.GameObject/AI/Actions/MateAttackTargetNode.cs"
$character = Read-Source "Data/NosGm.GameObject/Character.cs"

Assert-Contains $hasTarget 'target.Hp > 0' `
    "Mob targets are validated by life state instead of requiring a player character"
Assert-Contains $hasTarget 'entity.Target = target' `
    "Behavior-tree target is mirrored to the legacy monster target"
Assert-Contains $findTarget 'entity.AggroList?' `
    "Mob AI considers the combat aggro list"
Assert-Contains $findTarget 'candidate.MapInstance == entity.MapInstance' `
    "Stale cross-map aggro targets are rejected"
Assert-Contains $ownerTargeted 'TargettedByMonstersList(true)' `
    "Pet defence includes attacks involving the owner team"
Assert-Contains $mateAttack 'target.MapMonster.AddToAggroList(mate.BattleEntity)' `
    "Pet attacks generate threat for the pet"
Assert-Contains $mateAttack 'target.MapMonster.Target = mate.BattleEntity' `
    "Attacked monsters can immediately switch onto the pet"
Assert-Contains $character 'Mates.Where(x => x.IsTeamMember && x.IsAlive)' `
    "Only active living mates receive combat experience"
Assert-Contains $character 'mate.GenerateXp(xp);' `
    "Combat experience is forwarded to active mates"

Write-Host "Pet combat, tanking and experience contracts passed."
