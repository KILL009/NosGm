# Operator PenaltyRefresh callback cutover handshake

## Purpose

NosGM retains terminal `PenaltyRefresh` parity evidence from the legacy SCS
callback path and the typed gRPC callback stream. Qualification alone never
moves production authority.

This slice connects the operator-controlled handshake to the real
`PenaltyRefresh` effect path. It permits typed production effects only after
explicit activation, three clean parity generations, a fourth distinct runtime
generation and replay completion.

Master still publishes the temporary SCS and typed copies independently. The
receiver therefore keeps a bounded overlap ledger. The first-arrival rule is
simple: whichever copy arrives first applies the logical effect. The matching
copy from the other transport is suppressed. This closes the cross-transport
race without assuming that the asynchronous mirror and SCS sockets arrive in
the same order.

Every other callback kind remains on SCS. `SendMessageToCharacter` remains
excluded from the typed callback protocol and from this cutover.

## Required operator flags

The callback subscriber remains disabled by default. A production cutover
process requires all of the following:

- `NOSGM_COMMUNICATION_GRPC_CALLBACKS_ENABLED=true`
- `NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED=true`
- `NOSGM_COMMUNICATION_GRPC_CALLBACKS_PENALTY_REFRESH_ARM_REQUEST_ID=<guid>`
- `NOSGM_COMMUNICATION_GRPC_CALLBACKS_PENALTY_REFRESH_ROLLBACK_REQUESTED=false`

The arm request ID must be an exact lowercase canonical non-empty GUID. It is
retained in process-local status so the operator can correlate activation with
a deployment or change record.

Rollback accepts only `true` or `false` and defaults to `false`. Arm and
rollback cannot be requested together.

The first accepted operator and effect-routing configuration is immutable for
the lifetime of the Login or World process. Changing the request ID, rollback
value or apply authorization requires a process restart. A detected mutation
permanently blocks cutover and restores SCS authority.

## Arming rules

`CommunicationCallbackOperatorCutoverCoordinator` can arm only when all of the
following are true:

1. a valid operator request ID is present;
2. the qualification ledger has a complete, non-evicted history;
3. the latest three terminal windows report non-empty `PenaltyRefresh` parity;
4. all three windows belong to one process identity;
5. all three windows use distinct runtime generations;
6. the process has not been blocked or rolled back.

Without the operator request, the same evidence remains diagnostic and SCS
continues applying `PenaltyRefresh`.

## New-generation and replay handshake

Arming does not activate typed production effects immediately. The three
qualification generations are remembered by the cutover gate.

When the typed callback subscriber begins another stream, the coordinator
observes its Authentication runtime generation. Reusing any of the three
qualification generations cannot activate the gate. The first acceptable
activation therefore occurs on a fourth distinct runtime generation.

The new generation initially remains behind a replay barrier:

- replayed typed envelopes are observed but do not execute effects;
- SCS continues applying effects throughout replay;
- replayed typed twins consume matching SCS overlap entries;
- only live envelopes after replay completion may apply through typed gRPC.

`CompleteReplay` opens `TypedIngressReady` while holding the same process-local
lock used by both effect paths.

## Overlap-safe effect selection

`CommunicationClient.UpdatePenaltyLog` and
`CommunicationCallbackEnvelopeDispatcher` send the same SHA-256 semantic
fingerprint to the coordinator.

For the selected callback kind, `TryApply` performs the following atomically:

1. expire overlap entries older than ten minutes;
2. look for an already-applied matching fingerprint from the other transport;
3. suppress and consume that matching twin when present;
4. otherwise verify that the arriving source is currently allowed;
5. execute the effect;
6. retain one bounded unmatched entry for the possible later twin.

During the temporary dual-publication period, SCS remains eligible as the
safety copy even after typed ingress becomes ready. Typed gRPC is eligible only
in the explicitly armed, fourth-generation, replay-complete state. Therefore:

- SCS first: SCS applies, typed twin is suppressed;
- typed first: typed applies, SCS twin is suppressed;
- one transport missing: the available copy still applies;
- transport order inversion: fingerprints match independently of arrival order;
- repeated penalty IDs: each occurrence has its own bounded overlap entry.

The overlap ledger retains at most 1,024 unmatched effects by default and never
more than 4,096. Reaching capacity fails closed, rolls modeled authority back
and permits SCS to continue. Expiry, retained entries and suppressed duplicates
are exposed in cutover status.

This is an overlap-safe production bridge. A later Master-side authority lease
will stop publishing the SCS copy after all intended recipients prove typed
readiness. At that point typed gRPC will naturally become the only arriving
copy without changing the receiver effect contract again.

## Effect dispatch

`CommunicationClient.UpdatePenaltyLog` records the SCS observation, configures
the immutable routing request before the first overlap event and submits the
legacy effect through the fingerprint-aware coordinator method.

`CommunicationCallbackShadowEnvelopeHandler` always retains the typed
observation. It resolves the registered production dispatcher lazily and
passes the validated envelope onward. `CommunicationCallbackEnvelopeDispatcher`
submits that envelope through the same coordinator and fingerprint ledger.

The production dispatcher is registered through a factory so constructing the
legacy SCS callback client does not recursively construct
`CommunicationServiceClient`.

Only `PenaltyRefresh` may execute through typed gRPC. Bazaar, family, relation,
static bonus, presence, session, lifecycle and global-event callbacks continue
to execute through SCS.

## Rollback

Rollback is terminal for modeled typed authority in the process. It occurs when:

- the operator requests rollback;
- qualification evidence is corrupted or invalidated;
- the configured request or apply authorization changes;
- a fifth or otherwise unapproved runtime generation appears;
- the active typed stream ends or faults;
- a typed `PenaltyRefresh` effect throws;
- overlap retention reaches its bounded capacity.

The coordinator closes `TypedIngressReady`, moves the cutover gate to
`RolledBack` and rejects typed reactivation until a clean process restart.
Pending overlap entries remain long enough to suppress late twins of effects
that completed immediately before rollback. New callbacks apply through SCS.

## Observable status

`GetPenaltyRefreshOperatorCutoverStatus` exposes an immutable snapshot with:

- whether the coordinator has been configured;
- whether the process is blocked;
- whether production effect routing was authorized;
- whether the replay-complete typed ingress barrier is open;
- pending overlap entries;
- total recorded effects;
- suppressed cross-transport duplicates;
- expired overlap entries;
- the operator request ID;
- the configured and qualified process identity;
- the most recently observed runtime generation;
- the gate state;
- the active generation, when present;
- the last fail-closed exception.

`RequestPenaltyRefreshOperatorRollback` remains available as an explicit
lifecycle control.

## Default safety

With the default environment, the gRPC subscriber is disabled and all callback
effects use SCS. With only the subscriber flag enabled, the system remains in
shadow observation mode. Typed production effects require the separate apply
flag, the operator request, qualified evidence, the fourth generation and the
replay-complete barrier.
