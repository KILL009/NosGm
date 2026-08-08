# Configuration gRPC slice

## Purpose

This migration replaces the legacy `IConfigurationService` SCS surface with a typed, mTLS-authenticated gRPC boundary in controlled stages.

SCS is the fail-closed default. The typed contract, state host, isolated World gRPC client transport, opt-in shadow adapter, bounded cross-transport callback observation ledger, automatic parity comparator and one production joint authority selector now exist. Typed effects remain disabled unless an operator explicitly authorizes them at process start and the runtime qualification barriers pass.

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

## Typed state host

`NosGm.Authentication.Server` hosts `ClusterConfigurationService` and `ClusterConfigurationState` beside the existing authentication and communication services.

This state host starts non-authoritative and becomes selectable only through the joint authority barriers:

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

## Default SCS path and World shadow adapter

`ConfigurationServiceClient` can opt into a best-effort shadow mirror while keeping the legacy calls authoritative.

Enable it explicitly with:

- `NOSGM_CONFIGURATION_GRPC_SHADOW_ENABLED=true`
- optional `NOSGM_CONFIGURATION_GRPC_SHADOW_TIMEOUT_MS`, default `1500`, allowed range `100..10000`

The default is disabled.

While SCS is selected:

1. `GetConfigurationObject` obtains the authoritative object from SCS first;
2. the mirror reads the typed host;
3. if the typed snapshot already matches, it performs no write;
4. if the host is unseeded or differs, it writes the authoritative SCS snapshot to the typed host;
5. the original SCS object is returned unchanged to World.

`UpdateConfigurationObject` follows the same default order: SCS update first, shadow synchronization second. A timeout, transport failure, invalid shadow response or setup failure is logged without changing the SCS result or breaking World startup.

The adapter never mirrors from `OnConfigurationUpdated`. When typed authority is selected, Get and Update use the bounded gRPC client directly; any typed failure rolls the selector back before retrying the operation through SCS.

## Typed update subscriber

When `NOSGM_CONFIGURATION_GRPC_SHADOW_ENABLED=true`, World also starts a best-effort typed subscriber. It first reads the current snapshot, binds its cursor to `runtime_generation_id`, then opens `SubscribeConfigurationUpdates` after the last observed generation.

The subscriber:

- accepts only the next live generation and discards overlap duplicates;
- detects gaps, stale cursors and Authentication runtime restarts;
- recovers from the latest snapshot before reopening the stream;
- uses bounded exponential reconnect delay;
- supports native HTTP/2 and the explicit Windows 10 gRPC-Web mode;
- logs recovery, replay and live observations;
- raises `ConfigurationUpdate` only when the joint authority selector has atomically selected typed Get, Update and callback.

Therefore the typed path can be measured safely while unarmed. When live effects are authorized, an unavailable or malformed stream triggers terminal rollback before SCS resumes as authority for all three operations.

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

Equivalent SCS and gRPC payloads therefore produce the same fingerprint without storing a legacy object reference. The default retention is 512 observations with a hard maximum of 4096; oldest evidence is evicted first and eviction is counted explicitly. This ledger captures comparable evidence only; the separate bounded overlap ledger suppresses delayed opposite-source callback twins during cutover and rollback.

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

Eviction, malformed ordering, runtime reuse, fingerprint drift and persistent callback skew all fail closed for cutover qualification. Reports include the evaluated ledger boundary, runtime window, live and matched counts, recovery/replay counts, first mismatch coordinates and unmatched age. Both observation paths emit deduplicated diagnostics; terminal evidence failures are warnings. Reports never select authority directly: they must pass the immutable operator controls, activation and recovery barriers described below.

## Joint authority routing

The World process binds one Configuration authority coordinator to the real Get, Update and callback paths. Successful SCS and typed gRPC observations feed the same bounded qualification runtime; typed recovery, runtime generation changes, stream termination and terminal subscriber faults are also reported to the coordinator. SCS is the fail-closed default before authorization, throughout qualification and whenever rollback occurs.

Three immutable process-start controls exist:

- `NOSGM_CONFIGURATION_GRPC_AUTHORITY_ARM_REQUEST_ID` accepts one exact lowercase canonical non-empty GUID and allows three distinct successful parity runtimes to arm the gate.
- `NOSGM_CONFIGURATION_GRPC_AUTHORITY_EFFECTS_ENABLED=true` separately authorizes live effect routing. It is rejected unless an arm request is present, so an arm value previously used for dry-run evidence cannot begin applying typed gameplay effects after an upgrade.
- `NOSGM_CONFIGURATION_GRPC_AUTHORITY_ROLLBACK_REQUESTED=true` blocks qualification explicitly. It is mutually exclusive with the arm request and live-effects authorization.

Missing controls leave the runtime unarmed. Whitespace, malformed GUIDs, non-boolean rollback values, conflicting controls, process-generation drift or any attempt to mutate the controls inside one process fail closed. The process must be restarted to change the requested mode.

Live effect authorization also requires `NOSGM_CONFIGURATION_GRPC_SHADOW_ENABLED=true` so both the bounded request/reply client and the recovering typed subscriber exist. If either dependency cannot start, the process blocks typed authority and continues on SCS.

