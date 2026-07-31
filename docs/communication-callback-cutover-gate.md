# PenaltyRefresh callback cutover gate foundation

## Purpose

This slice adds the transport-neutral authority state machine needed before one
legacy SCS callback can be replaced by its typed gRPC equivalent.

The first supported callback kind is `PenaltyRefresh`. It is a cache-refresh
notification with an existing typed contract, exact `ALL_NODES` routing and
matching payload-only semantic fingerprints on both transports.

This foundation is deliberately not wired into the production SCS receiver or
the typed shadow handler. `CommunicationCallbackActivationOptions` continues to
reject callback effect application. SCS therefore remains the only transport
that can apply effects in Login and World.

## Kind-local parity evidence

Whole-window parity remains useful for broad diagnostics, but cutover
qualification must be specific to the callback kind being moved.

`CommunicationCallbackKindParityComparator` filters both terminal observation
windows to one callback kind and then delegates to the existing fail-closed
FIFO comparator. It preserves:

- process identity and runtime generation checks;
- replay-complete boundary equality;
- eviction refusal;
- non-empty live evidence;
- equal counts, semantic fingerprints and FIFO order.

A mismatch in another callback kind cannot falsely block or qualify
`PenaltyRefresh`. An observation window with no `PenaltyRefresh` traffic
returns `NoLiveObservations` and cannot count toward qualification.

## Qualification

The gate requires three distinct successful terminal windows by default.

All qualifying entries must:

- belong to the same bounded Login or World process identity;
- describe `PenaltyRefresh`;
- report `Parity`;
- contain equal, non-zero typed and SCS live counts;
- use distinct runtime generation IDs;
- be ordered by their terminal observation time.

A generation used to qualify the gate cannot also be used to activate it.
Activation therefore requires a new stream generation after the evidence was
collected.

## Authority states

The gate has four states:

| State | Effect authority |
| --- | --- |
| `ScsAuthoritative` | SCS applies every callback |
| `Armed` | SCS still applies every callback |
| `TypedGrpcAuthoritative` | typed gRPC applies only `PenaltyRefresh`; SCS remains authoritative for all other kinds |
| `RolledBack` | SCS applies every callback and the gate cannot reactivate in the same process |

`ShouldApply` always returns one authority for a known callback kind. There is
no automatic fallback from a failed typed effect to SCS because retrying a
stateful callback through the second transport could duplicate the effect.

Authority transitions use one process-local atomic state. Rollback is terminal
for that gate instance so stale parity evidence cannot reopen it.

## Current production boundary

The gate is a pure, tested foundation only.

Production still has these safeguards:

- `NOSGM_COMMUNICATION_GRPC_CALLBACKS_ENABLED=true` enables observation only;
- `NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED=true` still fails closed;
- `CommunicationClient` continues to dispatch every SCS callback;
- `CommunicationCallbackShadowEnvelopeHandler` continues to record typed
  callbacks without applying them.

This separation lets the state machine and evidence rules compile on both
`net481` and `net10.0` before any effect path is touched.

## Validation

The .NET 10 runtime self-test verifies:

- kind-local positive and negative parity;
- empty-kind refusal;
- the three-window minimum;
- distinct generation enforcement;
- identity binding;
- SCS authority while armed;
- exactly one authority after activation;
- isolation of unselected callback kinds;
- immediate terminal rollback to SCS.

The static verifier also proves that the production receiver and shadow handler
do not reference the gate yet.

## Next boundary

The next slice will persist bounded kind-local qualification evidence and wire a
coordinated activation boundary into Login and World. Only then can the SCS
`UpdatePenaltyLog` receiver be suppressed while the typed handler applies the
matching `PenaltyRefresh` effect exactly once.
