using Game.Configuration.BCards;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject._plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using static Frostvein.Domain.BCardType;
using BCardEvent = Game.Configuration.BCards.BCardEvent;
using IGuriHandler = Frostvein.GameObject._plugins.IGuriHandler;
using Frostvein.GameObject.Plugin.Event;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public static class PluginFacility
    {
        private static IDictionary<NRunType[], Action<ClientSession, NRunPacket>> _nrunHandler;
        private static IDictionary<GuriType[], Action<ClientSession, GuriPacket>> _guriHandler;
        private static IDictionary<CardType, Action<BCardEvent>> _bcardHandler;
        private static readonly HashSet<CardType> MissingBCardWarnings = new HashSet<CardType>();
        private static readonly object MissingBCardWarningsLock = new object();

        public static bool IsInitialized { get; set; }

        public static bool HasBCardHandler(CardType type) =>
            _bcardHandler != null && _bcardHandler.ContainsKey(type);

        public static IReadOnlyCollection<CardType> RegisteredBCardTypes =>
            _bcardHandler?.Keys.ToList() ?? new List<CardType>();

        public static void InitializeAll()
        {
            if (!IsInitialized)
            {
                _nrunHandler = new Dictionary<NRunType[], Action<ClientSession, NRunPacket>>();
                _guriHandler = new Dictionary<GuriType[], Action<ClientSession, GuriPacket>>();
                _bcardHandler = new Dictionary<CardType, Action<BCardEvent>>();
                IsInitialized = true;
            }

            //NrunPlugin.Enable();
            //GuriPlugin.Enable();
            BCardPlugin.Enable();
        }

        public static void AddBCardHandler(IBCardHandler type, Action<BCardEvent> action)
        {
            if (_bcardHandler.ContainsKey(type.ActionType)) return;

            _bcardHandler.Add(type.ActionType, action);
        }

        public static void AddNrunHandler(INrunHandler type, Action<ClientSession, NRunPacket> action)
        {
            if (_nrunHandler.ContainsKey(type.ActionType)) return;

            _nrunHandler.Add(type.ActionType, action);
        }

        public static void AddGuriHandler(IGuriHandler type, Action<ClientSession, GuriPacket> action)
        {
            if (_guriHandler.ContainsKey(type.ActionType)) return;

            _guriHandler.Add(type.ActionType, action);
        }

      
        public static void HandleBCard(BCardEvent evnt)
        {
            if (evnt?.Caster == null || evnt.Target == null || evnt.BCard == null)
            {
                return;
            }

            var cardType = (CardType)evnt.BCard.Type;
            if (_bcardHandler == null || !_bcardHandler.TryGetValue(cardType, out Action<BCardEvent> action))
            {
                lock (MissingBCardWarningsLock)
                {
                    if (MissingBCardWarnings.Add(cardType))
                    {
                        Logger.Warn($"[BCARD_HANDLER_MISSING] Type={(byte)cardType} Name={cardType}");
                    }
                }

                return;
            }

            try
            {
                action(evnt);
            }
            catch (Exception exception)
            {
                Logger.Error($"[BCARD_HANDLER_FAILED] Type={(byte)cardType} Name={cardType}", exception);
            }
        }

        public static void HandleNrun(ClientSession player, NRunPacket packet)
        {
            if (!_nrunHandler.Any(h => h.Key.Contains((NRunType)packet.Runner)))
            {
                // Logger.Log.Debug($"[HANDLER_NOT_FOUND] NRUN_EFFECT : {packet.Runner} ");
                return;
            }

            var action = _nrunHandler.FirstOrDefault(h => h.Key.Contains((NRunType)packet.Runner));
            action.Value(player, packet);
        }

        public static void HandleGuri(ClientSession player, GuriPacket packet)
        {
            if (!_guriHandler.Any(h => h.Key.Contains((GuriType)packet.Type)))
            {
                //Logger.Log.Debug($"[HANDLER_NOT_FOUND] GURI_EFFECT : {packet.Type} ");
                return;
            }

            var action = _guriHandler.FirstOrDefault(h => h.Key.Contains((GuriType)packet.Type));

            action.Value(player, packet);
        }

      
    }
}