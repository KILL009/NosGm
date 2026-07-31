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

            return ComputePayload(CloneSemanticPayload(envelope));
        }

        public static string ComputeCharacterPresence(
            long characterId,
            bool connected)
        {
            return ComputePayload(
                new WireV1.CommunicationCallbackEnvelope
                {
                    CharacterPresence =
                        new WireV1.CharacterPresenceCallback
                        {
                            CharacterId = characterId,
                            Connected = connected
                        }
                });
        }

        public static string ComputeKickSession(
            long? accountId,
            int? sessionId)
        {
            var callback = new WireV1.KickSessionCallback();
            if (accountId.HasValue)
            {
                callback.AccountId = accountId.Value;
            }
            if (sessionId.HasValue)
            {
                callback.SessionId = sessionId.Value;
            }
            return ComputePayload(
                new WireV1.CommunicationCallbackEnvelope
                {
                    KickSession = callback
                });
        }

        public static string ComputeLifecycle(
            WireV1.CommunicationLifecycleAction action,
            uint delaySeconds)
        {
            return ComputePayload(
                new WireV1.CommunicationCallbackEnvelope
                {
                    Lifecycle = new WireV1.LifecycleCallback
                    {
                        Action = action,
                        DelaySeconds = delaySeconds
                    }
                });
        }

        public static string ComputeGlobalEvent(
            WireV1.CommunicationGlobalEventType eventType,
            uint value)
        {
            return ComputePayload(
                new WireV1.CommunicationCallbackEnvelope
                {
                    GlobalEvent = new WireV1.GlobalEventCallback
                    {
                        EventType = eventType,
                        Value = value
                    }
                });
        }

        public static string ComputeBazaarRefresh(long bazaarItemId)
        {
            return ComputePayload(
                new WireV1.CommunicationCallbackEnvelope
                {
                    BazaarRefresh = new WireV1.BazaarRefreshCallback
                    {
                        BazaarItemId = bazaarItemId
                    }
                });
        }

        public static string ComputeFamilyRefresh(
            long familyId,
            bool changeFaction)
        {
            return ComputePayload(
                new WireV1.CommunicationCallbackEnvelope
                {
                    FamilyRefresh = new WireV1.FamilyRefreshCallback
                    {
                        FamilyId = familyId,
                        ChangeFaction = changeFaction
                    }
                });
        }

        public static string ComputePenaltyRefresh(int penaltyLogId)
        {
            return ComputePayload(
                new WireV1.CommunicationCallbackEnvelope
                {
                    PenaltyRefresh = new WireV1.PenaltyRefreshCallback
                    {
                        PenaltyLogId = penaltyLogId
                    }
                });
        }

        public static string ComputeRelationRefresh(long relationId)
        {
            return ComputePayload(
                new WireV1.CommunicationCallbackEnvelope
                {
                    RelationRefresh = new WireV1.RelationRefreshCallback
                    {
                        RelationId = relationId
                    }
                });
        }

        public static string ComputeStaticBonusRefresh(long characterId)
        {
            return ComputePayload(
                new WireV1.CommunicationCallbackEnvelope
                {
                    StaticBonusRefresh =
                        new WireV1.StaticBonusRefreshCallback
                        {
                            CharacterId = characterId
                        }
                });
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

        private static WireV1.CommunicationCallbackEnvelope
            CloneSemanticPayload(
                WireV1.CommunicationCallbackEnvelope envelope)
        {
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
            return semantic;
        }

        private static string ComputePayload(
            WireV1.CommunicationCallbackEnvelope semantic)
        {
            byte[] payload = semantic.ToByteArray();
            byte[] hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(payload);
            }
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}
