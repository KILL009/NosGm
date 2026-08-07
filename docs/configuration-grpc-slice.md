# Configuration gRPC slice

## Purpose

This slice defines the typed request/reply boundary that will replace the legacy `IConfigurationService` SCS surface.

The current runtime does not switch traffic in this slice. SCS remains authoritative until the typed state host, client adapter and `ConfigurationUpdated` subscriber have their own compatibility and rollback evidence.

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

The payload is `ConfigurationSnapshot` with `MaxGold` and the two buff timestamps encoded as Unix milliseconds.

The contract deliberately has no `Authenticate` RPC. The future runtime must authenticate World callers through the existing certificate identity model. `MasterAuthKey` must not be copied into a Protobuf request or logged as migration metadata.

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

## Callback boundary

This slice does not migrate `ConfigurationUpdated`. The callback remains an explicit blocker for SCS removal because the current behavior pushes configuration changes to every registered World.

A later callback slice must provide:

1. a typed World subscription;
2. bounded replay or an equivalent snapshot-plus-generation recovery rule;
3. shadow observation against the legacy callback;
4. a fail-closed authority switch and rollback path;
5. proof that a World cannot apply the same configuration update twice during overlap.

## Runtime sequence

The safe continuation is:

1. add a .NET 10 configuration state host using `ConfigurationSnapshot`;
2. add a typed World client adapter while keeping the SCS client available;
3. mirror legacy updates into a typed callback path without applying a second effect;
4. compare legacy and typed delivery across restart/reconnect windows;
5. switch Get/Update and callback authority together behind an explicit selector;
6. remove `IConfigurationService`, `IConfigurationClient` and their SCS registration only after acceptance passes.

Until step 5, `NOSGM_COMMUNICATION_TRANSPORT` and the existing communication callback cutover are unrelated to this service and must not be used as implicit Configuration selectors.
