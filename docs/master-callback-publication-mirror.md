# Master communication callback publication mirror

The legacy Master process can now publish typed copies of existing SCS callbacks to the central .NET 10 callback runtime. This is an observation and replay-validation stage. SCS remains the only transport allowed to apply gameplay, account or lifecycle effects.

## Activation

The mirror reads:

```text
NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_ENABLED
NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_QUEUE_CAPACITY
NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_STOP_TIMEOUT_MILLISECONDS
```

Activation defaults to `false`. When disabled, Master does not load the callback-publisher certificate and creates no worker or queue.

The bounded queue defaults to 4,096 entries and accepts values from 64 through 16,384. Shutdown waits up to 5,000 milliseconds by default and accepts values from 1,000 through 30,000 milliseconds. Boolean values accept only `true` or `false` without surrounding whitespace.

Publisher credentials remain in the communication-specific Master namespace:

```text
NOSGM_COMMUNICATION_GRPC_URL
NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PATH
NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PASSWORD
NOSGM_COMMUNICATION_GRPC_TRUSTED_ROOT_CERT_PATH
NOSGM_COMMUNICATION_GRPC_MASTER_INSTANCE_ID
NOSGM_COMMUNICATION_GRPC_DEADLINE_MILLISECONDS
NOSGM_COMMUNICATION_GRPC_WIRE_MODE
```

The AuthBridge certificate namespace is never read by the callback publisher.

## SCS-first ordering

Master registers `MirroredCommunicationService` as its SCS communication service. It inherits the complete legacy implementation and reimplements only the ten methods that currently emit supported callbacks.

Every reimplemented method follows the same order:

1. execute the original `CommunicationService` method;
2. return or propagate its existing SCS result;
3. enqueue a typed mirror publication without waiting for network I/O.

A mirror construction, queue or publication failure cannot replace the SCS result. `SendMessageToCharacter` is inherited unchanged and is not mirrored.

## Bounded asynchronous publication

`MasterCommunicationCallbackMirror` owns one bounded FIFO `BlockingCollection` and one publisher worker. Callback threads use non-blocking `TryAdd`; they never wait for gRPC.

Each queued item contains one immutable Protobuf publication template with a canonical event GUID. Every network attempt clones that template and creates a fresh request GUID and deadline while preserving the event GUID and semantic payload. The runtime therefore treats a transient retry as idempotent and returns the original accepted sequence.

Transient transport and capacity failures retry in FIFO order with bounded exponential delay. A local item expires after its callback TTL. Queue saturation, local expiry, build failures and terminal publisher failures are counted and logged. None of them invokes or retries the SCS callback.

On shutdown Master completes the queue and allows a bounded drain. When the timeout expires, it cancels the worker and disposes the gRPC channel without aborting a thread.

## Mirrored callback inventory

| Legacy emitter | Typed payload | Target |
| --- | --- | --- |
| `ConnectCharacter` | character presence connected | World group |
| `DisconnectCharacter` | character presence disconnected | World group |
| `KickSession` | kick session | All Worlds |
| `Restart` | lifecycle restart | All Worlds or World group |
| `RunGlobalEvent` | global event | All Worlds |
| `Shutdown` | lifecycle shutdown | All Worlds or World group |
| `UpdateBazaar` | Bazaar refresh | World group |
| `UpdateFamily` | Family refresh | World group |
| `RefreshPenalty` | Penalty refresh | All Login and World nodes |
| `UpdateRelation` | Relation refresh | World group |

`UpdateStaticBonus` has a typed request builder for the future `CharacterId` route, but the current `ICommunicationService` exposes no SCS emitter to mirror. It is not fabricated from another operation.

`SendMessageToCharacter` remains deferred because its DTO may contain rendered client packets and complex routing rules. It requires a separate typed messaging contract.

## Observability

Master records:

- enabled or disabled startup;
- bounded queue capacity and shutdown timeout;
- queue drops with logarithmically throttled warnings;
- locally expired publications;
- terminal publisher failure while declaring SCS authoritative;
- final enqueued, published, dropped and expired counts.

A publication count proves that the central runtime accepted the typed event. It does not mean the shadow subscriber applied a gameplay effect. Shadow handlers remain observation-only.

## Next cutover boundary

The next stage is parity evidence and a server-issued replay-complete barrier. Login and World must prove that their shadow cursors have consumed every mirrored event up to a declared sequence. Only then can an atomic inbound gate disable the matching SCS callback and enable the typed dispatcher exactly once, without a gap or duplicate effect.
