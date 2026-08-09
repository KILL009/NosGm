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

The normal Windows local stack must be able to exercise the existing callback gRPC path without manual environment setup.

The integrated startup will provide:

- Master callback publisher identity using the dedicated Master certificate namespace;
- Login callback subscriber identity using the Login certificate;
- World callback subscriber identity using the World certificate;
- separate absolute callback cursor files for Login and World;
- the same preselected HTTP2/GRPCWEB wire mode already chosen before process startup;
- shadow subscriber and Master publication mirror flags only when explicitly requested.

During this slice SCS remains the only effect authority. Typed callbacks are observed and compared only.

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

Do not merge the final authority slice until:

- Windows CI is green;
- .NET 10 CI is green;
- the local stack proves Master publication plus Login/World shadow subscription with role-separated mTLS identities;
- `PenaltyRefresh` typed delivery is observed in both Login and World;
- the final authority state has no SCS fallback for the migrated callback.
