using NosGm.Communication.Client;
using NosGm.Domain;
using System;
using System.Threading;
using System.Threading.Tasks;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Master.Library.Client
{
    public sealed class CommunicationCallbackEnvelopeDispatcher
        : ICommunicationCallbackEnvelopeHandler
    {
        private readonly CommunicationServiceClient _owner;

        public CommunicationCallbackEnvelopeDispatcher(
            CommunicationServiceClient owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public Task ApplyAsync(
            WireV1.CommunicationCallbackEnvelope envelope,
            CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }
            cancellationToken.ThrowIfCancellationRequested();

            switch (envelope.CallbackCase)
            {
                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .CharacterPresence:
                    if (envelope.CharacterPresence.Connected)
                    {
                        _owner.OnCharacterConnected(
                            envelope.CharacterPresence.CharacterId);
                    }
                    else
                    {
                        _owner.OnCharacterDisconnected(
                            envelope.CharacterPresence.CharacterId);
                    }
                    break;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .KickSession:
                    _owner.OnKickSession(
                        envelope.KickSession.HasAccountId
                            ? envelope.KickSession.AccountId
                            : (long?)null,
                        envelope.KickSession.HasSessionId
                            ? envelope.KickSession.SessionId
                            : (int?)null);
                    break;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .Lifecycle:
                    switch (envelope.Lifecycle.Action)
                    {
                        case WireV1.CommunicationLifecycleAction.Restart:
                            _owner.OnRestart(
                                checked((int)envelope.Lifecycle.DelaySeconds));
                            break;
                        case WireV1.CommunicationLifecycleAction.Shutdown:
                            _owner.OnShutdown();
                            break;
                        default:
                            throw new InvalidOperationException(
                                "The callback lifecycle action is unsupported.");
                    }
                    break;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .GlobalEvent:
                    EventType eventType = CommunicationGlobalEventMapper.ToDomain(
                        envelope.GlobalEvent.EventType);
                    _owner.OnRunGlobalEvent(
                        eventType,
                        checked((byte)envelope.GlobalEvent.Value));
                    break;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .BazaarRefresh:
                    _owner.OnUpdateBazaar(
                        envelope.BazaarRefresh.BazaarItemId);
                    break;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .FamilyRefresh:
                    _owner.OnUpdateFamily(
                        envelope.FamilyRefresh.FamilyId,
                        envelope.FamilyRefresh.ChangeFaction);
                    break;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .PenaltyRefresh:
                    _owner.OnUpdatePenaltyLog(
                        envelope.PenaltyRefresh.PenaltyLogId);
                    break;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .RelationRefresh:
                    _owner.OnUpdateRelation(
                        envelope.RelationRefresh.RelationId);
                    break;

                case WireV1.CommunicationCallbackEnvelope.CallbackOneofCase
                    .StaticBonusRefresh:
                    _owner.OnUpdateStaticBonus(
                        envelope.StaticBonusRefresh.CharacterId);
                    break;

                default:
                    throw new InvalidOperationException(
                        "The callback envelope contains no supported payload.");
            }

            return Task.CompletedTask;
        }
    }
}
