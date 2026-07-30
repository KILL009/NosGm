# Communication callback replay-complete barrier

## Purpose

The callback stream now has an explicit server-issued boundary between retained replay and newly pending delivery. This boundary is readiness evidence for the shadow-observation stage. It is not a gameplay callback, an acknowledgement RPC or permission to disable SCS.

SCS remains the only callback transport allowed to apply effects.

## Capability negotiation

`SubscribeCommunicationCallbacksRequest.supports_replay_complete_barrier` is additive and defaults to `false` on older generated clients.

- a client that sends `false` receives the legacy stream with callback envelopes only;
- a client that sends `true` receives exactly one `CommunicationCallbackReplayComplete` control envelope after retained replay and before pending delivery;
- the global contract remains version 1.0 because an old client never receives the control variant it cannot interpret.

## Atomic boundary

Subscription opening and callback publication share one process-wide, short-lived synchronization gate in `ClusterCommunicationCallbackService`.

The server opens the hub subscription and captures `CurrentSequence` while publication is excluded. The gate is released before any network write. Therefore:

- callbacks accepted before the snapshot are on the replay side of the boundary;
- callbacks accepted after the snapshot enter the pending side with a greater sequence;
- replay transmission, gRPC backpressure and callback handlers never hold the gate.

The boundary may cover callbacks that were not targeted to this subscriber. That is intentional because callback sequences are global. `replayed_events` counts only envelopes actually written to this subscriber before the boundary.

## Control envelope

A replay-complete envelope contains:

- the runtime generation ID;
- the global `replay_through_sequence` snapshot;
- the subscriber's durable `resume_after_sequence`;
- the number of callback envelopes replayed on this stream.

It contains no event ID, issue time, expiry time or callback target. Master cannot publish this control because it exists only in the server-stream envelope union, not in `PublishCommunicationCallbackRequest`.

## Subscriber behavior

`GrpcCommunicationCallbackSubscriber` announces support and resets readiness at every stream attempt. It processes incoming elements sequentially:

1. callback envelopes before the barrier are counted as replay and pass through the normal processor;
2. the barrier is validated by `CommunicationCallbackReplayTracker` and never reaches the processor;
3. callback envelopes after the barrier must have a sequence greater than the declared boundary;
4. a malformed, duplicate or generation-mismatched barrier fails closed;
5. stream termination clears active readiness so a dead connection cannot remain green.

Because the barrier bypasses `CommunicationCallbackProcessor`, it never invokes the callback handler and never advances the durable callback cursor.

## Exposed evidence

`CommunicationCallbackSubscriberLifecycle` exposes the active subscriber's:

- runtime generation ID;
- applied callback sequence;
- replay-complete state;
- immutable replay evidence with boundary, resume cursor, replay count and completion timestamp.

The existing shadow handler continues to report only actual callback envelopes through `ObservedCallbacks` and `LastObservedSequence`.

## Cutover boundary

Replay completion establishes the start of a trustworthy observation window. It does not itself prove payload parity or authorize typed effect application.

The legacy SCS receiver now uses this same boundary to divide warmup from live
observations. A later slice must compare the two bounded ledgers after this
boundary and introduce an atomic inbound cutover gate. Only then may a matching
SCS callback be disabled and the typed dispatcher be allowed to apply that
effect exactly once.