An arm request without the effects flag remains a dry run: the state machine may retain qualification evidence and observe a fourth runtime activation, but typed ingress never opens. With both explicit controls, recovery for the active runtime opens one atomic selector. `GetConfigurationObject`, `UpdateConfigurationObject` and both callback sources then consult the same decision.

The coordinator can arm only from the latest three successful parity windows. Every window must belong to the same process, use a distinct typed runtime generation, contain matched non-empty live evidence, retain its complete FIFO window and report no mismatch or unmatched age. Activation then requires a fourth, previously unqualified runtime generation from that same process. Reusing one of the three qualification generations cannot activate the gate.

Activation alone does not open typed effects. Recovery must complete for the exact active runtime before one atomic decision selects typed Get, Update and callback. Before that barrier, all three operations remain together on SCS; the selector cannot create a split-authority state.

Once typed authority is ready, an early SCS callback is rejected even if its typed counterpart has not arrived yet. A selected typed callback is applied and one delayed equal SCS twin is suppressed. Before typed recovery and after rollback, SCS remains selected. The bounded FIFO guard uses the same normalized SHA-256 snapshot fingerprint as parity evidence, pairs repeated identical updates occurrence by occurrence, expires stale pairs and never stores gameplay objects.

Every successful typed Get or Update response must carry the exact active runtime generation and a positive generation. After a typed Update succeeds, the client synchronizes the same object to SCS as a rollback standby; the resulting SCS callback is rejected or paired as the opposite-source twin while typed authority remains selected. A standby synchronization failure closes typed authority and remains visible to the caller instead of pretending that failback is safe.

The remaining World-side SCS dependency is now confined to `ScsConfigurationRollbackTransport`, which implements the narrow internal `IConfigurationRollbackTransport` boundary. `ConfigurationServiceClient` no longer implements `IConfigurationService`, constructs an SCS client, accesses a service proxy or owns the legacy callback client. The callback adapter receives an explicit delegate instead of reaching back through the global singleton. This is intentionally an isolation step, not removal: SCS still supplies the default and terminal rollback paths until live acceptance is complete.

A timeout, unavailable transport, malformed response, runtime-generation drift, active-stream loss, typed callback exception, malformed routing input or overlap-capacity saturation triggers terminal rollback for the process. Typed ingress then stays blocked, while a delayed SCS twin of an already-applied typed update can still be suppressed and new SCS operations continue normally.

### Date/time parity

The adapter converts legacy values outward for comparison/seeding and reconstructs a gameplay `ConfigurationObject` only for a selected typed Get or callback.

- `Utc` and `Local` values use their explicit `DateTime` semantics.
- `Unspecified` values are deliberately interpreted with the local time-zone offset, matching the legacy wall-clock behavior based on `DateTime.Now`.
- comparisons happen on the resulting Unix-millisecond snapshot, so sub-millisecond `DateTime` ticks cannot create false drift after transport normalization.

## Callback boundary

`ConfigurationUpdated` is now routed through the same selector as Get and Update, but its legacy SCS interface remains installed as the rollback path. Retirement still requires:

1. explicit local acceptance proving stable parity across live, replay, recovery, restart and reconnect windows;
2. dry-run collection across three parity runtimes and a fourth activation runtime using an explicit arm request;
3. live acceptance with `NOSGM_CONFIGURATION_GRPC_AUTHORITY_EFFECTS_ENABLED=true` proving that the bounded overlap guard prevents duplicate gameplay application during cutover and rollback;
4. removal acceptance after the SCS interfaces and Master registration are deleted.

## Runtime sequence

Completed foundations:

1. typed Configuration request/reply contract and legacy migration map;
2. shadow .NET 10 Configuration state host;
3. isolated World-only gRPC client transport;
4. opt-in SCS-first World shadow adapter with bounded best-effort synchronization and idempotent generations;
5. typed update subscriber with bounded replay, reconnect deduplication, snapshot recovery and selector-controlled application;
6. bounded SCS-versus-gRPC callback observation ledger with normalized SHA-256 semantic fingerprints and explicit delivery phases;
7. automatic runtime-scoped parity comparator with bounded settlement, ordered fingerprint matching and fail-closed evidence verdicts;
8. production-neutral joint Get/Update/callback authority gate with three-window qualification, fourth-runtime activation, recovery barrier, bounded semantic overlap deduplication and terminal rollback;
9. immutable World operator controls and dry-run lifecycle binding that observes evidence, recovery, generations and stream faults;
10. production joint authority routing for Get, Update and callback with a separate live-effects authorization, exact runtime identity checks and terminal SCS rollback.
11. one isolated World-side SCS rollback adapter, leaving the gameplay-facing Configuration facade transport-neutral and making the final deletion boundary explicit.

The safe continuation is:

1. run explicit local acceptance with Master + World, shadow enabled, and collect stable comparator parity across live, replay, recovery, restart and reconnect windows;
2. collect dry-run qualification evidence with an explicit arm request across three parity runtimes and a fourth activation runtime;
3. run live overlap/rollback acceptance with both the arm request and effects authorization;
4. remove `ScsConfigurationRollbackTransport`, `IConfigurationService`, `IConfigurationClient` and their Master registration only after acceptance passes; `ConfigurationServiceClient` must remain unchanged by that deletion.

`NOSGM_COMMUNICATION_TRANSPORT` and the existing Communication callback cutover are unrelated to this service and must not act as implicit Configuration selectors.
