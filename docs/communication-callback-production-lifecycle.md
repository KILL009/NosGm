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

If subscriber configuration or startup throws, the transport immediately unregisters the World again before surfacing the failure. A World therefore cannot remain registered after a failed shadow-lifecycle start.

Before normal World unregistration, the lifecycle cancels and disposes the callback subscriber. `ProcessExit` provides a final bounded cleanup path for exits that do not reach explicit unregistration.

## No duplicate effects

The shadow subscriber never constructs `CommunicationCallbackEnvelopeDispatcher` and never invokes `CommunicationServiceClient.On...` handlers. SCS callbacks continue to apply all supported effects, including presence, kicks, lifecycle commands, global events and cache refreshes.

The typed gRPC stream is therefore exercised inside the real Login and World processes without executing an event twice. Its cursor represents successful validation and shadow observation only.

## Per-process configuration

Login and every World process need distinct values for:

```text
NOSGM_COMMUNICATION_GRPC_CLIENT_CERT_PATH
NOSGM_COMMUNICATION_GRPC_CALLER_INSTANCE_ID
NOSGM_COMMUNICATION_GRPC_CALLBACK_CURSOR_PATH
```

The launcher or process supervisor must assign these values separately before enabling shadow mode. Reusing a caller instance ID or cursor between processes fails identity and replay assumptions.

## Next boundary

The next cutover slice must add a server-issued replay-complete barrier. The process can then keep SCS authoritative while gRPC catches up, observe the barrier, atomically switch the inbound gate and guarantee that every callback is applied exactly once by one transport. Until that barrier exists, application mode remains hard-blocked.
