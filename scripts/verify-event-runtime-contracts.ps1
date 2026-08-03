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

$eventContainer = Read-Source "Data/NosGm.GameObject/Event/EventContainer.cs"
$eventHandler = Read-Source "Data/NosGm.GameObject/Plugin/Event/GameEventHandler.cs"
$eventService = Read-Source "Data/NosGm.GameObject/Plugin/Event/EventServiceHandler.cs"
$eventGuard = Read-Source "Data/NosGm.GameObject/Plugin/Event/EventRuntimeGuard.cs"
$instantBattle = Read-Source "Data/NosGm.GameObject/Plugin/Event/Handler/GameEvent/InstantBattle/InstantBattleRuntime.cs"
$instantBattleCatalog = Read-Source "Data/NosGm.GameObject/Plugin/Event/Handler/GameEvent/InstantBattle/InstantBattleWaveCatalog.cs"
$upet = Read-Source "Data/NosGm.Handler/PacketHandler/Mate/UpetPacketHandler.cs"

Assert-Contains $eventContainer 'param is IEnumerable<MonsterToSummon> monsters' `
    "Event monster collections are normalized at the EventContainer boundary"
Assert-Contains $eventContainer 'Parameter = monsters.ToList();' `
    "SPAWNMONSTERS always reaches EventHelper as a concrete list"

Assert-Contains $eventHandler 'lock (EventStateSync)' `
    "Event start state is synchronized"
Assert-Contains $eventHandler 'CompleteEvent(type);' `
    "Failed or unsupported events can release their start marker"
Assert-Contains $eventHandler 'InstantBattleRuntime.GenerateInstantBattle();' `
    "Instant Battle uses the repaired runtime"
Assert-Contains $eventHandler 'Result=Failed' `
    "Event dispatcher exceptions are observable"
Assert-Contains $eventHandler 'Result=UnsupportedLocalDispatch' `
    "Unwired event types are reported instead of silently discarded"
Assert-NotContains $eventHandler 'InstantBattle.GenerateInstantBattle();' `
    "The obsolete Instant Battle implementation is unreachable"

Assert-Contains $eventGuard 'public static void Run' `
    "Recurring event actions have a shared exception boundary"
Assert-Contains $eventService 'EventRuntimeGuard.Protect<long>' `
    "Recurring world event streams use guarded callbacks"
Assert-Contains $eventService 'EventRuntimeGuard.ObserveFailure' `
    "Observable failures are logged"

Assert-Contains $instantBattle 'await Task.Delay(delay).ConfigureAwait(false);' `
    "Instant Battle countdowns are asynchronous"
Assert-Contains $instantBattle 'MaximumPlayersPerInstance = 50' `
    "Instant Battle instance capacity is explicit"
Assert-Contains $instantBattle 'currentMap == null || index % MaximumPlayersPerInstance == 0' `
    "Instant Battle does not create an unused duplicate map"
Assert-Contains $instantBattle 'GameEventHandler.CompleteEvent(EventType.INSTANTBATTLE);' `
    "Instant Battle releases its lobby marker after partitioning players"
Assert-Contains $instantBattle 'Math.Min(' `
    "Instant Battle rewards respect the maximum gold limit"
Assert-Contains $instantBattle '[INSTANT_BATTLE]' `
    "Instant Battle exposes runtime diagnostics"
Assert-NotContains $instantBattle 'Thread.Sleep' `
    "Instant Battle does not block worker threads"
Assert-NotContains $instantBattle 'Observable.Start' `
    "Instant Battle instance tasks do not hide exceptions in fire-and-forget Rx work"
Assert-NotContains $instantBattle 'static readonly List<Tuple<MapInstance, byte>> Maps' `
    "Instant Battle does not retain stale map instances between runs"

Assert-Contains $instantBattleCatalog 'case 40:' `
    "Level 40-49 players have a valid monster wave catalog"
Assert-Contains $instantBattleCatalog 'List<MonsterToSummon> GetMonsters' `
    "Instant Battle waves use the stable List contract"
Assert-NotContains $instantBattleCatalog 'ConcurrentBag<MonsterToSummon>' `
    "Instant Battle waves cannot trigger the historical collection cast crash"

Assert-Contains $upet 'ApplyAreaAttraction(attacker, skill);' `
    "Fiesta de sushi invokes area attraction"
Assert-Contains $upet 'skill?.SkillVNum != 663' `
    "Area attraction is restricted to Fiesta de sushi"
Assert-Contains $upet 'monster.AddToAggroList(attacker.BattleEntity);' `
    "Nearby monsters receive pet threat"
Assert-Contains $upet 'monster.Target = attacker.BattleEntity;' `
    "Nearby monsters immediately target the pet"
Assert-Contains $upet '[MATE_TAUNT]' `
    "Fiesta de sushi attraction is observable"

Write-Host "Event runtime, Instant Battle and pet attraction contracts passed."
