# NosGM Next

NosGM Next is the incremental modernization path for the existing NosGM emulator. It keeps the current gameplay coverage and client compatibility while replacing legacy OpenNos infrastructure behind stable boundaries.

This is not a full rewrite and it is not a migration to Vanosilla. Vanosilla and WingsEmu are architectural references only. Features must be reimplemented or adapted with explicit license and attribution review.

## Principles

1. Preserve packet and gameplay behavior before replacing infrastructure.
2. Make small pull requests with reproducible validation.
3. Measure production paths before and after each optimization.
4. Prefer bounded queues and centralized schedulers over per-entity polling.
5. Keep public web, launcher and game processes isolated.
6. Do not copy third-party source without license and provenance review.
7. Target .NET 10 LTS for the final modern runtime.

## Phase 0: safety baseline

Status: in progress.

Required work:

- [x] Remove null-account access from Login.
- [x] Parse and enforce the configured client version.
- [x] Validate legacy and current client-version fields safely.
- [x] Make stale-session retries actually wait.
- [x] Retrieve and send the world list once per login.
- [x] Stop logging the full world-list packet.
- [x] Normalize IPv4 and IPv6 endpoints before IP checks and registration.
- [x] Preserve password case for non-legacy authentication.
- [x] Reject oversized raw network messages before integer narrowing or unbounded buffering.
- [x] Dispose replaced wire-protocol buffers and validate unsupported outbound message types.
- [ ] Add a versioned password-hash migration using a per-account salt and a supported adaptive KDF.
- [x] Correct packet sequence validation and add diagnostic context.
- [x] Remove duplicate monster initialization.
- [x] Replace swallowed map and drop exceptions with bounded diagnostics.
- [x] Verify that World Release runs as a 64-bit process.
- [ ] Own and dispose the `MapInstance` life subscription deterministically.

## Phase 1: regression harness

Build a deterministic compatibility harness before major refactors.

The harness must cover:

- Login success and each failure mode.
- World and channel list generation for every supported region.
- Character selection and clean disconnection.
- Movement, map change and portal use.
- Single-target and area combat.
- Buff apply, refresh and removal.
- Drops, inventory and equipment persistence.
- Bazaar listing and purchase.
- Family, raid and Time-Space entry.
- Reconnection after interrupted sessions.

Packet fixtures must remove credentials, session keys and personal data.

## Phase 2: scheduler foundation

Introduce stable contracts without changing gameplay behavior:

```csharp
public interface IGameScheduler
{
    IDisposable Schedule(TimeSpan delay, Action action);
    IDisposable ScheduleRecurring(TimeSpan interval, Action action);
}

public interface IMapSystem
{
    string Name { get; }
    void ProcessTick(GameTickContext context);
    void EnterIdle();
    void WakeUp();
}
```

Initial targets:

- Remaining per-character and per-map `Observable.Interval` loops.
- Delayed packets and summon timers.
- Buff expiration and recurrent world events.
- Tick drift, backlog and slow-system telemetry.

## Phase 3: map decomposition

Split the current `MapInstance` responsibilities behind interfaces:

- Character system.
- Monster and AI system.
- NPC system.
- Mate system.
- Battle request system.
- Drop system.
- Portal system.
- Scripted-event system.

The first step is extraction, not behavior changes. Each extracted system must continue to produce the same packets and events as the current implementation.

Empty maps should sleep unless they contain players, active events, delayed work, temporary entities or scripted instances.

## Phase 4: packet pipeline

Continue the event-driven ingress work with:

- Strict packet-id validation with wrap-around handling.
- Header and payload-size limits.
- Per-header latency and exception metrics.
- Bounded cross-server authentication buffering.
- Sanitized packet trace capture and replay.
- Clear policies for malformed, duplicated and out-of-order packets.

No credential, session key or private chat payload may be written to production logs.

## Phase 5: modular services

Keep NosGM as a modular monolith until measurements justify additional processes.

Remain separate:

- Public web portal.
- Launcher and release tooling.
- Public snapshot publisher.
- Log storage where operationally useful.

Candidates for later extraction:

- Bazaar.
- Mail.
- Rankings.

Do not split Family, quests or combat merely to imitate another emulator. A service boundary must reduce contention, deployment risk or ownership complexity.

## Phase 6: .NET 10 migration

Migrate from the bottom upward:

1. Domain and contracts.
2. Packets and serialization.
3. DTOs and algorithms.
4. Configuration abstractions.
5. Telemetry and schedulers.
6. Data access.
7. Login and Master communication.
8. World.

Use multi-targeting where practical. Remove Windows Forms dependencies from server libraries. Replace unsupported communication and configuration components only after regression coverage exists.

## Performance gates

Each major phase must report:

- CPU and working set.
- Allocation rate and full-GC count.
- Login p50, p95 and p99.
- Packet queue depth and wait time.
- Tick duration and drift.
- SQL query count and duration.
- Connected-session cleanup after disconnect storms.
- A soak test long enough to reveal retained sessions, timers and maps.

A refactor is not accepted as a performance improvement without comparable measurements.

## Definition of done

NosGM Next is complete when:

- The supported client can log in and play through the regression suite.
- World no longer relies on per-player polling for core lifecycle work.
- Maps are processed through bounded, measurable systems.
- Authentication uses a versioned adaptive password hash.
- Production processes run on .NET 10 LTS.
- Server libraries are independent of Windows desktop APIs.
- Critical paths have automated regression and load tests.
- Deployment, backup, rollback and incident procedures are documented.
