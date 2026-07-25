using Game.Configuration.BCards;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Battle;
using NosGm.GameObject._plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using static NosGm.Domain.BCardType;
using BCardEvent = Game.Configuration.BCards.BCardEvent;
using IGuriHandler = NosGm.GameObject._plugins.IGuriHandler;
using NosGm.GameObject.Plugin.Event;

namespace Game.Configuration
{
    public static class PluginFacility
    {
        private static IDictionary<NRunType[], Action<ClientSession, NRunPacket>> _nrunHandler;
        private static IDictionary<GuriType[], Action<ClientSession, GuriPacket>> _guriHandler;
        private static IDictionary<CardType, Action<BCardEvent>> _bcardHandler;
        private static IDictionary<CardType, string> _bcardHandlerNames;
        private static readonly HashSet<string> MissingBCardWarnings = new HashSet<string>();
        private static readonly Dictionary<string, string> MissingBCardSamples = new Dictionary<string, string>();
        private static readonly object MissingBCardWarningsLock = new object();
        private static readonly object InitializationLock = new object();

        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// Returns true when a BCard is handled either by an executable plugin or by
        /// the passive combat/stat calculation pipeline. This keeps startup audits
        /// from reporting calculation-only BCards as missing handlers.
        /// </summary>
        public static bool HasBCardHandler(CardType type) =>
            HasExecutableBCardHandler(type) || BCardExecutionClassifier.IsPassiveCalculationOnly(type);

        public static bool HasExecutableBCardHandler(CardType type) =>
            _bcardHandler != null && _bcardHandler.ContainsKey(type);

        public static IReadOnlyCollection<CardType> RegisteredBCardTypes =>
            _bcardHandler?.Keys.OrderBy(type => (byte)type).ToList() ?? new List<CardType>();

        public static IReadOnlyDictionary<CardType, string> RegisteredBCardHandlers =>
            _bcardHandlerNames == null
                ? new Dictionary<CardType, string>()
                : new Dictionary<CardType, string>(_bcardHandlerNames);

        /// <summary>
        /// Unique executable shapes observed without a handler. Each entry is encoded
        /// as Type:SubType:ExecutionPhase and remains available for runtime diagnostics.
        /// </summary>
        public static IReadOnlyCollection<string> MissingBCardSignatures
        {
            get
            {
                lock (MissingBCardWarningsLock)
                {
                    return MissingBCardWarnings
                        .OrderBy(signature => signature, StringComparer.Ordinal)
                        .ToList();
                }
            }
        }

        /// <summary>
        /// Keeps the first compact runtime sample observed for every missing executable
        /// shape. The sample is intentionally bounded by the unique-signature set.
        /// </summary>
        public static IReadOnlyDictionary<string, string> MissingBCardSampleDetails
        {
            get
            {
                lock (MissingBCardWarningsLock)
                {
                    return new Dictionary<string, string>(MissingBCardSamples);
                }
            }
        }

        public static void InitializeAll()
        {
            lock (InitializationLock)
            {
                if (IsInitialized)
                {
                    return;
                }

                _nrunHandler = new Dictionary<NRunType[], Action<ClientSession, NRunPacket>>();
                _guriHandler = new Dictionary<GuriType[], Action<ClientSession, GuriPacket>>();
                _bcardHandler = new Dictionary<CardType, Action<BCardEvent>>();
                _bcardHandlerNames = new Dictionary<CardType, string>();

                lock (MissingBCardWarningsLock)
                {
                    MissingBCardWarnings.Clear();
                    MissingBCardSamples.Clear();
                }

                try
                {
                    BCardPlugin.Enable();
                    IsInitialized = true;
                }
                catch
                {
                    _nrunHandler = null;
                    _guriHandler = null;
                    _bcardHandler = null;
                    _bcardHandlerNames = null;
                    IsInitialized = false;
                    throw;
                }
            }
        }

        public static void AddBCardHandler(IBCardHandler handler, Action<BCardEvent> action)
        {
            TryAddBCardHandler(handler, action, out _);
        }

        public static bool TryAddBCardHandler(
            IBCardHandler handler,
            Action<BCardEvent> action,
            out string existingHandler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_bcardHandler == null || _bcardHandlerNames == null)
            {
                throw new InvalidOperationException("PluginFacility must be initialized before registering BCard handlers.");
            }

            CardType actionType = handler.ActionType;
            if (_bcardHandler.ContainsKey(actionType))
            {
                _bcardHandlerNames.TryGetValue(actionType, out existingHandler);
                return false;
            }

            _bcardHandler.Add(actionType, action);
            _bcardHandlerNames[actionType] = handler.GetType().FullName ?? handler.GetType().Name;
            existingHandler = null;
            return true;
        }

        public static void AddNrunHandler(INrunHandler type, Action<ClientSession, NRunPacket> action)
        {
            EnsureInitialized();
            if (_nrunHandler.ContainsKey(type.ActionType)) return;

            _nrunHandler.Add(type.ActionType, action);
        }

        public static void AddGuriHandler(IGuriHandler type, Action<ClientSession, GuriPacket> action)
        {
            EnsureInitialized();
            if (_guriHandler.ContainsKey(type.ActionType)) return;

            _guriHandler.Add(type.ActionType, action);
        }

