using Google.Protobuf;
using System;
using System.Security.Cryptography;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public enum CommunicationCallbackObservationPhase
    {
        Replay = 1,
        Live = 2
    }

    public interface ICommunicationCallbackStreamObservationContext
    {
        void BeginStream(
            string runtimeGenerationId,
            ulong resumeAfterSequence);

        void CompleteReplay(
            CommunicationCallbackReplayEvidence evidence);

        void EndStream();
    }

    public sealed class CommunicationCallbackShadowObservation
    {
        internal CommunicationCallbackShadowObservation(
            string runtimeGenerationId,
            string eventId,
            ulong sequence,
            WireV1.CommunicationCallbackKind kind,
            CommunicationCallbackObservationPhase phase,
            string semanticFingerprint,
            DateTimeOffset observedAt)
        {
            RuntimeGenerationId = runtimeGenerationId;
            EventId = eventId;
            Sequence = sequence;
            Kind = kind;
            Phase = phase;
            SemanticFingerprint = semanticFingerprint;
            ObservedAt = observedAt;
        }

        public string RuntimeGenerationId { get; }

        public string EventId { get; }

        public ulong Sequence { get; }

        public WireV1.CommunicationCallbackKind Kind { get; }

        public CommunicationCallbackObservationPhase Phase { get; }

        public string SemanticFingerprint { get; }

        public DateTimeOffset ObservedAt { get; }
    }

    public static class CommunicationCallbackSemanticFingerprint
    {
        public static string Compute(
            WireV1.CommunicationCallbackEnvelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            var semantic = new WireV1.CommunicationCallbackEnvelope();
            switch (envelope.CallbackCase)
            {
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .CharacterPresence:
                    semantic.CharacterPresence =
                        envelope.CharacterPresence.Clone();
                    break;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .KickSession:
                    semantic.KickSession = envelope.KickSession.Clone();
                    break;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .Lifecycle:
                    semantic.Lifecycle = envelope.Lifecycle.Clone();
                    break;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .GlobalEvent:
                    semantic.GlobalEvent = envelope.GlobalEvent.Clone();
                    break;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .BazaarRefresh:
                    semantic.BazaarRefresh =
                        envelope.BazaarRefresh.Clone();
                    break;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .FamilyRefresh:
                    semantic.FamilyRefresh =
                        envelope.FamilyRefresh.Clone();
                    break;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .PenaltyRefresh:
                    semantic.PenaltyRefresh =
                        envelope.PenaltyRefresh.Clone();
                    break;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .RelationRefresh:
                    semantic.RelationRefresh =
                        envelope.RelationRefresh.Clone();
                    break;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .StaticBonusRefresh:
                    semantic.StaticBonusRefresh =
                        envelope.StaticBonusRefresh.Clone();
                    break;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .None:
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .ReplayComplete:
                default:
                    throw new InvalidOperationException(
                        "A callback semantic fingerprint requires a gameplay callback payload.");
            }

            byte[] payload = semantic.ToByteArray();
            byte[] hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(payload);
            }
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        public static WireV1.CommunicationCallbackKind ResolveKind(
            WireV1.CommunicationCallbackEnvelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            switch (envelope.CallbackCase)
            {
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .CharacterPresence:
                    return WireV1.CommunicationCallbackKind.CharacterPresence;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .KickSession:
                    return WireV1.CommunicationCallbackKind.KickSession;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .Lifecycle:
                    return WireV1.CommunicationCallbackKind.Lifecycle;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .GlobalEvent:
                    return WireV1.CommunicationCallbackKind.GlobalEvent;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .BazaarRefresh:
                    return WireV1.CommunicationCallbackKind.BazaarRefresh;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .FamilyRefresh:
                    return WireV1.CommunicationCallbackKind.FamilyRefresh;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .PenaltyRefresh:
                    return WireV1.CommunicationCallbackKind.PenaltyRefresh;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .RelationRefresh:
                    return WireV1.CommunicationCallbackKind.RelationRefresh;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .StaticBonusRefresh:
                    return WireV1.CommunicationCallbackKind.StaticBonusRefresh;
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .None:
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .ReplayComplete:
                default:
                    throw new InvalidOperationException(
                        "The callback envelope has no observable gameplay kind.");
            }
        }
    }
}
