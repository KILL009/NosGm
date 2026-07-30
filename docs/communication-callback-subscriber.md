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

The endpoint must be an HTTPS loopback origin. The certificate and cursor paths must be absolute and cannot point to the same file.

## Windows 10 transport

`GRPCWEB` uses binary gRPC-Web over TLS 1.2 with the process certificate selected explicitly. This mode supports the contract's server-streaming response on Windows 10.

`HTTP2` uses native HTTP/2. The same explicit selector is resolved before opening the stream. There is no fallback from one wire mode to another and no fallback to SCS after a gRPC dispatch.

## Durable cursor

The subscriber loads one unsigned 64-bit sequence from its cursor store. The file implementation writes ASCII to a temporary file, flushes it to disk, then atomically replaces the previous cursor.

A corrupt cursor fails closed. A missing cursor begins at zero.

For every envelope:

1. sequences already at or below the durable cursor are ignored;
2. an unexpired envelope is passed to the typed handler;
3. the cursor is saved only after the handler completes successfully;
4. an expired envelope is not applied, but its sequence is durably skipped;
5. a handler exception is surfaced and leaves the previous cursor unchanged.

This gives at-least-once recovery without acknowledging work before it is applied. A process crash after applying a handler but before the atomic cursor write may replay that event, so callback handlers must remain safe for repeat application.

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

The deferred `SCSCharacterMessage` path is not accepted by the typed dispatcher.

## Live acceptance

The isolated acceptance runtime performs the complete loop over both supported wire modes:

1. Login opens a real callback server stream using the Login certificate;
2. Master publishes a typed penalty refresh using the separate Master certificate;
3. the runtime routes it to the Login subscriber;
4. the handler applies the envelope;
5. the test observes the cursor commit after handler completion;
6. cancellation closes the stream.

## Current migration boundary

Production remains on the SCS callback path. This PR provides the client, durable processing semantics and typed dispatcher only. A later coordinated PR must start the subscriber after successful Login/World registration, disable the matching SCS callbacks at the same boundary, and prove full real-client behavior before the communication transport selector can allow gRPC.
