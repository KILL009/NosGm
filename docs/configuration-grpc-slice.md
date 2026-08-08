# Configuration gRPC slice

## Purpose

This migration replaces the legacy `IConfigurationService` SCS surface with a typed, mTLS-authenticated gRPC boundary in controlled stages.

SCS is still the runtime authority. The typed contract, shadow state host, isolated World gRPC client transport, opt-in SCS-first shadow adapter, observation-only typed update subscriber, bounded cross-transport callback observation ledger, automatic parity comparator and production-neutral joint authority foundation now exist.

## Legacy surface

`IConfigurationService` contains three operations:

- `Authenticate(string authKey, Guid serverId)`
- `GetConfigurationObject()`
- `UpdateConfigurationObject(ConfigurationObject configurationObject)`

The legacy configuration payload contains only:

- `MaxGold`
- `TimeExpBuff`
- `TimeGoldBuff`

`IConfigurationClient.ConfigurationUpdated` is the corresponding World callback.

## Typed boundary

`ClusterConfiguration` exposes:

- `GetConfiguration`
- `UpdateConfiguration`
- `SubscribeConfigurationUpdates`

The payload is `ConfigurationSnapshot` with `MaxGold` and the two buff timestamps encoded as Unix milliseconds. Get and Update responses expose a monotonic `generation` plus a process-scoped `runtime_generation_id`. The stream resumes after a numeric generation only inside that runtime identity, preventing cursor reuse after a restart.

The contract deliberately has no `Authenticate` RPC. World callers authenticate through the existing certificate identity model. The legacy shared secret must not be copied into a Protobuf request or logged as migration metadata.

## Validation

`ClusterConfigurationContractValidator` fails closed when:

- the request or context is missing;
- the protocol context is invalid;
- the requested service is not `Configuration`;
- the caller role is not `World`;
- an update omits its snapshot;
- `MaxGold` is not positive;
- either timestamp cannot be represented by the legacy .NET `DateTime` range.

## Shadow state host

`NosGm.Authentication.Server` hosts `ClusterConfigurationService` and `ClusterConfigurationState` beside the existing authentication and communication services.

This state host is intentionally non-authoritative:

- it starts with no snapshot and generation `0`;
- `GetConfiguration` returns `Unavailable` until a typed snapshot has been supplied;
- it does not import `GameConfiguration` or invent a second default `MaxGold`;
- changed snapshots use last-write-wins and advance generation;
- an equivalent snapshot is idempotent and preserves generation, so multiple Worlds cannot inflate revisions merely by mirroring the same authoritative SCS state;
- input and returned Protobuf snapshots are cloned so callers cannot mutate stored state by reference;
- it has no SCS dependency and never invokes the legacy `ConfigurationUpdated` callback;
- it retains at most 256 changed snapshots and 32 pending updates per subscriber;
- equivalent snapshots do not publish duplicate envelopes;
- stale cursors fail closed and recover through the latest typed snapshot;
- reconnecting process identities replace their prior lease without creating a second authority.

The service reuses World-only mTLS certificate identity, wire-role/certificate-role equality, protocol validation, clock-skew and deadline bounds, request replay protection, and `AuthenticationDispatchGate` serialization.

## World gRPC client transport

`NosGm.Authentication.Client.Configuration.GrpcClusterConfigurationTransport` provides the typed World path.

It:

- accepts only `ClusterNodeRole.World` options;
- uses `ClusterService.Configuration` in every `RequestContext`;
- preserves loopback HTTPS, mTLS and bounded deadlines;
- supports native HTTP/2 and the Windows 10 gRPC-Web compatibility path;
- supports isolated trusted-root pinning;
- maps result codes, snapshots and generation without referencing SCS or the legacy Configuration interfaces;
- fails closed if the server claims `Success` without a snapshot.

Its construction self-test remains non-blocking. Network acceptance is performed only from an explicit runtime slice, never from a module initializer.

## SCS-first World shadow adapter

`ConfigurationServiceClient` can opt into a best-effort shadow mirror while keeping the legacy calls authoritative.

Enable it explicitly with:

