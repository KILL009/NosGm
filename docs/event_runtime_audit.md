# NosGM event runtime audit

## Scope

This audit covers the global `EventType` catalog, the local `GameEventHandler`, recurring world schedulers, Instant Battle and the shared event payload boundary.

## Critical defects repaired

### Instant Battle first-wave crash

The legacy Instant Battle generated waves as `ConcurrentBag<MonsterToSummon>`, but `EventHelper` cast every `SPAWNMONSTERS` payload directly to `List<MonsterToSummon>`. The first scheduled wave could therefore throw `InvalidCastException` on an Rx scheduler thread.

The repair normalizes every `IEnumerable<MonsterToSummon>` to `List<MonsterToSummon>` in `EventContainer`. The new Instant Battle runtime and catalog use `List<MonsterToSummon>` directly.

### Unsafe event dispatch

The previous outer `try/catch` surrounded `Task.Run`, not the work executed inside it. Exceptions thrown by event generators were not caught by that block. Event start tracking also used an unsynchronized `List<EventType>`.

The dispatcher now:

- synchronizes event start and completion state;
- catches failures inside the background task;
- removes a failed event from `StartedEvents`;
- records event name, channel and parameters;
- reports unwired event types instead of silently discarding them.

### Recurring scheduler failures

The world event service had recurring Rx callbacks without an error boundary. Save, group, Act 4, item cleanup and monster loops now run through `EventRuntimeGuard`, which logs an individual callback failure without turning it into a silent dead stream.

### Instant Battle lifecycle

The obsolete implementation was removed. The replacement:

- uses asynchronous countdowns instead of `Thread.Sleep`;
- creates exactly one map per group of up to 50 players;
- keeps instance state local so disposed maps are not replayed in later events;
- validates generated map instances;
- catches and identifies each delayed action failure;
- removes the ineffective unawaited `Task.Delay`;
- removes the duplicated 30-second warning;
- restores waves for the level 40-49 bracket;
- caps gold rewards at the configured maximum;
- logs lobby, wave, completion and failure states.

## Local dispatch coverage

### Connected to `GameEventHandler`

- `INSTANTBATTLE`
- `LOD`
- `MINILANDREFRESHEVENT`
- `RANKINGREFRESH`
- `GLACERNONSHIP`
- `TALENTARENA`
- `CALIGOR`
- `ICEBREAKER`
- `AUTOREBOOT`
- `RAINBOWBATTLE`
- `DAILYMISSIONEXTENSIONREFRESH`
- `ASGOBAS`
- `WORLDBOSS`

### Declared in the cluster/event contract but not locally dispatched here

- `GLACERNONRAID`
- `METEORITEGAME`
- `Act7Ship`
- `CELESTIALSPIRE`
- `DROPRATE`
- `FAIRYRATE`
- `HERORATE`
- `XPRATE`
- `RESETRATE`
- `BattleRoyal`
- `DUELEVENT`
- `DUELEVENTPRIVATE`
- `OpenWorldBoss`

Some of these values already have partial handler classes or are configuration/rate operations, but they do not currently have an explicit local dispatch contract in `GameEventHandler`. They now generate `UnsupportedLocalDispatch` diagnostics instead of appearing to start successfully.

They must be connected one at a time with a channel rule, required parameters, completion rule and focused runtime test. Guessing those arguments in the dispatcher would be more dangerous than rejecting the event clearly.

## Pathfinder relationship

The pathfinder migration exposed event fragility because scripted movement and newly spawned event monsters now enter the new AI/pathfinding stack. The confirmed Instant Battle crash, however, had a deterministic collection-contract failure before pathfinding was required.

Pathfinding remains a possible source of later failures for scripted `MOVE` actions or invalid event map grids. The new diagnostics will identify those failures by event and operation instead of allowing the World process to disappear without a useful cause.

## Runtime acceptance test

### Instant Battle

1. Start `INSTANTBATTLE` on a non-Act 4 channel.
2. Join with a character in each available level bracket, especially 40-49.
3. Confirm map creation and first wave spawn after ten seconds.
4. Keep World logs and verify:

```text
[EVENT_RUNTIME] Event=INSTANTBATTLE Result=Starting
[INSTANT_BATTLE] Result=LobbyClosed ...
[INSTANT_BATTLE] Result=InstanceStarted ...
[INSTANT_BATTLE] Result=WaveSpawned ... Wave=0 ...
```

5. Confirm the World process remains alive through all five waves.
6. Kill the final wave and verify rewards, portal and `Result=Succeeded`.

### Fiesta de sushi

Use skill 663 with several attackable monsters inside six cells. The log must contain:

```text
[MATE_TAUNT] Mate=... Skill=663 Range=6 Attracted=N
```

`N` must be greater than zero, and the affected monsters must switch their target to the pet.

## Dignity message

The repeated message `You have restored some of your dignity!` is independent from pet experience. `Character.GenerateDignity` currently restores 0.5 dignity when a character above level 20 kills a higher-level monster while dignity is below 100. This can display the message every two qualifying kills. It is a separate legacy rule and was not introduced by the pet experience repair.
