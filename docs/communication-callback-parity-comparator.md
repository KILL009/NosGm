# SCS-versus-gRPC callback parity comparator

## Purpose

Login and World can now produce one bounded, local parity report for the
comparable callback traffic observed through legacy SCS and the typed gRPC
shadow stream.

The comparator is diagnostic only:

- SCS remains the sole callback transport that applies gameplay, account and
  lifecycle effects;
- the gRPC subscriber remains observation-only;
- no parity result changes transport selection or acknowledges a callback;
- malformed evidence is isolated, logged and reported as `InvalidEvidence`.

## Terminal comparison boundary

The lifecycle automatically produces a report after a callback shadow stops.
An on-demand report while either observation window is active returns
`InProgress` and does not compare moving snapshots.

Only observations classified as live after the validated replay-complete
barrier enter comparison. Typed replay and SCS warmup evidence remain available
for diagnostics but cannot create positive parity.

Both windows must have:

- the same bounded process identity;
- the same canonical runtime generation;
- replay evidence with the same resume cursor, replay-through sequence and
  replay count;
- zero observation evictions;
- at least one live comparable callback.

## FIFO pairing

Live samples are paired by local FIFO position. Each pair must have the same:

- typed callback kind;
- uppercase payload-only SHA-256 semantic fingerprint.

The typed global sequence and SCS local ordinal do not need to be numerically
equal. They identify the first mismatching positions without pretending that
the two transports share a sequence allocator.

Repeated identical payloads remain distinct FIFO samples. Counts must match,
so duplicate or missing deliveries cannot disappear behind an identical hash.

## Fail-closed verdicts

The report exposes one verdict:

| Verdict | Meaning |
| --- | --- |
| `InProgress` | At least one observation window is still moving |
| `ReplayIncomplete` | One side never accepted the replay barrier |
| `IdentityMismatch` | Evidence belongs to different Login or World processes |
| `GenerationMismatch` | Evidence crosses runtime generations |
| `ReplayBoundaryMismatch` | The two sides did not share one replay boundary |
| `IncompleteEvidence` | At least one bounded ledger evicted observations |
| `NoLiveObservations` | The window has no live sample and cannot prove parity |
| `CountMismatch` | One transport retained more live callbacks |
| `OrderMismatch` | The first FIFO kind or fingerprint differs |
| `InvalidEvidence` | Evidence construction or validation failed |
| `Parity` | The complete non-empty live windows match in FIFO order |

`Parity` is evidence, not authorization. It cannot enable typed effects.

## Per-stream evidence

The typed observation handler now follows the same generation-local behavior
as the SCS ledger. A validated new stream:

- rejects overlap with an already active stream;
- clears observations from the previous generation;
- resets observed, sequence and eviction counters;
- rejects a repeated replay-complete transition.

This prevents evidence from an earlier reconnect or runtime generation from
polluting a later report.

## Production visibility

`CommunicationCallbackSubscriberLifecycle.ParityReport` exposes the active
non-terminal status or the last terminal report. Shutdown writes one bounded
`CALLBACK_PARITY_REPORT` log containing verdict, generation, counts, evictions
and the first mismatching typed sequence/SCS ordinal when applicable.

Payloads and rendered packets are never logged. Only the existing semantic
hashes participate in comparison.

## Validation

The .NET 10 runtime self-test verifies:

- positive FIFO parity;
- count, order, identity, generation and replay-boundary mismatches;
- eviction and active-window rejection;
- empty-window refusal;
- canonical fingerprints and monotonic source ordinals.

The Windows build compiles the lifecycle adapter into the production
.NET Framework Master library. Static verification confirms that the lifecycle
stores and logs the terminal report while preserving the SCS authority marker.

## Next boundary

The next slice may introduce a disabled-by-default atomic inbound activation
gate for one low-risk callback kind. It must require repeated successful parity
windows, preserve coordinated rollback and prove exactly-one effect delivery.
This comparator alone never opens that gate.
