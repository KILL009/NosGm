using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.Extension.Extension.Packet;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.Packets.Packets.ClientPackets;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class ExcListPacketHandler : IPacketHandler
    {
        #region Instantiation

        public ExcListPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task ExchangeList(ExcListPacket packet)
        {
            if (Session.Account.IsLimited)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                    Language.Instance.GetMessageFromKey("LIMITED_ACCOUNT")));
                return;
            }

            Logger.LogUserEvent("EXC_LIST", Session.GenerateIdentity(), $"Packet string: {packet}");

            var exchange = Session.Character.ExchangeInfo;
            if (exchange == null || exchange.OperationId == Guid.Empty || packet == null ||
                string.IsNullOrWhiteSpace(packet.PacketData))
            {
                return;
            }

            if (exchange.Gold != 0 || exchange.GoldBank != 0 ||
                exchange.ExchangeList.Count > 0 || exchange.Validated || exchange.CommitStarted)
            {
                return;
            }

            var targetSession = ServerManager.Instance.GetSessionByCharacterId(exchange.TargetCharacterId);
            var targetExchange = targetSession?.Character.ExchangeInfo;
            if (targetSession == null || targetExchange == null ||
                targetExchange.OperationId == Guid.Empty ||
                targetExchange.OperationId != exchange.OperationId ||
                targetExchange.TargetCharacterId != Session.Character.CharacterId ||
                Session.Character.MapInstanceId != targetSession.Character.MapInstanceId)
            {
                Session.CloseExchange(targetSession);
                return;
            }

            if (Session.Character.HasShopOpened || targetSession.Character.HasShopOpened)
            {
                Session.CloseExchange(targetSession);
                return;
            }

            var packetSplit = packet.PacketData.Split(new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            if (packetSplit.Length < 2)
            {
                Session.CloseExchange(targetSession);
                return;
            }

            if (!long.TryParse(packetSplit[0], out var gold) ||
                !long.TryParse(packetSplit[1], out var bankGold))
            {
                Session.CloseExchange(targetSession);
                return;
            }

            if (gold < 0 || gold > Session.Character.Gold ||
                bankGold < 0 || bankGold > Session.Character.GoldBank / 1000)
            {
                Session.CloseExchange(targetSession);
                return;
            }

            var type = new byte[10];
            var slot = new short[10];
            var quantity = new short[10];
            var packetList = string.Empty;

            for (int j = 4, i = 0; j < packetSplit.Length && i < 10; j += 3, i++)
            {
                if (!byte.TryParse(packetSplit[j - 2], out type[i]) ||
                    !short.TryParse(packetSplit[j - 1], out slot[i]) ||
                    !short.TryParse(packetSplit[j], out quantity[i]))
                {
                    Session.CloseExchange(targetSession);
                    return;
                }

                var inventoryType = (InventoryType)type[i];
                if (inventoryType == InventoryType.Bazaar ||
                    inventoryType == InventoryType.FamilyWareHouse)
                {
                    Session.CloseExchange(targetSession);
                    return;
                }

                var item = Session.Character.Inventory.LoadBySlotAndType(slot[i], inventoryType);
                if (item == null || quantity[i] <= 0 || item.Amount < quantity[i])
                {
                    Session.CloseExchange(targetSession);
                    return;
                }

                if ((item.ItemVNum >= 7185 && item.ItemVNum <= 7190) ||
                    (item.ItemVNum >= 7412 && item.ItemVNum <= 7414))
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                        "You can't Add Cash in Exchanger."));
                    Session.CloseExchange(targetSession);
                    return;
                }

                var offeredItem = item.DeepCopy();
                var tradable = offeredItem.Item.IsTradable &&
                               (!offeredItem.IsBound ||
                                (offeredItem.Item.Type == InventoryType.Equipment &&
                                 (offeredItem.Item.ItemType == ItemType.Armor ||
                                  offeredItem.Item.ItemType == ItemType.Weapon)));
                if (!tradable)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                        Language.Instance.GetMessageFromKey("ITEM_NOT_TRADABLE")));
                    Session.CloseExchange(targetSession);
                    return;
                }

                offeredItem.Amount = quantity[i];
                exchange.ExchangeList.Add(offeredItem);
                packetList += type[i] != 0
                    ? $"{i}.{type[i]}.{offeredItem.ItemVNum}.{quantity[i]} "
                    : $"{i}.{type[i]}.{offeredItem.ItemVNum}.{offeredItem.Rare}.{offeredItem.Upgrade} ";
            }

            exchange.Gold = gold;
            exchange.GoldBank = bankGold * 1000;
            exchange.Confirmed = false;
            targetExchange.Confirmed = false;
            Session.CurrentMapInstance?.Broadcast(Session,
                $"exc_list 1 {Session.Character.CharacterId} {gold} {bankGold} {packetList}",
                ReceiverType.OnlySomeone, "", exchange.TargetCharacterId);
            exchange.Validated = true;
        }

        #endregion
    }
}
