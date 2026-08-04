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
$mateDiagnostics = Read-Source "Data/NosGm.GameObject/Helpers/MateCombatDiagnostics.cs"
$packetHelper = Read-Source "Data/NosGm.GameObject/Helpers/StaticPacketHelper.cs"
$suctl = Read-Source "Data/NosGm.Handler/PacketHandler/Mate/SuctlPacketHandler.cs"
$upet = Read-Source "Data/NosGm.Handler/PacketHandler/Mate/UpetPacketHandler.cs"
$mate = Read-Source "Data/NosGm.GameObject/Mate.cs"
$character = Read-Source "Data/NosGm.GameObject/Character.cs"
$healingBurning = Read-Source "Data/NosGm.GameObject/Plugin/BCard/Handler/HealingBurningAndCastingHandler.cs"

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
Assert-Contains $mateAttack 'MateCombatDiagnostics.BeginBasicAttack' `
    "Automatic pet basic attacks are marked for client-safe packet encoding"
Assert-Contains $mateAttack 'MateCombatDiagnostics.ObserveExperienceAfterAttack' `
    "Automatic pet attacks expose post-kill experience state"
Assert-NotContains $mateAttack 'SelectReadySpecialSkill' `
    "The behavior tree cannot schedule pet special skills"
Assert-NotContains $mateAttack 'mate.PSkills' `
    "The behavior-tree attack node cannot inspect the pet special-skill collection"
Assert-NotContains $mateAttack 'selectedSkill' `
    "The behavior-tree attack node cannot forward a selected special skill"
Assert-NotContains $mateAttack 'Skill.Cooldown' `
    "Special cooldowns cannot become behavior-tree action locks"

Assert-Contains $mateDiagnostics 'Logger.Info(' `
    "Pet diagnostics remain visible in Release logging"
Assert-Contains $mateDiagnostics '[MATE_COMBAT]' `
    "Pet basic attacks expose their execution source"
Assert-Contains $mateDiagnostics '[MATE_XP]' `
    "Pet experience changes expose before, after and required values"
Assert-Contains $mateDiagnostics 'AwardedPetKills' `
    "Concurrent attack probes cannot award the same kill repeatedly"
Assert-Contains $mateDiagnostics 'CharacterHelper.ExperiencePenalty(mate.Level, monsterLevel)' `
    "Fallback pet experience uses the pet level instead of the owner level"
Assert-Contains $mateDiagnostics 'mate.GenerateXp(petXp);' `
    "A confirmed pet kill can award independent pet experience"
Assert-Contains $mateDiagnostics 'mate.Owner.Session.SendPacket(mate.GenerateScPacket());' `
    "Pet experience immediately refreshes the client pet panel"
Assert-Contains $mateDiagnostics '[MATE_XP_REWARD]' `
    "Independent pet experience awards are visible in runtime logs"
Assert-Contains $mateDiagnostics 'PendingBasicAttackPackets' `
    "Only explicitly marked basic attacks are normalized"
Assert-NotContains $mateDiagnostics 'skillVNum <= 0' `
    "Pets whose database BasicSkill is zero still receive safe packet normalization"

Assert-Contains $packetHelper 'MateCombatDiagnostics.TryConsumeBasicAttackPacket' `
    "Pet basic packet normalization is connected to packet serialization"
Assert-Contains $packetHelper 'skillEffect = skillVNum;' `
    "The original pet basic skill becomes the client effect"
Assert-Contains $packetHelper 'skillVNum = 0;' `
    "Client-safe pet basics use packet skill zero"
Assert-Contains $packetHelper 'attackAnimation = 11;' `
    "Client-safe pet basics use the NPC attack animation"
Assert-Contains $packetHelper 'x = 0;' `
    "Client-safe pet basics use the legacy zero X coordinate"
Assert-Contains $packetHelper 'y = 0;' `
    "Client-safe pet basics use the legacy zero Y coordinate"

Assert-Contains $suctl 'attacker.TargetHit(target, null);' `
    "Client-driven suctl commands always use the dedicated basic attack path"
Assert-Contains $suctl 'MateCombatDiagnostics.BeginBasicAttack' `
    "Client-driven pet basics are marked and logged"
Assert-Contains $suctl 'attacker.Monster.BasicRange <= 0' `
    "Client-driven melee pet range matches automatic AI range"
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
Assert-Contains $upet 'Logger.Info(' `
    "Manual pet special diagnostics remain visible in Release logging"
Assert-Contains $upet 'ApplyPositiveOwnerBuffs(attacker, battleEntityDefender, skill);' `
    "Pet support skills evaluate the actual client-selected target"
Assert-Contains $upet 'targetsPetItself' `
    "Self-target pet support layouts are recognized"
Assert-Contains $upet 'Result=AppliedToOwner' `
    "Owner buff routing is observable during runtime tests"
Assert-Contains $upet 'ScheduleAttractionRelease(' `
    "Sushi Party attraction has a bounded release lifecycle"
Assert-Contains $upet 'monster.RemoveFromAggroList(pet);' `
    "Expired artificial pet aggro is removed"
Assert-Contains $upet 'Result=Released' `
    "Sushi Party attraction release is observable"
Assert-NotContains $upet 'attacker.Monster.Skills.FirstOrDefault' `
    "u_pet does not mutate a shared monster-template cooldown"
Assert-NotContains $upet 'SkillVNum = 200' `
    "Missing pet skills fail closed instead of fabricating a fallback skill"

Assert-Contains $healingBurning 'int disposableKey = bCardId > 0 ? bCardId : cardId.Value;' `
    "Periodic poison and burning effects use the same BCard key as buff removal"
Assert-Contains $healingBurning 'if (!target.HasBuff(cardId.Value))' `
    "Orphan periodic effects stop when their visible owning buff is gone"
Assert-Contains $healingBurning 'target.Mate.Owner.Session.SendPackets(target.Mate.Owner.GeneratePst());' `
    "Mate HP and effect state refresh after every periodic tick or removal"
Assert-NotContains $healingBurning 'int disposableKey = cardId.Value;' `
    "Periodic effects cannot survive under a mismatched CardId key"

Assert-Contains $mate 'if (!CanUseBasicSkill())' `
    "Mate.TargetHit keeps the dedicated basic attack cooldown check"
Assert-Contains $mate 'LastBasicSkillUse = DateTime.Now;' `
    "Successful basic attacks update their own timer"
Assert-Contains $character 'Mates.Where(x => x.IsTeamMember && x.IsAlive)' `
    "Only active living mates receive the normal character kill reward"
Assert-Contains $character 'mate.GenerateXp(xp);' `
    "Normal character kill experience is still forwarded to active mates"

Write-Host "Pet combat, taunt release, mate debuff lifecycle, model visibility, owner buffs and experience contracts passed."
