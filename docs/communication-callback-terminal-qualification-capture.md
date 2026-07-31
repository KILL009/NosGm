# Terminal PenaltyRefresh parity qualification capture

## Purpose

The callback cutover gate and its bounded qualification ledger now have a
production observation source.

When a typed callback stream ends, Login or World captures the final typed
observation window before the next reconnect clears it. The existing SCS window
is then closed synchronously, both terminal windows are adapted to the same
transport-neutral model, and `PenaltyRefresh` parity evidence is appended to
the process-local qualification runtime.

This remains an observation-only milestone. SCS still applies every callback.

## Synchronous terminal handoff

`CommunicationCallbackShadowEnvelopeHandler.EndStream` now snapshots:

- runtime generation;
- replay-complete evidence;
- cumulative observed and evicted counts;
- the bounded typed observation array;
- terminal time.

The immutable snapshot is exposed only for the duration of the existing
`streamEnded` callback through a thread-local
`CommunicationCallbackTerminalObservationContext`.

The handoff is synchronous and contains no awaits. The context is cleared in a
`finally` block even when the callback fails. Repeated `EndStream` calls cannot
publish the same terminal window twice.

No callback payloads are copied into this handoff. Observations continue to
contain only callback kind, sequence or local ordinal, phase, event identity and
payload-only SHA-256 semantic fingerprints.

## SCS terminal capture

`CommunicationCallbackScsObservationLedger.EndWindow` is already called by
`CommunicationCallbackSubscriberLifecycle` whenever the typed stream exits.
It now:

1. atomically freezes and closes the matching SCS observation window;
2. reads the synchronous typed terminal snapshot;
3. adapts both windows with the same process identity and replay evidence;
4. compares only `PenaltyRefresh` through the existing fail-closed comparator;
5. appends the resulting terminal evidence to the bounded qualification
   runtime.

The next stream cannot clear either ledger until this synchronous capture has
finished.

A missing typed counterpart, malformed adapter evidence or unexpected capture
exception permanently invalidates qualification for that process. It does not
interrupt callback delivery and cannot move authority away from SCS.

## Qualification runtime

`CommunicationCallbackQualificationRuntime` owns the process-local
`PenaltyRefresh` evidence ledger and exposes an immutable status containing:

- bounded capacity;
- appended and evicted evidence counts;
- complete-history and invalidation state;
- whether the current evidence could arm a fresh inactive gate;
- the last terminal evidence;
- the last capture exception, if any.

A valid mismatch is retained as ordinary fail-closed evidence. It breaks the
three-generation parity streak but does not corrupt the ledger. A later three
clean terminal generations can qualify again, provided no evidence has been
evicted and no integrity failure has invalidated the process.

## Lifecycle visibility

The companion extension methods expose the runtime through the existing
lifecycle object:

```csharp
CommunicationCallbackQualificationStatus status =
    CommunicationCallbackSubscriberLifecycle.Instance
        .GetPenaltyRefreshQualificationStatus();

IReadOnlyList<CommunicationCallbackKindParityEvidence> evidence =
    CommunicationCallbackSubscriberLifecycle.Instance
        .GetPenaltyRefreshQualificationEvidenceSnapshot();
```

These methods are diagnostics only. `IsQualified` means that a new inactive
gate could be armed from the retained evidence. It does not mean that typed
effects are active.

## Safety boundary

This slice does not change:

- `CommunicationClient` SCS callback dispatch;
- `CommunicationCallbackShadowEnvelopeHandler.ApplyAsync`, which remains
  observation-only;
- `CommunicationCallbackActivationOptions`, which still rejects production
  callback effect application;
- the callback wire contract or routing scopes;
- `SendMessageToCharacter`, which remains excluded.

`NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED=true` continues to fail
closed. SCS remains the sole effect-applying transport.

## Validation

The compiled .NET 10 self-tests verify:

- synchronous terminal snapshot visibility;
- replay evidence, generation, counts, phases and observations survive closure;
- repeated end calls cannot duplicate a terminal window;
- thread-local context cleanup survives callback failure;
- three captured terminal parity windows qualify the inactive gate;
- a newer valid mismatch breaks qualification without corrupting the ledger.

The static guard proves that SCS closure consumes the typed terminal context,
uses the transport-neutral adapters, targets only `PenaltyRefresh`, and leaves
production effect activation blocked. The complete Windows .NET Framework build
remains the authority for the Master lifecycle and SCS integration.

## Next boundary

The next slice will add an explicit operator-controlled arming request and a
fresh-generation activation handshake. Even then, the first activation will be
limited to `PenaltyRefresh`, disabled by default and protected by terminal
rollback to SCS.
