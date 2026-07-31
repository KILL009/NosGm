using NosGm.Communication.Client;
using System;
using System.Collections.Generic;

namespace NosGm.Master.Library.Client
{
    public static class CommunicationCallbackParityEvidenceAdapter
    {
        public static CommunicationCallbackParityWindow CreateTypedWindow(
            string processIdentity,
            string runtimeGenerationId,
            bool isActive,
            CommunicationCallbackReplayEvidence replayEvidence,
            long observedCallbacks,
            long evictedObservations,
            IReadOnlyList<CommunicationCallbackShadowObservation>
                observations)
        {
            if (observations == null)
            {
                throw new ArgumentNullException(nameof(observations));
            }

            var samples = new List<CommunicationCallbackParitySample>();
            for (int index = 0; index < observations.Count; index++)
            {
                CommunicationCallbackShadowObservation observation =
                    observations[index] ??
                    throw new InvalidOperationException(
                        "The typed callback evidence contains a null observation.");
                if (!string.Equals(
                        observation.RuntimeGenerationId,
                        runtimeGenerationId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The typed callback evidence crosses runtime generations.");
                }
                if (observation.Phase !=
                    CommunicationCallbackObservationPhase.Live)
                {
                    continue;
                }

                samples.Add(
                    new CommunicationCallbackParitySample(
                        observation.RuntimeGenerationId,
                        observation.Sequence,
                        observation.Kind,
                        observation.SemanticFingerprint));
            }

            return new CommunicationCallbackParityWindow(
                CommunicationCallbackParitySource.TypedGrpc,
                processIdentity,
                runtimeGenerationId,
                isActive,
                replayEvidence,
                observedCallbacks,
                evictedObservations,
                samples);
        }

        public static CommunicationCallbackParityWindow CreateScsWindow(
            string processIdentity,
            string runtimeGenerationId,
            bool isActive,
            CommunicationCallbackReplayEvidence replayEvidence,
            long observedCallbacks,
            long evictedObservations,
            IReadOnlyList<CommunicationCallbackScsObservation> observations)
        {
            if (observations == null)
            {
                throw new ArgumentNullException(nameof(observations));
            }

            var samples = new List<CommunicationCallbackParitySample>();
            for (int index = 0; index < observations.Count; index++)
            {
                CommunicationCallbackScsObservation observation =
                    observations[index] ??
                    throw new InvalidOperationException(
                        "The SCS callback evidence contains a null observation.");
                if (!string.Equals(
                        observation.ProcessIdentity,
                        processIdentity,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The SCS callback evidence belongs to another process identity.");
                }
                if (!string.Equals(
                        observation.RuntimeGenerationId,
                        runtimeGenerationId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The SCS callback evidence crosses runtime generations.");
                }
                if (observation.Phase !=
                    CommunicationCallbackScsObservationPhase.Live)
                {
                    continue;
                }

                samples.Add(
                    new CommunicationCallbackParitySample(
                        observation.RuntimeGenerationId,
                        observation.LocalOrdinal,
                        observation.Kind,
                        observation.SemanticFingerprint));
            }

            return new CommunicationCallbackParityWindow(
                CommunicationCallbackParitySource.LegacyScs,
                processIdentity,
                runtimeGenerationId,
                isActive,
                replayEvidence,
                observedCallbacks,
                evictedObservations,
                samples);
        }
    }
}
