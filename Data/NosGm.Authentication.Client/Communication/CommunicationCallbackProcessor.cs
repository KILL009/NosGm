using System;
using System.Threading;
using System.Threading.Tasks;
using NosGm.Cluster.Contracts.Communication.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackProcessor
    {
        private readonly ICommunicationCallbackCursorStore _cursorStore;
        private readonly ICommunicationCallbackEnvelopeHandler _handler;
        private ulong _appliedSequence;

        public CommunicationCallbackProcessor(
            ICommunicationCallbackCursorStore cursorStore,
            ICommunicationCallbackEnvelopeHandler handler)
        {
            _cursorStore = cursorStore ??
                throw new ArgumentNullException(nameof(cursorStore));
            _handler = handler ??
                throw new ArgumentNullException(nameof(handler));
            _appliedSequence = _cursorStore.Load();
        }

        public ulong AppliedSequence => _appliedSequence;

        public async Task<bool> ProcessAsync(
            WireV1.CommunicationCallbackEnvelope envelope,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            if (envelope == null || envelope.Sequence == 0)
            {
                throw new InvalidOperationException(
                    "The callback stream returned an invalid envelope.");
            }
            if (envelope.Sequence <= _appliedSequence)
            {
                return false;
            }

            ValidateEnvelope(envelope);
            cancellationToken.ThrowIfCancellationRequested();
            if (envelope.ExpiresAtUnixTimeMs > now.ToUnixTimeMilliseconds())
            {
                await _handler.ApplyAsync(envelope, cancellationToken)
                    .ConfigureAwait(false);
            }

            // Commit only after application. A failed handler leaves the event
            // eligible for replay from the previously durable sequence.
            _cursorStore.Save(envelope.Sequence);
            _appliedSequence = envelope.Sequence;
            return true;
        }

        private static void ValidateEnvelope(
            WireV1.CommunicationCallbackEnvelope envelope)
        {
            if (envelope.Sequence > (ulong)long.MaxValue ||
                !IsCanonicalNonEmptyGuid(envelope.EventId) ||
                envelope.IssuedAtUnixTimeMs <= 0 ||
                envelope.ExpiresAtUnixTimeMs <=
                    envelope.IssuedAtUnixTimeMs ||
                envelope.ExpiresAtUnixTimeMs -
                    envelope.IssuedAtUnixTimeMs >
                    CommunicationCallbackContractLimits.MaxEventTtlSeconds *
                    1000L ||
                !ValidateTarget(envelope.Target) ||
                !ValidateCallbackAndTarget(envelope))
            {
                throw new InvalidOperationException(
                    "The callback stream returned a malformed envelope.");
            }
        }

        private static bool ValidateCallbackAndTarget(
            WireV1.CommunicationCallbackEnvelope envelope)
        {
            WireV1.CommunicationCallbackTargetKind targetKind =
                envelope.Target.Kind;
            switch (envelope.CallbackCase)
            {
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .CharacterPresence:
                    return envelope.CharacterPresence.CharacterId > 0 &&
                           targetKind == WireV1
                               .CommunicationCallbackTargetKind.WorldGroup;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .KickSession:
                    return (envelope.KickSession.HasAccountId ||
                            envelope.KickSession.HasSessionId) &&
                           (!envelope.KickSession.HasAccountId ||
                            envelope.KickSession.AccountId > 0) &&
                           (!envelope.KickSession.HasSessionId ||
                            envelope.KickSession.SessionId > 0) &&
                           targetKind == WireV1
                               .CommunicationCallbackTargetKind.AllWorlds;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .Lifecycle:
                    return Enum.IsDefined(
                               typeof(WireV1.CommunicationLifecycleAction),
                               envelope.Lifecycle.Action) &&
                           envelope.Lifecycle.Action != WireV1
                               .CommunicationLifecycleAction.Unspecified &&
                           envelope.Lifecycle.DelaySeconds <=
                               CommunicationCallbackContractLimits
                                   .MaxRestartDelaySeconds &&
                           (envelope.Lifecycle.Action != WireV1
                                .CommunicationLifecycleAction.Shutdown ||
                            envelope.Lifecycle.DelaySeconds == 0) &&
                           (targetKind == WireV1
                                .CommunicationCallbackTargetKind.AllWorlds ||
                            targetKind == WireV1
                                .CommunicationCallbackTargetKind.WorldGroup);

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .GlobalEvent:
                    return Enum.IsDefined(
                               typeof(WireV1.CommunicationGlobalEventType),
                               envelope.GlobalEvent.EventType) &&
                           envelope.GlobalEvent.EventType != WireV1
                               .CommunicationGlobalEventType.Unspecified &&
                           envelope.GlobalEvent.Value <=
                               CommunicationCallbackContractLimits
                                   .MaxGlobalEventValue &&
                           targetKind == WireV1
                               .CommunicationCallbackTargetKind.AllWorlds;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .BazaarRefresh:
                    return envelope.BazaarRefresh.BazaarItemId > 0 &&
                           targetKind == WireV1
                               .CommunicationCallbackTargetKind.WorldGroup;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .FamilyRefresh:
                    return envelope.FamilyRefresh.FamilyId > 0 &&
                           targetKind == WireV1
                               .CommunicationCallbackTargetKind.WorldGroup;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .PenaltyRefresh:
                    return envelope.PenaltyRefresh.PenaltyLogId > 0 &&
                           targetKind == WireV1
                               .CommunicationCallbackTargetKind.AllNodes;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .RelationRefresh:
                    return envelope.RelationRefresh.RelationId > 0 &&
                           targetKind == WireV1
                               .CommunicationCallbackTargetKind.WorldGroup;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .StaticBonusRefresh:
                    return envelope.StaticBonusRefresh.CharacterId > 0 &&
                           targetKind == WireV1
                               .CommunicationCallbackTargetKind.CharacterId &&
                           envelope.Target.CharacterId ==
                               envelope.StaticBonusRefresh.CharacterId;

                default:
                    return false;
            }
        }

        private static bool ValidateTarget(
            WireV1.CommunicationCallbackTarget target)
        {
            if (target == null ||
                !Enum.IsDefined(
                    typeof(WireV1.CommunicationCallbackTargetKind),
                    target.Kind) ||
                target.Kind == WireV1
                    .CommunicationCallbackTargetKind.Unspecified)
            {
                return false;
            }

            switch (target.Kind)
            {
                case WireV1.CommunicationCallbackTargetKind.AllWorlds:
                case WireV1.CommunicationCallbackTargetKind.AllLoginNodes:
                case WireV1.CommunicationCallbackTargetKind.AllNodes:
                    return HasNoTargetDetails(target);

                case WireV1.CommunicationCallbackTargetKind.WorldGroup:
                    return IsBoundedText(
                               target.WorldGroup,
                               CommunicationCallbackContractLimits
                                   .MaxWorldGroupLength) &&
                           string.IsNullOrEmpty(target.WorldId) &&
                           target.CharacterId == 0;

                case WireV1.CommunicationCallbackTargetKind.WorldId:
                    return string.IsNullOrEmpty(target.WorldGroup) &&
                           IsCanonicalNonEmptyGuid(target.WorldId) &&
                           target.CharacterId == 0;

                case WireV1.CommunicationCallbackTargetKind.CharacterId:
                    return string.IsNullOrEmpty(target.WorldGroup) &&
                           string.IsNullOrEmpty(target.WorldId) &&
                           target.CharacterId > 0;

                default:
                    return false;
            }
        }

        private static bool HasNoTargetDetails(
            WireV1.CommunicationCallbackTarget target)
        {
            return string.IsNullOrEmpty(target.WorldGroup) &&
                   string.IsNullOrEmpty(target.WorldId) &&
                   target.CharacterId == 0;
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

        private static bool IsBoundedText(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Length > maximumLength ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                return false;
            }

            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
