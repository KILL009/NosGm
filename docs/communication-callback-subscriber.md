# Communication callback subscriber

This slice provides the dual-target `net481` / `net10.0` client that consumes typed callbacks from the central .NET 10 runtime. Production Login and World can now own the subscriber in an explicit disabled-by-default shadow mode. The guarded SCS communication cutover remains unchanged.

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

## Runtime-generation handshake

Each central callback runtime creates a new canonical generation GUID when its process starts. This value is intentionally different from the configured server instance name and changes even when the process restarts with identical configuration.

Before every stream connection the subscriber:

1. calls `GetCommunicationCallbackRuntimeInfo` with its Login or World mTLS identity;
2. validates the returned generation, start time and current sequence;
3. binds the durable cursor to that generation;
4. sends the same generation in `SubscribeCommunicationCallbacks`.

The server compares the supplied generation with the process currently serving the stream. If the runtime restarted between the unary query and stream setup, the subscription fails with `FailedPrecondition`. The reconnect loop queries the runtime again and binds to the new generation before retrying.

## Envelope validation

Before a new sequence may enter a handler or advance the cursor, the client verifies:

1. a canonical non-empty event GUID;
2. a positive sequence inside the signed runtime range;
3. a positive issued time and a strictly later expiry;
4. an event lifetime no longer than the bounded server replay TTL;
5. a defined target with no contradictory target details;
6. a supported typed payload whose identity and target match the authoritative callback contract.

A malformed envelope fails closed. It is not applied and its sequence is not saved. Duplicate or older sequences are ignored before this validation because they have already been durably acknowledged by the same process identity and runtime generation.

## Durable cursor

The durable cursor is runtime-generation scoped. The file cursor uses the following ASCII structure:

```text
NOSGM_CALLBACK_CURSOR_V1
<runtime-generation-guid>
<unsigned-sequence>
```

The file implementation writes to a temporary file, flushes it to disk and atomically replaces the previous cursor. The client certificate, trusted root and cursor paths remain isolated.

A missing cursor begins at zero. A cursor from another runtime generation also begins at zero. Legacy pre-generation files containing one unsigned sequence are migrated safely from zero on the next commit. Structurally corrupt files fail closed.

For every valid envelope:

1. sequences already at or below the generation-scoped durable cursor are ignored;
2. an unexpired envelope is passed to the typed handler;
3. the cursor is saved only after the typed handler returns successfully;
4. an expired envelope is not applied, but its sequence is durably skipped;
5. a handler exception is surfaced and leaves the previous cursor unchanged.

This gives at-least-once delivery at the typed handler boundary. A process crash after the handler returns but before the atomic cursor write may replay that event, so callback handlers must remain safe for repeat application.

The legacy handler surface is not uniformly completion-aware. Some handlers, including global-event generation and restart or shutdown scheduling, enqueue asynchronous work and return before the complete gameplay effect finishes. Therefore a cursor commit currently proves successful typed dispatch, not completion of every downstream business effect. Production cutover must either make those handlers awaitable and idempotent or explicitly accept and test that boundary.

## Controlled lifecycle and reconnection

`CommunicationCallbackSubscriberHost` owns exactly one subscriber runner. It exposes `Created`, `Starting`, `Running`, `Stopping`, `Stopped` and `Faulted` states, retains a terminal exception and supports bounded cancellation during process shutdown.

The subscriber opens one stream at a time. Transient gRPC status codes reconnect with bounded exponential delay. A generation change is retried through a fresh runtime-info query. Fatal authorization, invalid-request, replay-cursor and data-loss errors escape to the lifecycle host and become visible as `Faulted`.

A second `Start` or `RunAsync` call fails immediately. Cancellation stops retry and stream processing. The lifecycle host does not silently start SCS or another wire mode.

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
2. Login queries the real runtime generation with its Login certificate;
3. Login opens a generation-bound callback server stream;
4. Master publishes a typed penalty refresh using the separate Master certificate;
5. the runtime routes it to the Login subscriber;
6. the handler applies the envelope exactly once;
7. the test observes the matching cursor commit after handler completion;
8. cancellation closes the stream.

Unique test identities prevent the second wire-mode execution from inheriting retained events from the first while preserving the runtime's real replay behavior.

## Current migration boundary

Production remains on the SCS callback path. Login and World may start the gRPC subscriber only in shadow mode, where validated callbacks advance a dedicated cursor without invoking `CommunicationCallbackEnvelopeDispatcher` or any `CommunicationServiceClient.On...` effect. Real application remains blocked until a replay-complete barrier permits an atomic SCS-to-gRPC inbound cutover, downstream completion semantics are resolved and full real-client behavior is proven.
