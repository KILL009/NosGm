using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.HttpClients;
using Frostvein.GameObject.Modules.Bazaar.Commands;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.Handler.Bazaar
{
    public class BuyBazaarPacketHandling : IPacketHandler
    {
        private static readonly KeepAliveClient KeepAliveClient = KeepAliveClient.Instance;
        private static readonly BazaarHttpClient BazaarClient = BazaarHttpClient.Instance;

        public BuyBazaarPacketHandling(ClientSession session) => Session = session;

        private ClientSession Session { get; }

        public void BuyBazaar(CBuyPacket packet)
        {
            if (packet == null || Session?.Character?.Inventory == null || Session.Account == null)
            {
                return;
            }

            if (!CanUseBazaar())
            {
                return;
            }

            BazaarItemDTO listing = DAOFactory.BazaarItemDAO.LoadById(packet.BazaarId);
            if (listing == null || packet.Amount <= 0 || packet.Price <= 0)
            {
                SendStateChanged();
                return;
            }

            ItemInstanceDTO sourceDto = DAOFactory.ItemInstanceDAO.LoadById(listing.ItemInstanceId);
            CharacterDTO seller = DAOFactory.CharacterDAO.LoadById(listing.SellerId);
            if (sourceDto == null || seller == null ||
                sourceDto.Type != InventoryType.Bazaar ||
                sourceDto.CharacterId != listing.SellerId ||
                sourceDto.ItemVNum != packet.VNum ||
                listing.Price != packet.Price ||
                packet.Amount > sourceDto.Amount ||
                listing.IsPackage && packet.Amount != sourceDto.Amount)
            {
                SendStateChanged();
                return;
            }

            if (listing.SellerId == Session.Character.CharacterId ||
                seller.AccountId == Session.Account.AccountId)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo("You cannot buy your own bazaar item."));
                return;
            }

            long totalPrice;
            try
            {
                totalPrice = checked(packet.Price * packet.Amount);
            }
            catch (OverflowException)
            {
                Logger.LogUserEvent("BAZAAR_BUY_REJECTED", Session.GenerateIdentity(),
                    $"Price overflow. BazaarId={packet.BazaarId} Amount={packet.Amount} UnitPrice={packet.Price}");
                SendStateChanged();
                return;
            }

            BazaarPurchasePlan committedPlan;
            lock (Session.Character.Inventory)
            {
                if (!TryBuildPlan(listing, sourceDto, packet.Amount, totalPrice, out BazaarPurchasePlan plan,
                        out BazaarPurchaseResult planningResult))
                {
                    SendFailure(planningResult);
                    return;
                }

                BazaarPurchaseResult result = BazaarPurchaseService.Instance.Commit(plan.Commit);
                if (result != BazaarPurchaseResult.Success && result != BazaarPurchaseResult.AlreadyCommitted)
                {
                    SendFailure(result);
                    return;
                }

                ApplyPlan(plan, seller.Name);
                RecordTrace(plan);
                committedPlan = plan;
            }

            RefreshBazaarCache(listing);
            NotifySeller(listing.SellerId, committedPlan.SourceBefore, packet.Amount);

            Logger.LogUserEvent("BAZAAR_BUY_COMMIT", Session.GenerateIdentity(),
                $"OperationId={committedPlan.Commit.OperationId} BazaarId={packet.BazaarId} " +
                $"SellerId={listing.SellerId} ItemVNum={sourceDto.ItemVNum} Amount={packet.Amount} " +
                $"UnitPrice={packet.Price} Total={totalPrice}");
        }

        private bool CanUseBazaar()
        {
            if (ServerManager.Instance.InShutdown ||
                Session.Character.InExchangeOrTrade ||
                Session.Character.HasShopOpened)
            {
                return false;
            }

            if (DateTime.Now < Session.Character.BazaarActionTimer.LastBuyAction.AddSeconds(2))
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                    "You need to wait a few seconds before buying an item again."));
                return false;
            }

            if (!KeepAliveClient.IsBazaarOnline())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                    "The bazaar server is offline. Please inform a staff member."));
                return false;
            }

            Session.Character.BazaarActionTimer.LastBuyAction = DateTime.Now;
            return true;
        }

        private bool TryBuildPlan(
            BazaarItemDTO listing,
            ItemInstanceDTO sourceDto,
            short amount,
            long totalPrice,
            out BazaarPurchasePlan plan,
            out BazaarPurchaseResult result)
        {
            plan = null;
            result = BazaarPurchaseResult.StateChanged;

            var source = new ItemInstance(sourceDto);
            if (source.Item == null || source.Amount < amount)
            {
                return false;
            }

            long goldAfter = Session.Character.Gold;
            long goldBankAfter = Session.Character.GoldBank;
            if (goldAfter >= totalPrice)
            {
                goldAfter -= totalPrice;
            }
            else if (goldBankAfter >= totalPrice)
            {
                goldBankAfter -= totalPrice;
            }
            else
            {
                result = BazaarPurchaseResult.NotEnoughGold;
                return false;
            }

            if (!TryBuildInventoryChanges(source, amount, out List<ItemInstance> buyerBefore,
                    out List<ItemInstance> buyerAfter))
            {
                result = BazaarPurchaseResult.NoInventorySpace;
                return false;
            }

            ItemInstance sourceBefore = source.DeepCopy();
            ItemInstance sourceAfter = source.DeepCopy();
            sourceAfter.Amount -= amount;

            var commit = new BazaarPurchaseDTO
            {
                OperationId = Guid.NewGuid(),
                BazaarItemId = listing.BazaarItemId,
                BuyerAccountId = Session.Account.AccountId,
                BuyerCharacterId = Session.Character.CharacterId,
                SellerCharacterId = listing.SellerId,
                BazaarItemInstanceId = listing.ItemInstanceId,
                ItemVNum = source.ItemVNum,
                Amount = amount,
                UnitPrice = listing.Price,
                BazaarAmountBefore = sourceBefore.Amount,
                BazaarAmountAfter = sourceAfter.Amount,
                BuyerGoldBefore = Session.Character.Gold,
                BuyerGoldAfter = goldAfter,
                BuyerGoldBankBefore = Session.Character.GoldBank,
                BuyerGoldBankAfter = goldBankAfter
            };

            commit.BuyerItemsBefore.AddRange(buyerBefore);
            commit.BuyerItemsAfter.AddRange(buyerAfter);

            plan = new BazaarPurchasePlan
            {
                Commit = commit,
                BuyerBefore = buyerBefore,
                BuyerAfter = buyerAfter,
                SourceBefore = sourceBefore,
                SourceAfter = sourceAfter
            };
            result = BazaarPurchaseResult.Success;
            return true;
        }

        private bool TryBuildInventoryChanges(
            ItemInstance source,
            short purchaseAmount,
            out List<ItemInstance> before,
            out List<ItemInstance> after)
        {
            before = new List<ItemInstance>();
            after = new List<ItemInstance>();

            var working = Session.Character.Inventory.GetAllItems()
                .Where(item => item != null)
                .Select(item => item.DeepCopy())
                .ToList();
            var changedBefore = new Dictionary<Guid, ItemInstance>();
            var changedAfter = new Dictionary<Guid, ItemInstance>();

            InventoryType destinationType = source.Item.Type;
            bool stackable = destinationType == InventoryType.Main || destinationType == InventoryType.Etc;
            int remaining = purchaseAmount;

            if (stackable)
            {
                foreach (ItemInstance stack in working
                             .Where(item => item.Type == destinationType &&
                                            item.ItemVNum == source.ItemVNum &&
                                            item.Amount > 0 &&
                                            item.Amount < InventoryConfigrationExtension.MaxItemPerSlot)
                             .OrderBy(item => item.Slot)
                             .ToList())
                {
                    if (!changedBefore.ContainsKey(stack.Id))
                    {
                        changedBefore[stack.Id] = stack.DeepCopy();
                    }

                    int moved = Math.Min(
                        InventoryConfigrationExtension.MaxItemPerSlot - stack.Amount,
                        remaining);
                    stack.Amount += (short)moved;
                    remaining -= moved;
                    changedAfter[stack.Id] = stack.DeepCopy();
                    if (remaining == 0)
                    {
                        break;
                    }
                }
            }

            while (remaining > 0)
            {
                short? freeSlot = FindFreeSlot(working, destinationType);
                if (!freeSlot.HasValue)
                {
                    return false;
                }

                short chunkAmount = stackable
                    ? (short)Math.Min(InventoryConfigrationExtension.MaxItemPerSlot, remaining)
                    : (short)1;
                var destination = source.DeepCopy();
                destination.Id = Guid.NewGuid();
                destination.EquipmentSerialId = Guid.NewGuid();
                destination.CharacterId = Session.Character.CharacterId;
                destination.Type = destinationType;
                destination.Slot = freeSlot.Value;
                destination.Amount = chunkAmount;

                working.Add(destination);
                changedAfter[destination.Id] = destination.DeepCopy();
                remaining -= chunkAmount;
            }

            before.AddRange(changedBefore.Values.OrderBy(item => item.Type).ThenBy(item => item.Slot));
            after.AddRange(changedAfter.Values.OrderBy(item => item.Type).ThenBy(item => item.Slot));
            return true;
        }

        private short? FindFreeSlot(IEnumerable<ItemInstance> items, InventoryType type)
        {
            int capacity = type == InventoryType.Miniland ? 50 : Session.Character.Inventory.BackpackSize();
            for (short slot = 0; slot < capacity; slot++)
            {
                if (items.All(item => item.Type != type || item.Slot != slot))
                {
                    return slot;
                }
            }

            return null;
        }

        private void ApplyPlan(BazaarPurchasePlan plan, string sellerName)
        {
            foreach (ItemInstance before in plan.BuyerBefore)
            {
                Session.Character.Inventory.Remove(before.Id);
            }

            foreach (ItemInstance after in plan.BuyerAfter)
            {
                ItemInstance applied = after.DeepCopy();
                Session.Character.Inventory[applied.Id] = applied;
                string inventoryPacket = applied.GenerateInventoryAdd();
                if (!string.IsNullOrWhiteSpace(inventoryPacket))
                {
                    Session.SendPacket(inventoryPacket);
                }
            }

            Session.Character.Gold = plan.Commit.BuyerGoldAfter;
            Session.Character.GoldBank = plan.Commit.BuyerGoldBankAfter;
            Session.SendPacket(Session.Character.GenerateGold());
            Session.SendPacket($"rc_buy 1 {plan.Commit.ItemVNum} {sellerName} " +
                               $"{plan.Commit.Amount} {plan.Commit.UnitPrice} 0 0 0");
            Session.SendPacket(Session.Character.GenerateSay(
                $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: " +
                $"{plan.SourceBefore.Item.Name} x {plan.Commit.Amount}", 10));
        }

        private void RecordTrace(BazaarPurchasePlan plan)
        {
            try
            {
                int sequence = 0;
                ItemTraceService.Instance.Record(
                    plan.Commit.OperationId,
                    sequence++,
                    ItemTraceAction.StackChanged,
                    ItemTraceSource.Bazaar,
                    plan.SourceBefore,
                    plan.SourceAfter,
                    Session.Account.AccountId,
                    Session.Character.CharacterId,
                    Session.Character.Name,
                    "Atomic bazaar purchase source decrement",
                    new { plan.Commit.BazaarItemId, plan.Commit.Amount, plan.Commit.UnitPrice });

                var beforeById = plan.BuyerBefore.ToDictionary(item => item.Id, item => item);
                foreach (ItemInstance item in plan.BuyerAfter)
                {
                    beforeById.TryGetValue(item.Id, out ItemInstance before);
                    ItemTraceService.Instance.Record(
                        plan.Commit.OperationId,
                        sequence++,
                        before == null ? ItemTraceAction.Created : ItemTraceAction.StackChanged,
                        ItemTraceSource.Bazaar,
                        before,
                        item,
                        Session.Account.AccountId,
                        Session.Character.CharacterId,
                        Session.Character.Name,
                        "Atomic bazaar purchase buyer inventory",
                        new { plan.Commit.BazaarItemId, plan.Commit.Amount, plan.Commit.UnitPrice });
                }
            }
            catch (Exception exception)
            {
                Logger.LogUserEventError("BAZAAR_TRACE", Session.GenerateIdentity(),
                    $"Unable to record bazaar operation {plan.Commit.OperationId}.", exception);
            }
        }

        private static void RefreshBazaarCache(BazaarItemDTO listing)
        {
            try
            {
                BazaarClient.InsertOrUpdateBazaar(new InsertOrUpdateBazaarItemCommand { BazaarItem = listing });
            }
            catch (Exception exception)
            {
                Logger.Error($"Unable to refresh bazaar cache for item {listing.BazaarItemId}.", exception);
            }
        }

        private void NotifySeller(long sellerId, ItemInstance source, short amount)
        {
            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
            {
                DestinationCharacterId = sellerId,
                SourceWorldId = ServerManager.Instance.WorldId,
                Message = StaticPacketHelper.Say(1, sellerId, 12,
                    string.Format(Language.Instance.GetMessageFromKey("BAZAAR_ITEM_SOLD"),
                        amount, source.Item.Name)),
                Type = MessageType.Other
            });
        }

        private void SendFailure(BazaarPurchaseResult result)
        {
            switch (result)
            {
                case BazaarPurchaseResult.NotEnoughGold:
                    Session.SendPacket(Session.Character.GenerateSay(
                        Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                    Session.SendPacket(UserInterfaceHelper.GenerateModal(
                        Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 1));
                    break;

                case BazaarPurchaseResult.NoInventorySpace:
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                    break;

                case BazaarPurchaseResult.MissingSchema:
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                        "The bazaar database migration is missing. Please contact an administrator."));
                    break;

                default:
                    SendStateChanged();
                    break;
            }
        }

        private void SendStateChanged()
        {
            Session.SendPacket(UserInterfaceHelper.GenerateModal(
                Language.Instance.GetMessageFromKey("STATE_CHANGED"), 1));
        }

        private sealed class BazaarPurchasePlan
        {
            public BazaarPurchaseDTO Commit { get; set; }

            public List<ItemInstance> BuyerBefore { get; set; }

            public List<ItemInstance> BuyerAfter { get; set; }

            public ItemInstance SourceBefore { get; set; }

            public ItemInstance SourceAfter { get; set; }
        }
    }
}
