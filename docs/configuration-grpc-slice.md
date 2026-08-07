# Configuration gRPC slice

## Purpose

This migration replaces the legacy `IConfigurationService` SCS surface with a typed, mTLS-authenticated gRPC boundary in controlled stages.

SCS is still the runtime authority. The typed contract and a shadow state host now exist, but no World request is routed to the new host and no typed `ConfigurationUpdated` callback is active yet.

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

The payload is `ConfigurationSnapshot` with `MaxGold` and the two buff timestamps encoded as Unix milliseconds. Get and Update responses also expose a monotonic `generation` that can later anchor callback recovery and overlap deduplication.

The contract deliberately has no `Authenticate` RPC. World callers must authenticate through the existing certificate identity model. The legacy shared secret must not be copied into a Protobuf request or logged as migration metadata.

## Validation

`ClusterConfigurationContractValidator` fails closed when:

- the request or context is missing;
- the protocol context is invalid;
- the requested service is not `Configuration`;
- the caller role is not `World`;
- an update omits its snapshot;
- `MaxGold` is not positive;
- either timestamp cannot be represented by the legacy .NET `DateTime` range.

The runtime self-test exercises valid Get/Update requests plus wrong-role, wrong-service, missing-payload and boundary failures.

## Shadow state host

`NosGm.Authentication.Server` now hosts `ClusterConfigurationService` and `ClusterConfigurationState` beside the existing authentication and communication services.

This state host is intentionally non-authoritative:

- it starts with no snapshot and generation `0`;
- `GetConfiguration` returns `Unavailable` until a typed snapshot has been supplied;
- it does not import `GameConfiguration` or invent a second default `MaxGold`;
- `UpdateConfiguration` uses the same last-write-wins shape as the legacy service and advances the generation once per accepted update;
- input and returned Protobuf snapshots are cloned so callers cannot mutate stored state by reference;
- it has no SCS dependency and publishes no `ConfigurationUpdated` callback;
- no World client currently calls it, so registering the endpoint does not move runtime authority.

The service reuses the existing cluster-runtime protections: World-only mTLS certificate identity, wire-role/certificate-role equality, protocol validation, clock-skew and deadline bounds, request replay protection, and `AuthenticationDispatchGate` serialization during runtime cutover-sensitive work.

## Callback boundary

`ConfigurationUpdated` remains an explicit blocker for SCS removal because the current legacy behavior pushes configuration changes to every registered World.

A later callback slice must provide:

1. a typed World subscription;
2. bounded replay or snapshot-plus-generation recovery;
3. shadow observation against the legacy callback;
4. a fail-closed authority switch and rollback path;
5. proof that a World cannot apply the same configuration update twice during overlap.

## Runtime sequence

Completed foundations:

1. typed Configuration request/reply contract and legacy migration map;
2. shadow .NET 10 Configuration state host with monotonic generation and runtime guards.

The safe continuation is:

1. add a World shadow adapter that keeps SCS authoritative while seeding and comparing the typed host;
2. add a typed `ConfigurationUpdated` subscriber with replay/recovery semantics;
3. mirror legacy callback delivery without applying a second gameplay effect;
4. compare legacy and typed snapshots/delivery across restart and reconnect windows;
5. switch Get/Update and callback authority together behind one explicit Configuration selector;
6. remove `IConfigurationService`, `IConfigurationClient` and their SCS registration only after acceptance passes.

Until the joint authority switch, `NOSGM_COMMUNICATION_TRANSPORT` and the existing Communication callback cutover are unrelated to this service and must not act as implicit Configuration selectors.