        public static void HandleBCard(BCardEvent evnt)
        {
            if (evnt?.Caster == null || evnt.Target == null || evnt.BCard == null)
            {
                return;
            }

            if (!IsInitialized)
            {
                BCardPipelineMonitor.RecordPreInitializationAttempt();
                EnsureInitialized();
            }

            var cardType = (CardType)evnt.BCard.Type;
            if (!_bcardHandler.TryGetValue(cardType, out Action<BCardEvent> action))
            {
                // Calculation-only BCards are data consumed by DamageHelper/stat loaders. They are not
                // missing executable handlers and must not flood the logs as false positives.
                if (BCardExecutionClassifier.IsPassiveCalculationOnly(cardType, evnt.BCard.SubType))
                {
                    BCardPipelineMonitor.RecordPassiveSkipped();
                    return;
                }

                // Group warnings by executable shape rather than by BCard row. Thousands of rows can
                // legitimately share one unsupported type/subtype/phase combination.
                string warningKey = string.Join(":",
                    (byte)cardType,
                    evnt.BCard.SubType,
                    (byte)evnt.ExecutionPhase);

                bool unique;
                lock (MissingBCardWarningsLock)
                {
                    unique = MissingBCardWarnings.Add(warningKey);
                    if (unique)
                    {
                        MissingBCardSamples[warningKey] = BuildMissingBCardSample(evnt);
                    }
                }

                BCardPipelineMonitor.RecordMissing(unique);
                if (unique)
                {
                    Logger.Warn(
                        $"[BCARD_HANDLER_MISSING] Type={(byte)cardType} Name={cardType} " +
                        $"SubType={evnt.BCard.SubType} Phase={evnt.ExecutionPhase} " +
                        $"SampleSkillVNum={FormatNullable(evnt.BCard.SkillVNum)} " +
                        $"SampleCardId={FormatNullable(evnt.BCard.CardId)} SampleBCardId={evnt.BCard.BCardId} " +
                        $"FirstData={evnt.FirstData} RawFirstData={evnt.BCard.FirstData} " +
                        $"SecondData={evnt.BCard.SecondData} ThirdData={evnt.BCard.ThirdData} " +
                        $"CastType={evnt.BCard.CastType} IsLevelDivided={evnt.BCard.IsLevelDivided} " +
                        $"LevelUpgraded={evnt.LevelUpgraded} CasterLevel={evnt.CasterLevel} " +
                        $"CastId={FormatCastId(evnt.CastContext)} " +
                        $"Caster={DescribeEntity(evnt.Caster)} Target={DescribeEntity(evnt.Target)}");
                }

                return;
            }

            try
            {
                action(evnt);
                BCardPipelineMonitor.RecordExecuted();
            }
            catch (Exception exception)
            {
                BCardPipelineMonitor.RecordHandlerFailure();
                Logger.Error(
                    $"[BCARD_HANDLER_FAILED] Type={(byte)cardType} Name={cardType} " +
                    $"SubType={evnt.BCard.SubType} Phase={evnt.ExecutionPhase} " +
                    $"SkillVNum={FormatNullable(evnt.BCard.SkillVNum)} " +
                    $"CardId={FormatNullable(evnt.BCard.CardId)} BCardId={evnt.BCard.BCardId} " +
                    $"FirstData={evnt.FirstData} SecondData={evnt.BCard.SecondData} " +
                    $"ThirdData={evnt.BCard.ThirdData} CastId={FormatCastId(evnt.CastContext)} " +
                    $"Caster={DescribeEntity(evnt.Caster)} Target={DescribeEntity(evnt.Target)}",
                    exception);
            }
        }

        public static void HandleNrun(ClientSession player, NRunPacket packet)
        {
            EnsureInitialized();
            if (!_nrunHandler.Any(h => h.Key.Contains((NRunType)packet.Runner)))
            {
                return;
            }

            var action = _nrunHandler.FirstOrDefault(h => h.Key.Contains((NRunType)packet.Runner));
            action.Value(player, packet);
        }

        public static void HandleGuri(ClientSession player, GuriPacket packet)
        {
            EnsureInitialized();
            if (!_guriHandler.Any(h => h.Key.Contains((GuriType)packet.Type)))
            {
                return;
            }

            var action = _guriHandler.FirstOrDefault(h => h.Key.Contains((GuriType)packet.Type));
            action.Value(player, packet);
        }

        private static void EnsureInitialized()
        {
            if (IsInitialized)
            {
                return;
            }

            InitializeAll();
        }

        private static string BuildMissingBCardSample(BCardEvent evnt) =>
            $"BCard {evnt.BCard.BCardId} | Skill {FormatNullable(evnt.BCard.SkillVNum)} | " +
            $"Card {FormatNullable(evnt.BCard.CardId)} | Data {evnt.BCard.FirstData}/{evnt.BCard.SecondData}/{evnt.BCard.ThirdData} | " +
            $"EffectiveFirst {evnt.FirstData} | Cast {evnt.BCard.CastType} | LevelDiv {(evnt.BCard.IsLevelDivided ? 1 : 0)}";

        private static string DescribeEntity(BattleEntity entity)
        {
            if (entity?.Character != null)
            {
                return "Character";
            }

            if (entity?.Mate != null)
            {
                return "Mate";
            }

            if (entity?.MapMonster != null)
            {
                return "MapMonster";
            }

            if (entity?.MapNpc != null)
            {
                return "MapNpc";
            }

            return entity == null ? "null" : "BattleEntity";
        }

        private static string FormatCastId(SkillCastContext context) =>
            context == null ? "-" : context.CastId.ToString("N");

        private static string FormatNullable<T>(T? value) where T : struct =>
            value.HasValue ? value.Value.ToString() : "-";
    }
}
