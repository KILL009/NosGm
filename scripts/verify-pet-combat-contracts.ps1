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
$upet = Read-Source "Data/NosGm.Handler/PacketHandler/Mate/UpetPacketHandler.cs"
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

Assert-Contains $mateAttack 'mate.TargetHit(target, null);' `
    "Pet behavior-tree combat is restricted to the dedicated basic attack path"
Assert-Contains $mateAttack 'mate.Monster.BasicRange <= 0' `
    "Zero-range melee pets retain an effective one-cell attack range"
Assert-Contains $mateAttack 'target.MapMonster.AddToAggroList(mate.BattleEntity)' `
    "Pet attacks generate threat for the pet"
Assert-Contains $mateAttack 'target.MapMonster.Target = mate.BattleEntity' `
    "Attacked monsters can immediately switch onto the pet"
Assert-Contains $mateAttack 'Source=AI Action=Basic' `
    "Automatic pet basic attacks expose their execution source in debug logs"
Assert-NotContains $mateAttack 'SelectReadySpecialSkill' `
    "The behavior tree cannot schedule pet special skills"
Assert-NotContains $mateAttack 'mate.PSkills' `
    "The behavior-tree attack node cannot inspect the pet special-skill collection"
Assert-NotContains $mateAttack 'selectedSkill' `
    "The behavior-tree attack node cannot forward a selected special skill"
Assert-NotContains $mateAttack 'Skill.Cooldown' `
    "Special cooldowns cannot become behavior-tree action locks"

Assert-Contains $suctl 'attacker.TargetHit(target, null);' `
    "Client-driven suctl commands always use the dedicated basic attack path"
Assert-Contains $suctl 'Source=SUCTL Action=Basic' `
    "Client-driven pet basic attacks expose their execution source"
Assert-NotContains $suctl '1000 * s.Skill.Cooldown' `
    "Client-driven basic attacks are not filtered by a special skill cooldown"
Assert-NotContains $suctl 's.Rate == 0' `
    "A zero-rate manual pet skill cannot replace the normal attack"

Assert-Contains $upet 'mate.MateTransportId == upetPacket.MateTransportId' `
    "u_pet resolves the exact pet identified by the client packet"
Assert-Contains $upet 'attacker.PSkills?' `
    "Special cooldown state comes from per-mate skill instances"
Assert-Contains $upet 'mateSkill.LastSkillUse = DateTime.Now;' `
    "Successful manual pet skills update only their own cooldown timer"
Assert-Contains $upet 'Source=UPET Action=Special' `
    "Manual pet special skills expose their execution source"
Assert-NotContains $upet 'attacker.Monster.Skills.FirstOrDefault' `
    "u_pet does not mutate a shared monster-template cooldown"
Assert-NotContains $upet 'SkillVNum = 200' `
    "Missing pet skills fail closed instead of fabricating a fallback skill"

Assert-Contains $mate 'if (!CanUseBasicSkill())' `
    "Mate.TargetHit keeps the dedicated basic attack cooldown check"
Assert-Contains $mate 'LastBasicSkillUse = DateTime.Now;' `
    "Successful basic attacks update their own timer"
Assert-Contains $character 'Mates.Where(x => x.IsTeamMember && x.IsAlive)' `
    "Only active living mates receive combat experience"
Assert-Contains $character 'mate.GenerateXp(xp);' `
    "Combat experience is forwarded to active mates"

Write-Host "Pet combat authority, tanking, cooldown and experience contracts passed."
