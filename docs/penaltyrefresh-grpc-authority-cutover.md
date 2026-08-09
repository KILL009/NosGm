# PenaltyRefresh gRPC authority cutover

## Objective

`PenaltyRefresh` is the first remaining `ICommunicationClient` callback selected for removal from the SCS callback transport after the completed Configuration cutover.

The target architecture is:

- Master publishes one typed `PenaltyRefreshCallback` through `ClusterCommunicationCallbacks`;
- Login and World keep authenticated server-streaming subscriptions with distinct process identities and cursor files;
- typed gRPC becomes the only effect authority for `PenaltyRefresh` after the final cutover;
- the legacy SCS `UpdatePenaltyLog` callback is then suppressed and removed;
- all other `ICommunicationClient` callbacks remain unchanged until their own slices.

## Why this callback is first

`PenaltyRefresh` already has:

- a typed Protobuf payload;
- exact `ALL_NODES` routing;
- Master mTLS publication;
- Login and World subscriber roles;
- replay-complete barriers;
- semantic fingerprints shared by SCS and gRPC;
- bounded parity evidence;
- cross-transport overlap deduplication;
- operator-controlled activation and terminal rollback machinery.

This makes it a smaller and safer cut than attempting the full `ICommunicationService` surface at once.

## Slice 1: reproducible local shadow wiring

The normal Windows local stack can exercise the existing callback gRPC path without manual credential setup through an explicit wrapper:

```powershell
./scripts/start-communication-callback-shadow-local.ps1
```

The wrapper provides:

- Master callback publisher identity by reusing only the already role-scoped Master Configuration mTLS identity when no dedicated callback identity is supplied;
- Login callback subscriber identity by reusing only the Login process gRPC identity;
- World callback subscriber identity by reusing only the World process gRPC identity;
- separate absolute callback cursor files under the current user's local application data;
- the same preselected HTTP2/GRPCWEB wire mode already chosen before process startup;
- `NOSGM_COMMUNICATION_GRPC_CALLBACKS_ENABLED=true`;
- `NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_ENABLED=true`;
- `NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED=false`.

During this slice SCS remains the only effect authority. Typed callbacks are observed and compared only.

### Real-process PenaltyRefresh observation probe

With the shadow stack running, execute:

```powershell
./scripts/test-communication-callback-shadow-local.ps1
```

The acceptance probe does not create, update or delete a penalty. A dedicated .NET 10 probe publishes one typed `PenaltyRefreshCallback` directly through the Master mTLS publisher while callback APPLY remains disabled in Login and World. The payload uses a reserved positive observation-only ID and targets `ALL_NODES`.

The script requires the central runtime to report at least two matching subscribers, then waits for the durable callback cursor belonging to `login-local-1` and the cursor belonging to `world-local-1` to advance to at least the accepted sequence. Both cursors must commit against the same runtime generation. This proves real typed delivery to Login and World without executing `PenaltyLogRefresh` gameplay effects or touching `PenaltyLogDAO`.

Expected terminal line:

```text
Communication PenaltyRefresh real-process shadow acceptance passed.
```

Stop the stack normally afterward:

```powershell
./scripts/stop-modern-login-local.ps1
```

## Slice 2: PenaltyRefresh authority cutover

After the real-process shadow path is green:

1. require the callback subscriber and publisher when PenaltyRefresh gRPC authority is selected;
2. open typed ingress only after runtime generation and replay completion are valid;
3. stop Master from sending the legacy SCS `UpdatePenaltyLog` copy;
4. remove the SCS `UpdatePenaltyLog` receiver path;
5. fail closed if the typed callback path is unavailable rather than retrying the same effect over SCS;
6. update the legacy SCS manifest so `ICommunicationClient` no longer lists `UpdatePenaltyLog`.

## Explicit non-goals

This cutover does not migrate:

- `SendMessageToCharacter`;
- character presence callbacks;
- kick/session callbacks;
- lifecycle restart/shutdown callbacks;
- global events;
- bazaar, family, relation or static-bonus refresh callbacks;
- the 48-method `ICommunicationService` request surface.

Configuration remains gRPC-only and is not modified by this work.

## Merge gate

Do not merge the preparatory shadow slice until:

- Windows CI is green;
- .NET 10 CI is green;
- the local stack proves Master publication plus Login/World shadow subscription with role-separated mTLS identities;
- the observation-only `PenaltyRefresh` probe advances both durable subscriber cursors on one runtime generation;
- SCS remains callback effect authority and typed APPLY remains disabled.

The final authority slice additionally requires no SCS fallback for the migrated callback before merge.
