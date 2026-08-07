# Configuration gRPC slice

## Purpose

This migration replaces the legacy `IConfigurationService` SCS surface with a typed, mTLS-authenticated gRPC boundary in controlled stages.

SCS is still the runtime authority. The typed contract, shadow state host, isolated World gRPC client transport and an opt-in SCS-first shadow adapter now exist. No typed `ConfigurationUpdated` callback is active yet.

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

The payload is `ConfigurationSnapshot` with `MaxGold` and the two buff timestamps encoded as Unix milliseconds. Get and Update responses expose a monotonic `generation` for recovery and overlap deduplication.

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
- it has no SCS dependency and publishes no `ConfigurationUpdated` callback.

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

The adapter never mirrors from `OnConfigurationUpdated`. That callback remains legacy-only so multiple World receivers cannot reapply the same push through gRPC during this stage.

### Date/time parity

The adapter only converts legacy values outward for comparison/seeding. It does not reconstruct the gameplay `ConfigurationObject` from gRPC.

- `Utc` and `Local` values use their explicit `DateTime` semantics.
- `Unspecified` values are deliberately interpreted with the local time-zone offset, matching the legacy wall-clock behavior based on `DateTime.Now`.
- comparisons happen on the resulting Unix-millisecond snapshot, so sub-millisecond `DateTime` ticks cannot create false drift after transport normalization.

## Callback boundary

`ConfigurationUpdated` remains the blocker for retiring this SCS family. A later callback slice must provide:

1. a typed World subscription;
2. bounded replay or snapshot-plus-generation recovery;
3. shadow observation against the legacy callback;
4. a fail-closed authority switch and rollback path;
5. proof that a World cannot apply the same configuration update twice during overlap.

## Runtime sequence

Completed foundations:

1. typed Configuration request/reply contract and legacy migration map;
2. shadow .NET 10 Configuration state host;
3. isolated World-only gRPC client transport;
4. opt-in SCS-first World shadow adapter with bounded best-effort synchronization and idempotent generations.

The safe continuation is:

1. run explicit local acceptance with Master + World, shadow enabled, and verify SCS startup output plus typed host parity across reconnects;
2. add a typed `ConfigurationUpdated` subscriber with replay/recovery semantics;
3. mirror legacy callback delivery without applying a second gameplay effect;
4. compare legacy and typed snapshots/delivery across restart and reconnect windows;
5. switch Get/Update and callback authority together behind one explicit Configuration selector;
6. remove `IConfigurationService`, `IConfigurationClient` and their SCS registration only after acceptance passes.

Until the joint authority switch, `NOSGM_COMMUNICATION_TRANSPORT` and the existing Communication callback cutover are unrelated to this service and must not act as implicit Configuration selectors.
