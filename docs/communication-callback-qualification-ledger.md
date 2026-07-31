# PenaltyRefresh callback parity qualification ledger

## Purpose

The cutover gate introduced after the SCS-versus-gRPC parity comparator needs a
small, process-local history of terminal evidence. A single successful stream
window is not enough to move callback authority.

`CommunicationCallbackKindParityEvidenceLedger` retains the bounded evidence
that a later lifecycle integration will feed into the `PenaltyRefresh` cutover
gate.

This slice does not collect evidence from production streams and does not wire
the gate into either callback transport. SCS remains the only callback path that
applies effects.

## Scope

The first ledger accepts only
`CommunicationCallbackKind.PenaltyRefresh` evidence.

Each retained entry already contains:

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

Ordinary capacity eviction does not make the recent suffix unusable. The gate
still evaluates only its latest required evidence window, currently three
terminal generations.

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

## Qualification behavior

`TryArm` takes an atomic snapshot while holding the evidence lock and delegates
the decision to `CommunicationCallbackCutoverGate`.

The ledger cannot weaken the gate rules. The gate still requires:

- three most-recent successful parity entries;
- equal non-zero SCS and typed counts;
- one process identity;
- three distinct runtime generations;
- strict terminal time ordering.

Arming only records qualification. It does not activate typed effects.

## Validation

The compiled .NET 10 self-test covers:

- three successful terminal generations;
- idempotent retries;
- latest-suffix ordering;
- a mismatch breaking the qualification streak;
- later clean generations recovering qualification;
- exact FIFO capacity and eviction counters;
- permanent invalidation after conflicting, out-of-order, cross-identity,
  cross-kind or moving evidence;
- SCS authority after every failed qualification path.

A dedicated static workflow verifies the bounded constants, invalidation rules,
atomic `TryArm` snapshot and the continued production application block.

## Next boundary

The next slice will capture a terminal typed observation window before it is
cleared, compare it with the matching SCS window, append the resulting
`PenaltyRefresh` evidence to this ledger and expose its state through
`CommunicationCallbackSubscriberLifecycle`.

That integration will still leave `NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED`
blocked. Production authority will move only in a later coordinated activation
slice with an explicit rollback path.
