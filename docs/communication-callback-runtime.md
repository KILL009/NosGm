# Communication callback runtime

This slice implements the central .NET 10 runtime for typed Master → Login/World callbacks. It does not activate the production gRPC communication selector and does not remove the SCS callback path.

## Authority boundary

`ClusterCommunicationState` remains authoritative for account, session, character and World mutations. `CommunicationCallbackHub` is a derived routing index. It is updated only after an authoritative mutation succeeds:

- successful World registration adds the assigned World ID, channel and group;
- successful character attachment adds its exact World/account/session tuple;
- pulses refresh the derived character route;
- character, account and World teardown removes derived routes.

A routing-index failure during World registration rolls the authoritative World registration back. The callback index therefore cannot silently drift into a second cluster authority.

## Subscription model

Login and World processes open `SubscribeCommunicationCallbacks`, a server-streaming RPC supported by binary gRPC-Web on Windows 10.

A subscriber identity consists of:

- certificate role;
- caller instance ID;
- for World only, the registered World ID, assigned channel and World group;
- the normalized callback-kind filter.

Only one active stream may own one process identity. Reconnecting with changed World details or filters fails closed. World subscriptions must match the derived authoritative registration.

## Bounded memory

The runtime enforces three independent bounds:

- at most `NOSGM_COMMUNICATION_MAX_CALLBACK_SUBSCRIBERS` retained subscriber states, default 2,048 and maximum 8,192;
- 1,024 pending events per active subscriber;
- 4,096 retained replay events per subscriber.

If the pending queue fills, the stream is terminated with `ResourceExhausted`. The accepted event is retained before termination so the process can reconnect from its last durably applied sequence. Inactive subscriber states are the only states eligible for capacity eviction.

## Replay and sequence

Every newly accepted event receives one process-wide monotonic sequence. A reconnect supplies `resume_after_sequence`, and receives only retained, non-expired events with a larger sequence.

The runtime rejects:

- a cursor beyond the current sequence;
- a new process identity claiming an unknown non-zero cursor;
- a cursor older than an unexpired event removed because the 4,096-event replay bound was exceeded.

Global sequence gaps are normal because an event may not target a particular subscriber.

## Idempotency and TTL

Master supplies a canonical GUID `event_id`. The runtime fingerprints the target, callback payload and TTL without including transport context.

- publishing the same event ID with the same semantic request returns the original sequence and does not deliver twice;
- reusing the event ID with another payload returns `Conflict`;
- event IDs and replay entries are removed after their bounded TTL;
- expired events are never sent or replayed.

## Routing

The runtime supports the contract targets:

- all Worlds;
- World group;
- World ID;
- all Login nodes;
- all nodes;
- character ID resolved through the exact derived character route.

Login receives only callback kinds allowed by the contract. World subscribers receive only events matching their registered identity and filter.

## Publication security

`PublishCommunicationCallback` accepts only a certificate mapped to `ClusterNodeRole.Master`. The request context must also declare Master and Communication. Publication remains unavailable when the Master fingerprint allow-list is empty.

Subscriptions accept only Login and World certificates. The stream setup request uses a bounded, replay-protected request context, while the long-running stream itself is terminated by client cancellation, queue overflow, World unregistration or server shutdown.

## Current migration boundary

The runtime is intentionally isolated. Production `CommunicationServiceClient` still defaults to SCS and the guarded gRPC selector remains blocked. The next slices must add the net481 callback subscriber/client adapter, migrate the legacy callback handlers, and prove coordinated state plus callback cutover before removing that guard.

Production CommunicationServiceClient still defaults to SCS.
