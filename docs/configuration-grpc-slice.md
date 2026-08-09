# Configuration gRPC authority

## Final architecture

Configuration has completed its transport cutover. `ClusterConfiguration` is the only Configuration authority. There is no Configuration SCS request/reply service, callback service, rollback adapter, shadow mirror, parity selector, or transport fallback remaining in the runtime path.

The gameplay-facing `ConfigurationServiceClient` façade is intentionally retained so existing World handlers do not need a broad API rewrite. Internally it uses `GrpcClusterConfigurationTransport` for `GetConfiguration` and `UpdateConfiguration`, plus `ConfigurationGrpcSubscriber` for the authoritative update stream. Any transport or contract failure is fail-closed.

## Roles

The gRPC service uses mTLS certificate identity and wire-role equality.

| Operation | Allowed role | Purpose |
| --- | --- | --- |
| `GetConfiguration` | World, Master | World reads gameplay state; Master checks whether a seed already exists. |
| `UpdateConfiguration` | World, Master | World publishes gameplay changes; Master seeds only when the runtime is empty. |
| `SubscribeConfigurationUpdates` | World | World-only authoritative update stream. |
| `GetConfigurationRuntimeInfo` | Master | Operational status. |
| `RestartConfigurationRuntime` | Master | Guarded Configuration-only runtime restart. |

There is no Configuration `Authenticate` RPC and no Configuration application-level shared secret. World and Master identities are established by their own client certificates.

## Cold boot and Master restart

`NosGm.Master.Server` calls `EnsureConfigurationGrpcAuthority` before it starts its legacy Master SCS listener. The dedicated `ConfigurationMasterSeedClient` uses the Master certificate and follows this sequence:

1. `GetConfiguration` checks the authoritative state.
2. If a valid snapshot already exists, Master preserves it unchanged.
3. If the Configuration runtime reports `Unavailable`, Master publishes the initial `MSManager.ConfigurationObject` snapshot with `UpdateConfiguration`.
4. Any other failure aborts Master startup. There is no Configuration SCS fallback.

This makes cold boot deterministic while preventing a Master restart from overwriting live World Configuration values.

## World data path

World uses its own mTLS identity. Startup keeps the existing `ConfigurationServiceClient.Authenticate(authKey, serverId)` call only as a compatibility façade; the key is not sent or inspected by the Configuration transport. The method proves readiness with an authoritative gRPC Get.

`ConfigurationGrpcSubscriber` recovers exclusively from the gRPC snapshot, binds its cursor to the returned `runtime_generation_id`, replays retained generations, consumes live updates, and reconnects with bounded backoff. A gap or runtime replacement triggers gRPC snapshot recovery. No SCS callback participates.

The public `ConfigurationUpdate` event still carries `ConfigurationObject` as the sender so existing `ServerManager` consumption remains compatible.

## Gameplay mutation ordering

Family EXP and gold buffs publish the new Configuration snapshot first. Gameplay effects are applied only after the gRPC update succeeds. If publication fails, the handler returns without applying the buff and without consuming the family mission. This prevents local gameplay state from outrunning the cluster authority.

## Runtime state

`NosGm.Authentication.Server` owns `ConfigurationRuntimeController` and `ClusterConfigurationState`. The state:

- starts unseeded at generation zero;
- returns `Unavailable` until Master supplies the first snapshot;
- advances generation only for changed snapshots;
- treats equivalent snapshots as idempotent;
- retains a bounded replay window;
- gives every runtime epoch a canonical `runtime_generation_id`;
- terminates stale subscribers when a guarded Configuration-only restart replaces the epoch.

Runtime control remains disabled by default. When enabled, its status/restart RPCs are Master-only and use exact runtime-generation compare-and-swap.

## Local Windows startup

`scripts/start-modern-login-core-local.ps1` always starts the .NET Authentication gRPC host because it now also hosts mandatory Configuration authority. This is true even when `-AuthenticationTransport SCS` is selected for the wider Login/Gameforge authentication path.

The startup script requires a certificate bundle containing `AuthBridge`, `Login`, `World`, and `Master`. It gives World only the World identity needed by Configuration, and gives Master a separate Configuration-specific Master identity through the `NOSGM_CONFIGURATION_GRPC_CONTROL_*` variables. Secrets are scoped to each child process and are not written to the process state JSON.

Canonical order is:

1. Authentication/Configuration gRPC host
2. Master, which confirms or seeds Configuration
3. World, which reads and subscribes
4. Login
5. Launcher

## Removed Configuration SCS surface

The final cut deletes these Configuration-only legacy pieces:

- `IConfigurationService`
- `IConfigurationClient`
- `ConfigurationService`
- `ConfigurationClient`
- `IConfigurationRollbackTransport`
- `ScsConfigurationRollbackTransport`
- the `WorldServer.ConfigurationServiceClient` SCS registration slot
- the Configuration shadow mirror/lifecycle
- the Configuration joint-authority selector, parity ledgers, qualification gates and acceptance-pulse machinery

Other NosGM services may still use SCS during their own independent migrations. This document and the final verifier intentionally scope the prohibition to Configuration.

## Verification contract

The canonical static guard is `scripts/verify-configuration-grpc-authority-final.ps1`. It must prove that the deleted SCS surface cannot re-enter the Configuration path, that role permissions remain exact, that Master seeding occurs before its legacy listener, that World is the sole subscriber role, and that family gameplay effects are ordered after authoritative publication.

After static verification, the repository still requires the normal Windows 11 build and runtime checks. Previously accepted LiveEffects migration evidence is historical cutover evidence and is not part of the final SCS-removal command sequence.
