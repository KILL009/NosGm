# Communication callback subscriber

This slice adds the dual-target `net481` / `net10.0` client that consumes typed callbacks from the central .NET 10 runtime. The client is not started by the production Login or World processes yet, and the guarded SCS communication cutover remains unchanged.

## Connection identity

Only Login and World may create a subscriber. The client reads a communication-specific environment namespace:

```text
NOSGM_COMMUNICATION_GRPC_URL
NOSGM_COMMUNICATION_GRPC_CLIENT_CERT_PATH
NOSGM_COMMUNICATION_GRPC_CLIENT_CERT_PASSWORD
NOSGM_COMMUNICATION_GRPC_TRUSTED_ROOT_CERT_PATH
NOSGM_COMMUNICATION_GRPC_CALLER_INSTANCE_ID
NOSGM_COMMUNICATION_GRPC_SETUP_DEADLINE_MILLISECONDS
NOSGM_COMMUNICATION_GRPC_WIRE_MODE
NOSGM_COMMUNICATION_GRPC_CALLBACK_CURSOR_PATH
NOSGM_COMMUNICATION_GRPC_CALLBACK_RECONNECT_INITIAL_MILLISECONDS
NOSGM_COMMUNICATION_GRPC_CALLBACK_RECONNECT_MAXIMUM_MILLISECONDS
```

Login is forbidden from supplying World fields. World must supply the exact registered World ID, assigned channel and group. Master cannot open a subscriber stream.

The endpoint must be an HTTPS loopback origin. The client certificate, optional trusted root and durable cursor paths must be absolute and pairwise distinct. The cursor writer is never allowed to overwrite either certificate file.

## Transport selection

Native HTTP/2 is the primary Windows 11 path. `HTTP2` is also the default when `NOSGM_COMMUNICATION_GRPC_WIRE_MODE` is not set. The legacy `net481` process uses `WinHttpHandler`, while .NET 10 uses `SocketsHttpHandler` with TLS 1.2 or TLS 1.3.

`GRPCWEB` remains an explicit compatibility mode. It uses binary gRPC-Web over TLS 1.2 with the process certificate selected explicitly. New migration work, acceptance decisions and production wiring target native HTTP/2 first.

The selector is resolved before opening the stream. There is no automatic fallback from one wire mode to another and no fallback to SCS after a gRPC dispatch.

## Envelope validation

Before a new sequence may enter a handler or advance the cursor, the client verifies:

1. a canonical non-empty event GUID;
2. a positive sequence inside the signed runtime range;
3. a positive issued time and a strictly later expiry;
4. an event lifetime no longer than the bounded server replay TTL;
5. a defined target with no contradictory target details;
6. a supported typed payload whose identity and target match the authoritative callback contract.

A malformed envelope fails closed. It is not applied and its sequence is not saved. Duplicate or older sequences are ignored before this validation because they have already been durably acknowledged by the same process identity.

## Durable cursor

The subscriber loads one unsigned 64-bit sequence from its cursor store. The file implementation writes ASCII to a temporary file, flushes it to disk, then atomically replaces the previous cursor.

A corrupt cursor fails closed. A missing cursor begins at zero.

For every valid envelope:

1. sequences already at or below the durable cursor are ignored;
2. an unexpired envelope is passed to the typed handler;
3. the cursor is saved only after the typed handler returns successfully;
4. an expired envelope is not applied, but its sequence is durably skipped;
5. a handler exception is surfaced and leaves the previous cursor unchanged.

This gives at-least-once delivery at the typed handler boundary. A process crash after the handler returns but before the atomic cursor write may replay that event, so callback handlers must remain safe for repeat application.

The legacy handler surface is not uniformly completion-aware. Some handlers, including global-event generation and restart or shutdown scheduling, enqueue asynchronous work and return before the complete gameplay effect finishes. Therefore a cursor commit currently proves successful typed dispatch, not completion of every downstream business effect. Production cutover must either make those handlers awaitable and idempotent or explicitly accept and test that boundary.

The current cursor is runtime-generation scoped. It can recover a Login or World process restart while the central callback runtime still retains that subscriber state. The central runtime currently keeps its sequence and replay registry in memory, so production lifecycle wiring must rotate or bind the cursor to a runtime generation before the callback cutover is enabled. A stale cursor from a previous runtime generation fails closed with an unavailable replay-cursor error.

## Controlled reconnection

The subscriber opens one stream at a time. Transient gRPC status codes reconnect with bounded exponential delay. Fatal authorization, invalid-request, replay-cursor and data-loss errors escape to the process supervisor.

A second `RunAsync` call on the same subscriber fails immediately. Cancellation stops retry and stream processing.

## Existing handler reuse

`CommunicationCallbackEnvelopeDispatcher` maps typed envelopes to the current `CommunicationServiceClient.On...` methods:

- character connected or disconnected;
- session kick;
- restart or shutdown;
- global event;
- Bazaar refresh;
- Family refresh;
- Penalty refresh;
- Relation refresh;
- Static Bonus refresh.

Global events use `CommunicationGlobalEventMapper`. The Protobuf enum starts at one while the legacy domain enum starts at zero, so direct numeric casts are forbidden. The mapper contains an explicit two-way entry for every supported event and can later be reused by the Master publisher.

The deferred `SCSCharacterMessage` path is not accepted by the typed dispatcher.

## Live acceptance

The isolated acceptance runtime performs the complete loop over both supported wire modes, with native HTTP/2 treated as the primary Windows 11 route:

1. each wire-mode execution uses a unique Login subscriber process identity;
2. Login opens a real callback server stream using the Login certificate;
3. Master publishes a typed penalty refresh using the separate Master certificate;
4. the runtime routes it to the Login subscriber;
5. the handler applies the envelope exactly once;
6. the test observes the matching cursor commit after handler completion;
7. cancellation closes the stream.

Unique test identities prevent the second wire-mode execution from inheriting retained events from the first while preserving the runtime's real replay behavior.

## Current migration boundary

Production remains on the SCS callback path. This slice provides the client, durable processing semantics and typed dispatcher only. A later coordinated PR must start the subscriber after successful Login/World registration, bind the cursor to the active runtime generation, disable the matching SCS callbacks at the same boundary, resolve the downstream completion boundary, and prove full real-client behavior before the communication transport selector can allow gRPC.
