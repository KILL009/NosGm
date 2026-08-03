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

function Assert-NotContains([string]$source, [string]$unexpected, [string]$name) {
    if ($source.Contains($unexpected)) {
        throw "$name failed. Forbidden source contract remains: $unexpected"
    }

    Write-Host "[PASS] $name"
}

$hasTarget = Read-Source "Data/NosGm.GameObject/AI/Conditions/HasTargetCondition.cs"
$findTarget = Read-Source "Data/NosGm.GameObject/AI/Actions/FindTargetNode.cs"
$ownerTargeted = Read-Source "Data/NosGm.GameObject/AI/Conditions/OwnerIsTargetedCondition.cs"
$mateAttack = Read-Source "Data/NosGm.GameObject/AI/Actions/MateAttackTargetNode.cs"
$suctl = Read-Source "Data/NosGm.Handler/PacketHandler/Mate/SuctlPacketHandler.cs"
$mate = Read-Source "Data/NosGm.GameObject/Mate.cs"
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
Assert-Contains $mateAttack 'private const int BasicAttackRecoveryMilliseconds = 1500;' `
    "Pet basic attacks use a short independent recovery"
Assert-Contains $mateAttack 'mate.TargetHit(target, selectedSkill);' `
    "Pet AI routes a null selected skill into the dedicated basic attack path"
Assert-Contains $mateAttack '.Where(skill => skill.SkillVNum != mate.Monster.BasicSkill)' `
    "The basic attack is excluded from the special skill scheduler"
Assert-Contains $mateAttack '.Where(skill => skill.CanBeUsed())' `
    "Only ready special skills may be selected"
Assert-NotContains $mateAttack '_skill.Skill.Cooldown * 100' `
    "Special skill cooldown never becomes an AI action lock"
Assert-NotContains $mateAttack 'mateSkills.FirstOrDefault(s => s != null)' `
    "Pet AI cannot fall back to a special skill that is still cooling down"
Assert-Contains $suctl 'attacker.TargetHit(target, null);' `
    "Client-driven suctl commands always use the dedicated basic attack path"
Assert-NotContains $suctl '1000 * s.Skill.Cooldown' `
    "Client-driven basic attacks are not filtered by a special skill cooldown"
Assert-NotContains $suctl 's.Rate == 0' `
    "A zero-rate manual pet skill cannot replace the normal attack"
Assert-NotContains $suctl 'attacker.TargetHit(target.BattleEntity, skill);' `
    "The client pet attack handler never forwards a selected special skill"
Assert-Contains $mate 'if (!CanUseBasicSkill())' `
    "Mate.TargetHit keeps the dedicated basic attack cooldown check"
Assert-Contains $mate 'LastBasicSkillUse = DateTime.Now;' `
    "Successful basic attacks update their own timer"
Assert-Contains $character 'Mates.Where(x => x.IsTeamMember && x.IsAlive)' `
    "Only active living mates receive combat experience"
Assert-Contains $character 'mate.GenerateXp(xp);' `
    "Combat experience is forwarded to active mates"

Write-Host "Pet combat, tanking, client basic attacks and experience contracts passed."
