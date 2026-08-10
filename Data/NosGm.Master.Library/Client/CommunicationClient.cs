using NosGm.Communication.Client;
using NosGm.Core;
using NosGm.Domain;
using NosGm.Master.Library.Data;
using NosGm.Master.Library.Interface;
using System;
using System.Threading.Tasks;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Master.Library.Client
{
    internal class CommunicationClient : ICommunicationClient
    {
        public CommunicationClient()
        {
            CommunicationCallbackTypedEffectHandlerRegistry.Configure(
                () => new CommunicationCallbackEnvelopeDispatcher(
                    CommunicationServiceClient.Instance));
        }

        #region Methods

        public void CharacterConnected(long characterId)
        {
            Observe(
                "CharacterConnected",
                WireV1.CommunicationCallbackKind.CharacterPresence,
                () => CommunicationCallbackSemanticFingerprint
                    .ComputeCharacterPresence(characterId, true));
            Task.Run(() => CommunicationServiceClient.Instance.OnCharacterConnected(characterId));
        }

        public void CharacterDisconnected(long characterId)
        {
            Observe(
                "CharacterDisconnected",
                WireV1.CommunicationCallbackKind.CharacterPresence,
                () => CommunicationCallbackSemanticFingerprint
                    .ComputeCharacterPresence(characterId, false));
            Task.Run(() => CommunicationServiceClient.Instance.OnCharacterDisconnected(characterId));
        }

        public void KickSession(long? accountId, int? sessionId)
        {
            Observe(
                "KickSession",
                WireV1.CommunicationCallbackKind.KickSession,
                () => CommunicationCallbackSemanticFingerprint
                    .ComputeKickSession(accountId, sessionId));
            Task.Run(() => CommunicationServiceClient.Instance.OnKickSession(accountId, sessionId));
        }

        public void Restart(int time = 5)
        {
            Observe(
                "Restart",
                WireV1.CommunicationCallbackKind.Lifecycle,
                () => CommunicationCallbackSemanticFingerprint
                    .ComputeLifecycle(
                        WireV1.CommunicationLifecycleAction.Restart,
                        checked((uint)time)));
            Task.Run(() => CommunicationServiceClient.Instance.OnRestart(time));
        }

        public void RunGlobalEvent(EventType eventType, byte value)
        {
            Observe(
                "RunGlobalEvent",
                WireV1.CommunicationCallbackKind.GlobalEvent,
                () => CommunicationCallbackSemanticFingerprint
                    .ComputeGlobalEvent(
                        CommunicationGlobalEventMapper.ToWire(eventType),
                        value));
            Task.Run(() => CommunicationServiceClient.Instance.OnRunGlobalEvent(eventType, value));
        }

        public void SendMessageToCharacter(SCSCharacterMessage message)
        {
            Task.Run(() => CommunicationServiceClient.Instance.OnSendMessageToCharacter(message));
        }

        public void Shutdown()
        {
            Observe(
                "Shutdown",
                WireV1.CommunicationCallbackKind.Lifecycle,
                () => CommunicationCallbackSemanticFingerprint
                    .ComputeLifecycle(
                        WireV1.CommunicationLifecycleAction.Shutdown,
                        0));
            Task.Run(() => CommunicationServiceClient.Instance.OnShutdown());
        }

        public void UpdateBazaar(long bazaarItemId)
        {
            Observe(
                "UpdateBazaar",
                WireV1.CommunicationCallbackKind.BazaarRefresh,
                () => CommunicationCallbackSemanticFingerprint
                    .ComputeBazaarRefresh(bazaarItemId));
            Task.Run(() => CommunicationServiceClient.Instance.OnUpdateBazaar(bazaarItemId));
        }

        public void UpdateFamily(long familyId, bool changeFaction)
        {
            Observe(
                "UpdateFamily",
                WireV1.CommunicationCallbackKind.FamilyRefresh,
                () => CommunicationCallbackSemanticFingerprint
                    .ComputeFamilyRefresh(familyId, changeFaction));
            Task.Run(() => CommunicationServiceClient.Instance.OnUpdateFamily(familyId, changeFaction));
        }

        public void UpdateRelation(long relationId)
        {
            Observe(
                "UpdateRelation",
                WireV1.CommunicationCallbackKind.RelationRefresh,
                () => CommunicationCallbackSemanticFingerprint
                    .ComputeRelationRefresh(relationId));
            Task.Run(() => CommunicationServiceClient.Instance.OnUpdateRelation(relationId));
        }

        public void UpdateStaticBonus(long characterId)
        {
            Observe(
                "UpdateStaticBonus",
                WireV1.CommunicationCallbackKind.StaticBonusRefresh,
                () => CommunicationCallbackSemanticFingerprint
                    .ComputeStaticBonusRefresh(characterId));
            Task.Run(() => CommunicationServiceClient.Instance.OnUpdateStaticBonus(characterId));
        }

        private static void Observe(
            string operation,
            WireV1.CommunicationCallbackKind kind,
            Func<string> createFingerprint)
        {
            CommunicationCallbackScsObservationLedger ledger =
                CommunicationCallbackScsObservationLedger.Instance;
            if (!ledger.IsWindowActive)
            {
                return;
            }

            try
            {
                ledger.TryRecord(kind, createFingerprint());
            }
            catch (Exception exception)
            {
                Logger.Error(
                    "[CALLBACK_SCS_OBSERVATION_FAILED] Operation=" +
                    operation +
                    " SCS delivery continues.",
                    exception);
            }
        }

        #endregion
    }
}