- `NOSGM_CONFIGURATION_GRPC_SHADOW_ENABLED=true`
- optional `NOSGM_CONFIGURATION_GRPC_SHADOW_TIMEOUT_MS`, default `1500`, allowed range `100..10000`

The default is disabled.

When enabled:

1. `GetConfigurationObject` obtains the authoritative object from SCS first;
2. the mirror reads the typed host;
3. if the typed snapshot already matches, it performs no write;
4. if the host is unseeded or differs, it writes the authoritative SCS snapshot to the typed host;
5. the original SCS object is returned unchanged to World.

`UpdateConfigurationObject` follows the same authority order: SCS update first, shadow synchronization second. A timeout, transport failure, invalid shadow response or setup failure is logged without changing the SCS result or breaking World startup.

The adapter never mirrors from `OnConfigurationUpdated`. That callback remains the only path allowed to apply gameplay configuration during this stage.

## Observation-only update subscriber

When `NOSGM_CONFIGURATION_GRPC_SHADOW_ENABLED=true`, World also starts a best-effort typed subscriber. It first reads the current snapshot, binds its cursor to `runtime_generation_id`, then opens `SubscribeConfigurationUpdates` after the last observed generation.

The subscriber:

- accepts only the next live generation and discards overlap duplicates;
- detects gaps, stale cursors and Authentication runtime restarts;
- recovers from the latest snapshot before reopening the stream;
- uses bounded exponential reconnect delay;
- supports native HTTP/2 and the explicit Windows 10 gRPC-Web mode;
- logs recovery, replay and live observations without raising `ConfigurationUpdate` or assigning `ServerManager.Configuration`.

Therefore the typed path can be measured during overlap, but an unavailable or malformed stream cannot alter an SCS result, block World startup or apply a gameplay effect.

## Bounded callback parity ledger

The legacy `ConfigurationUpdated` callback and every accepted typed update now enter one process-local, transport-neutral observation ledger. Recording remains best effort: a malformed observation or ledger failure is logged and isolated before the authoritative SCS callback continues.

Each retained observation includes:

- one process generation identity and combined FIFO ordinal;
- an independent SCS or gRPC source ordinal;
- the source and typed delivery phase (`recovery`, `replay` or `live`);
- typed runtime generation and numeric generation when available;
- the normalized primitive snapshot values;
- a SHA-256 semantic fingerprint over those normalized values;
- the UTC observation timestamp.

Equivalent SCS and gRPC payloads therefore produce the same fingerprint without storing a legacy object reference or applying a second gameplay effect. The default retention is 512 observations with a hard maximum of 4096; oldest evidence is evicted first and eviction is counted explicitly. The ledger captures comparable evidence only; it does not change authority, replay callbacks or deduplicate gameplay effects.

## Automatic bounded parity comparator

Every successful SCS or gRPC ledger insertion now reevaluates an immutable parity snapshot. The default arrival-skew settlement window is 5 seconds and the implementation accepts only bounded values from 100 milliseconds through 60 seconds. A transient one-sided delivery is `InProgress`; it becomes `CountMismatch` only when reevaluated after the oldest unmatched observation has exceeded that window.

Parity is scoped to the latest typed `runtime_generation_id`. Recovery starts a new comparison window after an Authentication runtime restart. Recovery and replay observations remain visible as evidence but never count as live callback matches, and SCS observations from an earlier runtime window are excluded explicitly.

The report distinguishes:

- waiting for a typed runtime;
- a recovered runtime with no live observations yet;
- transient arrival skew;
- proven ordered semantic parity;
- ordered payload mismatch;
- persistent count mismatch;
- incomplete evidence after FIFO eviction;
- structurally invalid evidence.

Eviction, malformed ordering, runtime reuse, fingerprint drift and persistent callback skew all fail closed for future cutover qualification. Reports include the evaluated ledger boundary, runtime window, live and matched counts, recovery/replay counts, first mismatch coordinates and unmatched age. Both observation paths emit deduplicated diagnostics; terminal evidence failures are warnings. These reports are measurement only: SCS authority is unchanged, no typed update is applied to gameplay, and no callback is suppressed or replayed.

