# Communication callback production lifecycle

Login and World now contain disabled-by-default ownership for the generation-scoped gRPC callback subscriber. This wiring is intentionally a shadow-observation stage. SCS remains the only callback path allowed to execute gameplay or account effects.

## Activation controls

The production processes read:

```text
NOSGM_COMMUNICATION_GRPC_CALLBACKS_ENABLED
NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED
NOSGM_COMMUNICATION_GRPC_CALLBACKS_STOP_TIMEOUT_MILLISECONDS
```

`NOSGM_COMMUNICATION_GRPC_CALLBACKS_ENABLED` defaults to `false`.

When it is `true`, the process starts a real mTLS callback subscription, validates every envelope, follows runtime-generation replay rules and advances its dedicated durable cursor. The subscriber uses `CommunicationCallbackShadowEnvelopeHandler`, which records callback count and sequence but performs no legacy callback action.

`NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED` also defaults to `false`. Setting it to `true` fails process initialization. Real callback application remains blocked until the runtime can provide an explicit replay barrier and the process can atomically move inbound callback authority from SCS to gRPC.

Boolean values accept only `true` or `false` without surrounding whitespace. The stop timeout defaults to 5,000 milliseconds and must remain between 1,000 and 30,000 milliseconds.

## Login ownership

Login starts its optional shadow subscriber only after:

- Master authentication succeeds;
- optional Gameforge ticket-consumer authentication succeeds;
- database and packet initialization complete;
- every configured regional Login listener starts successfully.

Any later initialization exception stops the subscriber before listeners are torn down. An unhandled Login exception also stops the subscriber before the process is restarted.

## World ownership

The SCS state transport starts the optional World shadow only after Master successfully registers the World and returns its assigned channel ID. The subscriber identity uses the exact registered World GUID, channel ID and World group.

Because authoritative World state still lives in SCS, the callback runtime does not learn that assignment through `ClusterCommunicationService`. Before opening its stream, the World subscriber therefore calls `RegisterCommunicationCallbackShadowWorld` using its World certificate, current runtime generation and SCS-assigned identity. This temporary route exists only in `CommunicationCallbackHub`; it cannot create accounts, attach sessions, assign a channel or modify authoritative cluster state.

When a stream ends, the client attempts `UnregisterCommunicationCallbackShadowWorld`. Runtime restart clears the route automatically. Registration conflict or identity mismatch fails the subscriber closed rather than opening an unrouteable stream.

If subscriber configuration or startup throws, the SCS transport immediately unregisters the authoritative World before surfacing the failure. A World therefore cannot remain registered after a synchronous shadow-lifecycle start failure.

Before normal World unregistration, the lifecycle cancels and disposes the callback subscriber. `ProcessExit` provides a final bounded cleanup path for exits that do not reach explicit unregistration.

## No duplicate effects

The shadow subscriber never constructs `CommunicationCallbackEnvelopeDispatcher` and never invokes `CommunicationServiceClient.On...` handlers. SCS callbacks continue to apply all supported effects, including presence, kicks, lifecycle commands, global events and cache refreshes.

The typed gRPC stream is therefore exercised inside the real Login and World processes without executing an event twice. Its cursor represents successful validation and shadow observation only.

## Publication boundary

Production Master does not yet mirror SCS callback publications into the .NET 10 callback runtime. A shadow subscriber can observe callbacks published by acceptance tests, diagnostics or future guarded mirror work, but a zero observed count does not currently prove that SCS delivered no callbacks.

The next publisher slice must copy typed callback intent into the runtime while SCS remains the sole effect applier. That mirror must use the same semantic event identity and must never invoke a second gameplay handler.

## Per-process configuration

Login and every World process need distinct values for:

```text
NOSGM_COMMUNICATION_GRPC_CLIENT_CERT_PATH
NOSGM_COMMUNICATION_GRPC_CALLER_INSTANCE_ID
NOSGM_COMMUNICATION_GRPC_CALLBACK_CURSOR_PATH
```

The launcher or process supervisor must assign these values separately before enabling shadow mode. Reusing a caller instance ID or cursor between processes fails identity and replay assumptions.

## Next boundary

The next migration steps are a guarded Master publication mirror followed by a server-issued replay-complete barrier. The process can then keep SCS authoritative while gRPC catches up, observe the barrier, atomically switch the inbound gate and guarantee that every callback is applied exactly once by one transport. Until that barrier exists, application mode remains hard-blocked.
