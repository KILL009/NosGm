# Operator PenaltyRefresh callback cutover handshake

## Purpose

NosGM now retains terminal `PenaltyRefresh` parity evidence from the legacy SCS
callback path and the typed gRPC callback stream. Qualification alone must never
move production authority.

This slice adds the operator-controlled handshake that sits between qualified
evidence and a later production effect-routing change.

It does not route production effects. SCS remains the only production effect
path, and `NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED=true` continues to
fail closed.

## Operator request

The process reads two strict environment variables when the first terminal
`PenaltyRefresh` evidence window is captured:

- `NOSGM_COMMUNICATION_GRPC_CALLBACKS_PENALTY_REFRESH_ARM_REQUEST_ID`
- `NOSGM_COMMUNICATION_GRPC_CALLBACKS_PENALTY_REFRESH_ROLLBACK_REQUESTED`

The arm request is disabled by default. Enabling it requires an exact lowercase
canonical non-empty GUID. The GUID is retained in process-local status so an
operator can correlate the request with logs, deployment notes or a change
record.

Rollback accepts only `true` or `false` and defaults to `false`. Arm and rollback
cannot be requested together.

The first accepted configuration is immutable for the lifetime of the process.
Changing the request ID or rollback value requires a process restart. A detected
configuration mutation permanently blocks cutover and restores SCS authority.

## Arming rules

`CommunicationCallbackOperatorCutoverCoordinator` can arm only when all of the
following are true:

1. a valid operator request ID is present;
2. the qualification ledger has a complete, non-evicted history;
3. the latest three terminal windows report non-empty `PenaltyRefresh` parity;
4. all three windows belong to one process identity;
5. all three windows use distinct runtime generations;
6. the process has not been blocked or rolled back.

Without the operator request, the same qualification evidence remains purely
diagnostic and SCS stays authoritative.

## New-generation activation

Arming does not activate authority immediately. The three qualification
generations are remembered by the existing cutover gate.

When the typed callback subscriber begins another stream, the coordinator
observes its Authentication runtime generation. Reusing any of the three
qualification generations cannot activate the gate. The first acceptable
activation therefore occurs on a fourth distinct runtime generation.

Activation remains scoped to that exact generation. Seeing another generation
after modeled typed authority has become active triggers terminal rollback for
the process. A clean process restart and a new operator request are required to
try again.

## Rollback

Rollback is available through both the strict startup option and the lifecycle
diagnostic extension `RequestPenaltyRefreshOperatorRollback`.

Rollback blocks reactivation for the rest of the process and makes the cutover
gate select SCS for `PenaltyRefresh`. Every callback kind other than
`PenaltyRefresh` always selects SCS.

Malformed request input, qualification corruption, evidence invalidation,
configuration mutation and generation drift all use the same fail-closed
rollback boundary.

## Observable status

`GetPenaltyRefreshOperatorCutoverStatus` exposes an immutable snapshot with:

- whether the coordinator has been configured;
- whether the process is blocked;
- the operator request ID;
- the configured and qualified process identity;
- the most recently observed runtime generation;
- the gate state;
- the active generation, when present;
- the last fail-closed exception.

The status is diagnostic only and does not route production effects.

## Safety boundary

This slice deliberately does not reference the coordinator from
`CommunicationClient` or `CommunicationCallbackEnvelopeDispatcher`.

Therefore:

- legacy SCS callback delivery is unchanged;
- typed gRPC callback handling remains observation-only;
- no callback can be applied twice through this handshake;
- `SendMessageToCharacter` remains excluded;
- SCS remains the only production effect path.

The next slice may connect the modeled authority decision to exactly the
`PenaltyRefresh` effect path behind the existing blocked application flag. That
change must atomically suppress SCS before typed dispatch is allowed, preserve
an immediate rollback path and retain all other callback kinds on SCS.