## Authority selector foundation (not wired yet)

The client library now contains a production-neutral authority coordinator for the future Configuration cutover. It is deliberately not referenced by `ConfigurationServiceClient` or the typed subscriber lifecycle, so SCS remains the production authority in this slice.

The coordinator can arm only from the latest three successful parity windows. Every window must belong to the same process, use a distinct typed runtime generation, contain matched non-empty live evidence, retain its complete FIFO window and report no mismatch or unmatched age. Activation then requires a fourth, previously unqualified runtime generation from that same process. Reusing one of the three qualification generations cannot activate the gate.

Activation alone does not open typed effects. Recovery must complete for the exact active runtime before one atomic decision selects typed Get, Update and callback. Before that barrier, all three operations remain together on SCS; the selector cannot create a split-authority state.

During the future callback overlap window, the first-arriving semantic copy may apply and one equal opposite-source copy is suppressed. The bounded FIFO guard uses the same normalized SHA-256 snapshot fingerprint as parity evidence, pairs repeated identical updates occurrence by occurrence, expires stale pairs and never stores gameplay objects. This covers both SCS-first and typed-first arrival without applying one logical update twice.

Runtime-generation drift, active-stream loss, a typed callback exception, malformed routing input or overlap-capacity saturation triggers terminal rollback for the process. Typed ingress then stays blocked, while a delayed SCS twin of an already-applied typed update can still be suppressed and new SCS updates continue normally. Production wiring and operator controls remain a later, separately reviewed step.

### Date/time parity

The adapter only converts legacy values outward for comparison/seeding. It does not reconstruct the gameplay `ConfigurationObject` from gRPC.

- `Utc` and `Local` values use their explicit `DateTime` semantics.
- `Unspecified` values are deliberately interpreted with the local time-zone offset, matching the legacy wall-clock behavior based on `DateTime.Now`.
- comparisons happen on the resulting Unix-millisecond snapshot, so sub-millisecond `DateTime` ticks cannot create false drift after transport normalization.

## Callback boundary

`ConfigurationUpdated` remains the blocker for retiring this SCS family. This slice now provides typed subscription, recovery, comparable evidence capture, automatic bounded parity verdicts and an isolated selector/duplicate guard. Later qualification must still provide:

1. explicit local acceptance proving stable parity across live, replay, recovery, restart and reconnect windows;
2. operator-controlled production wiring for the fail-closed joint Get/Update/callback selector;
3. live acceptance proving that the bounded overlap guard prevents duplicate gameplay application during cutover and rollback.

## Runtime sequence

Completed foundations:

1. typed Configuration request/reply contract and legacy migration map;
2. shadow .NET 10 Configuration state host;
3. isolated World-only gRPC client transport;
4. opt-in SCS-first World shadow adapter with bounded best-effort synchronization and idempotent generations;
5. observation-only typed update subscriber with bounded replay, reconnect deduplication and snapshot recovery;
6. bounded SCS-versus-gRPC callback observation ledger with normalized SHA-256 semantic fingerprints and explicit delivery phases;
7. automatic runtime-scoped parity comparator with bounded settlement, ordered fingerprint matching and fail-closed evidence verdicts;
8. production-neutral joint Get/Update/callback authority gate with three-window qualification, fourth-runtime activation, recovery barrier, bounded semantic overlap deduplication and terminal rollback.

The safe continuation is:

1. run explicit local acceptance with Master + World, shadow enabled, and collect stable comparator parity across live, replay, recovery, restart and reconnect windows;
2. bind the Configuration authority coordinator to the World process lifecycle behind immutable operator arm/rollback controls;
3. switch Get/Update and callback authority together behind that one explicit selector and run live overlap/rollback acceptance;
4. remove `IConfigurationService`, `IConfigurationClient` and their SCS registration only after acceptance passes.

Until the joint authority switch, `NOSGM_COMMUNICATION_TRANSPORT` and the existing Communication callback cutover are unrelated to this service and must not act as implicit Configuration selectors.
