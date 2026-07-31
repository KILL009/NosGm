# PenaltyRefresh callback parity qualification ledger

## Purpose

The cutover gate introduced after the SCS-versus-gRPC parity comparator needs a
small, process-local history of terminal evidence. A single successful stream
window is not enough to move callback authority.

`CommunicationCallbackKindParityEvidenceLedger` retains bounded
`PenaltyRefresh` evidence across typed callback stream reconnects inside one
Login or World process.

The ledger originally landed as an isolated foundation. The terminal capture
integration now feeds it from matched production observation windows whenever a
typed stream and its SCS observation window close. This still does not wire the
gate into either effect-applying callback path. SCS remains the only callback
transport that applies effects.

## Scope

The first ledger accepts only
`CommunicationCallbackKind.PenaltyRefresh` evidence.

Each retained entry contains:

- one bounded Login or World process identity;
- one Authentication runtime generation ID;
- the terminal callback-kind parity verdict;
- typed and SCS live observation counts;
- the terminal observation time.

The ledger is intentionally in-memory. It spans callback stream reconnects
inside one Login or World process but cannot silently carry authority evidence
through a process restart. Durable operator authorization belongs to the later
coordinated activation boundary, not to this diagnostic retention layer.

## Bounded retention

- default capacity: 16 terminal generations;
- absolute ceiling: 64 terminal generations;
- FIFO eviction of the oldest entry;
- cumulative append and eviction counters;
- defensive array snapshots;
- a bounded latest-evidence suffix for diagnostics.

Eviction keeps recent diagnostic evidence available, but it makes the complete
process history unavailable. `TryArm` therefore refuses authority qualification
after the first eviction. A clean process must collect a new complete history
instead of trusting a suffix that could hide a reused or conflicting runtime
generation.

## Identity and generation integrity

The first accepted entry binds the ledger to one exact process identity.
Evidence from another Login or World process is rejected.

An exact retry for the same runtime generation is idempotent. A different
verdict, count or timestamp for an already retained generation is conflicting
evidence and invalidates the ledger for the rest of the process.

Evidence must arrive in strictly increasing terminal observation order. A
moving `InProgress` window, a callback-kind mismatch, an identity mismatch,
out-of-order evidence or a conflicting generation permanently invalidates the
ledger instance.

Invalidation is deliberate. Once evidence integrity becomes ambiguous, the
same process cannot arm callback authority from the surviving entries. SCS
remains authoritative until a clean process starts with an empty ledger.

The terminal capture runtime can also invalidate the ledger explicitly when a
stream closes without its synchronous typed counterpart or when terminal
adapter evidence is malformed. It records the exception without fabricating a
parity verdict.

## Qualification behavior

`TryArm` takes an atomic snapshot while holding the evidence lock and delegates
the decision to `CommunicationCallbackCutoverGate`.

The ledger cannot weaken the gate rules. The gate still requires:

- three most-recent successful parity entries;
- equal non-zero SCS and typed counts;
- one process identity;
- three distinct runtime generations;
- strict terminal time ordering;
- no qualification-history eviction.

Arming only records qualification. It does not activate typed effects.

## Production observation integration

`CommunicationCallbackQualificationRuntime` receives terminal typed and SCS
windows from the synchronous stream-closure handoff. It compares only
`PenaltyRefresh`, appends valid parity or mismatch evidence, and exposes an
immutable qualification status.

A valid mismatch breaks the three-generation streak but does not invalidate the
ledger. Missing or structurally ambiguous evidence invalidates it permanently
for the process. In every case, SCS continues to apply the callback.

The status and bounded evidence snapshot are available through companion
methods on `CommunicationCallbackSubscriberLifecycle`. They are diagnostic
views only and cannot change authority.

## Validation

The compiled .NET 10 self-tests cover:

- three successful terminal generations;
- idempotent retries;
- latest-suffix ordering;
- a mismatch breaking the qualification streak;
- later clean generations recovering qualification;
- exact FIFO capacity and eviction counters;
- qualification refusal after evidence eviction;
- permanent invalidation after conflicting, out-of-order, cross-identity,
  cross-kind, moving or missing terminal evidence;
- SCS authority after every failed qualification path.

Dedicated static workflows verify the bounded constants, invalidation rules,
atomic `TryArm` snapshot, terminal production capture and the continued
production application block.

## Next boundary

The next slice will add an explicit operator-controlled arming request and a
fresh-generation activation handshake for `PenaltyRefresh`.

`NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED` remains blocked until that
coordinated activation slice provides one-authority enforcement and terminal
rollback to SCS.
