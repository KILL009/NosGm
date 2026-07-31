using System;
using System.Collections.Generic;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackTerminalTypedObservationWindow
    {
        private readonly CommunicationCallbackShadowObservation[]
            _observations;

        internal CommunicationCallbackTerminalTypedObservationWindow(
            string runtimeGenerationId,
            CommunicationCallbackReplayEvidence replayEvidence,
            long observedCallbacks,
            long evictedObservations,
            IReadOnlyList<CommunicationCallbackShadowObservation>
                observations,
            DateTimeOffset endedAt)
        {
            if (!IsCanonicalNonEmptyGuid(runtimeGenerationId) ||
                observedCallbacks < 0 ||
                evictedObservations < 0 ||
                observations == null ||
                endedAt == default(DateTimeOffset) ||
                (replayEvidence != null &&
                 !string.Equals(
                     runtimeGenerationId,
                     replayEvidence.RuntimeGenerationId,
                     StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "The terminal typed callback observation window is malformed.");
            }

            _observations =
                new CommunicationCallbackShadowObservation[
                    observations.Count];
            for (int index = 0; index < observations.Count; index++)
            {
                CommunicationCallbackShadowObservation observation =
                    observations[index] ??
                    throw new InvalidOperationException(
                        "The terminal typed callback observation window contains a null observation.");
                if (!string.Equals(
                        runtimeGenerationId,
                        observation.RuntimeGenerationId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The terminal typed callback observation crosses runtime generations.");
                }
                _observations[index] = observation;
            }

            RuntimeGenerationId = runtimeGenerationId;
            ReplayEvidence = replayEvidence;
            ObservedCallbacks = observedCallbacks;
            EvictedObservations = evictedObservations;
            EndedAt = endedAt.ToUniversalTime();
        }

        public string RuntimeGenerationId { get; }

        public CommunicationCallbackReplayEvidence ReplayEvidence { get; }

        public long ObservedCallbacks { get; }

        public long EvictedObservations { get; }

        public DateTimeOffset EndedAt { get; }

        public IReadOnlyList<CommunicationCallbackShadowObservation>
            GetObservationSnapshot()
        {
            return (CommunicationCallbackShadowObservation[])
                _observations.Clone();
        }

        private static bool IsCanonicalNonEmptyGuid(string value)
        {
            return value != null &&
                   value.Length == 36 &&
                   Guid.TryParseExact(value, "D", out Guid parsed) &&
                   parsed != Guid.Empty &&
                   string.Equals(
                       parsed.ToString("D"),
                       value,
                       StringComparison.Ordinal);
        }
    }

    public static class CommunicationCallbackTerminalObservationContext
    {
        [ThreadStatic]
        private static CommunicationCallbackTerminalTypedObservationWindow
            _currentTypedWindow;

        public static CommunicationCallbackTerminalTypedObservationWindow
            CurrentTypedWindow => _currentTypedWindow;

        internal static void Invoke(
            CommunicationCallbackTerminalTypedObservationWindow typedWindow,
            Action callback)
        {
            if (typedWindow == null)
            {
                throw new ArgumentNullException(nameof(typedWindow));
            }
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            if (_currentTypedWindow != null)
            {
                throw new InvalidOperationException(
                    "A terminal typed callback observation context is already active on this thread.");
            }

            _currentTypedWindow = typedWindow;
            try
            {
                callback();
            }
            finally
            {
                _currentTypedWindow = null;
            }
        }
    }
}
