# Typed communication callback shadow observation ledger

## Purpose

Each production Login or World callback shadow now retains a bounded local record of the typed callback envelopes it actually observed. This is the typed half of future SCS-versus-gRPC parity analysis.

The ledger is local, read-only evidence. It does not acknowledge events to the runtime, publish network traffic, mutate gameplay state or authorize typed effect application. SCS remains the only effect-applying callback transport.

## Stream context

`GrpcCommunicationCallbackSubscriber` detects handlers that implement `ICommunicationCallbackStreamObservationContext` and supplies three lifecycle transitions:

1. `BeginStream(runtimeGenerationId, resumeAfterSequence)` after runtime generation and durable cursor are validated;
2. `CompleteReplay(evidence)` after the server-issued replay barrier is validated;
3. `EndStream()` on every stream exit and before each reconnect attempt.

`CommunicationCallbackShadowEnvelopeHandler` therefore records each observation with the active runtime generation and one explicit phase:

- `Replay` for callbacks applied before the replay-complete barrier;
- `Live` for callbacks applied after the barrier.

A callback cannot be recorded while no stream context is active.

## Observation record

Every retained `CommunicationCallbackShadowObservation` contains:

- runtime generation ID;
- canonical event ID;
- accepted global sequence;
- typed callback kind;
- replay or live phase;
- SHA-256 semantic fingerprint;
- local observation timestamp.

The semantic fingerprint is computed from a synthetic Protobuf envelope containing only the typed callback payload. It deliberately excludes:

- EventId;
- sequence;
- issue and expiry timestamps;
- callback target and recipient scope.

This allows a later SCS observer to construct the same semantic payload fingerprint from legacy callback arguments. Repeated identical payloads will share a fingerprint and must later be paired in FIFO observation order rather than treated as unique solely by hash.

## Bounded memory

The ledger defaults to 4,096 observations and has an absolute ceiling of 16,384. It uses a FIFO queue:

- when capacity is reached, the oldest observation is evicted;
- total callback count remains cumulative;
- eviction count is cumulative and visible;
- snapshots copy the queue and never expose the mutable collection.

Any parity report built from a ledger with nonzero evictions must declare the observation window incomplete unless its requested window begins after all evicted entries.

## Production visibility

`CommunicationCallbackSubscriberLifecycle` exposes:

- observation capacity;
- cumulative observation and eviction counts;
- last observed sequence;
- a defensive observation snapshot;
- existing replay evidence and active runtime generation.

Startup and shutdown logs include retained and evicted observation counts.

## Next boundary

The next slice will instrument the legacy SCS callback receiver with the same semantic fingerprint vocabulary and a separate bounded ledger. A comparator can then pair live observations by process identity, callback kind, semantic fingerprint and FIFO order after the replay boundary.

No transport cutover is permitted merely because fingerprints exist. Parity must first prove complete, non-evicted observation windows and explain every unmatched or reordered callback.
