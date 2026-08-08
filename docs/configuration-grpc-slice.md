# Configuration gRPC slice

## Purpose

This migration replaces the legacy `IConfigurationService` SCS surface with a typed, mTLS-authenticated gRPC boundary in controlled stages.

SCS is still the runtime authority. The typed contract, shadow state host, isolated World gRPC client transport, opt-in SCS-first shadow adapter, observation-only typed update subscriber and bounded cross-transport callback observation ledger now exist.

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

Equivalent SCS and gRPC payloads therefore produce the same fingerprint without storing a legacy object reference or applying a second gameplay effect. The default retention is 512 observations with a hard maximum of 4096; oldest evidence is evicted first and eviction is counted explicitly. This ledger captures comparable evidence only. It does not declare parity, change authority, replay callbacks or deduplicate gameplay effects.

### Date/time parity

The adapter only converts legacy values outward for comparison/seeding. It does not reconstruct the gameplay `ConfigurationObject` from gRPC.

- `Utc` and `Local` values use their explicit `DateTime` semantics.
- `Unspecified` values are deliberately interpreted with the local time-zone offset, matching the legacy wall-clock behavior based on `DateTime.Now`.
- comparisons happen on the resulting Unix-millisecond snapshot, so sub-millisecond `DateTime` ticks cannot create false drift after transport normalization.

## Callback boundary

`ConfigurationUpdated` remains the blocker for retiring this SCS family. This slice now provides the typed subscription, recovery and comparable evidence-capture foundations. Later qualification must still provide:

1. a bounded comparator that qualifies matching legacy and typed observations across live, replay, recovery, restart and reconnect windows;
2. a fail-closed joint Get/Update/callback authority switch and rollback path;
3. proof that a World cannot apply the same configuration update twice during overlap or cutover.

## Runtime sequence

Completed foundations:

1. typed Configuration request/reply contract and legacy migration map;
2. shadow .NET 10 Configuration state host;
3. isolated World-only gRPC client transport;
4. opt-in SCS-first World shadow adapter with bounded best-effort synchronization and idempotent generations;
5. observation-only typed update subscriber with bounded replay, reconnect deduplication and snapshot recovery;
6. bounded SCS-versus-gRPC callback observation ledger with normalized SHA-256 semantic fingerprints and explicit delivery phases.

The safe continuation is:

1. run explicit local acceptance with Master + World, shadow enabled, and verify SCS startup output plus typed host parity across reconnects;
2. compare retained legacy and typed snapshots/delivery across live, replay, recovery, restart and reconnect windows;
3. switch Get/Update and callback authority together behind one explicit Configuration selector;
4. remove `IConfigurationService`, `IConfigurationClient` and their SCS registration only after acceptance passes.

Until the joint authority switch, `NOSGM_COMMUNICATION_TRANSPORT` and the existing Communication callback cutover are unrelated to this service and must not act as implicit Configuration selectors.
