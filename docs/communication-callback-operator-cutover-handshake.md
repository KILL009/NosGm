# Operator PenaltyRefresh callback cutover handshake

## Purpose

NosGM retains terminal `PenaltyRefresh` parity evidence from the legacy SCS
callback path and the typed gRPC callback stream. Qualification alone never
moves production authority.

This slice connects the operator-controlled handshake to the real
`PenaltyRefresh` effect path. It routes production effects only after explicit
activation, three clean parity generations, a fourth distinct runtime
generation and replay completion.

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

Arming does not activate production effects immediately. The three
qualification generations are remembered by the cutover gate.

When the typed callback subscriber begins another stream, the coordinator
observes its Authentication runtime generation. Reusing any of the three
qualification generations cannot activate the gate. The first acceptable
activation therefore occurs on a fourth distinct runtime generation.

The new generation initially remains behind a replay barrier:

- replayed typed envelopes are observed but do not execute effects;
- SCS remains authoritative throughout replay;
- replay completion and the authority transition share one process-local lock;
- only live envelopes after replay completion may use typed authority.

The typed stream handler and legacy SCS receiver call the same atomic
`TryApply` decision. Once typed ingress is ready, SCS is suppressed before the
typed effect is permitted. There is never a process-local state in which both
sources are selected for `PenaltyRefresh`.

## Effect dispatch

`CommunicationClient.UpdatePenaltyLog` records the SCS observation and then
asks the coordinator whether legacy SCS may execute the effect.

`CommunicationCallbackShadowEnvelopeHandler` always retains the typed
observation. It resolves the registered production dispatcher lazily and
passes the validated envelope onward. `CommunicationCallbackEnvelopeDispatcher`
asks the same coordinator whether typed gRPC may execute the effect.

The production dispatcher is registered through a factory so constructing the
legacy SCS callback client does not recursively construct
`CommunicationServiceClient`.

Only `PenaltyRefresh` can select typed gRPC. Bazaar, family, relation, static
bonus, presence, session, lifecycle and global-event callbacks continue to
select SCS.

## Rollback

Rollback is terminal for the process and restores SCS before another callback
can consume typed authority. It occurs when:

- the operator requests rollback;
- qualification evidence is corrupted or invalidated;
- the configured request or apply authorization changes;
- a fifth or otherwise unapproved runtime generation appears;
- the active typed stream ends or faults;
- a typed `PenaltyRefresh` effect throws.

The coordinator closes `TypedIngressReady`, moves the cutover gate to
`RolledBack` and rejects reactivation until a clean process restart.

## Observable status

`GetPenaltyRefreshOperatorCutoverStatus` exposes an immutable snapshot with:

- whether the coordinator has been configured;
- whether the process is blocked;
- whether production effect routing was authorized;
- whether the replay-complete typed ingress barrier is open;
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
shadow observation mode. Production effect routing requires the separate apply
flag, the operator request, qualified evidence, the fourth generation and the
replay-complete barrier.
