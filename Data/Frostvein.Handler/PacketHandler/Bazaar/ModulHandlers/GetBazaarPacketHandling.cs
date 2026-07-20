using Frostvein.Configuration;
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
using Frostvein.Handler.World.Bazaar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.Handler.Bazaar
{
    public class GetBazaarPacketHandling : IPacketHandler
    {
        private static readonly KeepAliveClient KeepAliveClient = KeepAliveClient.Instance;
        private static readonly BazaarHttpClient BazaarClient = BazaarHttpClient.Instance;

        public GetBazaarPacketHandling(ClientSession session) => Session = session;

        private ClientSession Session { get; }

        public void GetBazaar(CScalcPacket packet)
        {
            if (packet == null || Session?.Character?.Inventory == null)
            {
                return;
            }

            if (!CanUseBazaar())
            {
                return;
            }

            BazaarItemDTO listing = DAOFactory.BazaarItemDAO.LoadById(packet.BazaarId);
            if (listing == null)
            {
                SendEmptyResult();
                RemoveBazaarCache(packet.BazaarId);
                return;
            }

            ItemInstanceDTO sourceDto = DAOFactory.ItemInstanceDAO.LoadById(listing.ItemInstanceId);
            if (sourceDto == null ||
                listing.SellerId != Session.Character.CharacterId ||
                sourceDto.CharacterId != Session.Character.CharacterId ||
                sourceDto.Type != InventoryType.Bazaar ||
                sourceDto.ItemVNum != listing.ItemInstanceId.Equals(Guid.Empty) ? packet.VNum : sourceDto.ItemVNum)
            {
                SendStateChanged();
                return;
            }

            BazaarRecollectPlan committedPlan;
            lock (Session.Character.Inventory)
            {
                if (!TryBuildPlan(listing, sourceDto, out BazaarRecollectPlan plan,
                        out BazaarRecollectResult planningResult))
                {
                    SendFailure(planningResult, listing, sourceDto);
                    return;
                }

                BazaarRecollectResult result = BazaarRecollectService.Instance.Commit(plan.Commit);
                if (result != BazaarRecollectResult.Success &&
                    result != BazaarRecollectResult.AlreadyCommitted)
                {
                    SendFailure(result, listing, sourceDto);
                    return;
                }

                ApplyPlan(plan);
                RecordTrace(plan);
                committedPlan = plan;
            }

            RemoveBazaarCache(listing.BazaarItemId);
            new RefreshPersonalListPacketHandler(Session)
                .RefreshPersonalBazarList(new CSListPacket());
            Session.SendPacket("rc_reg 1");

            Logger.LogUserEvent("BAZAAR_RECOLLECT_COMMIT", Session.GenerateIdentity(),
                $"OperationId={committedPlan.Commit.OperationId} BazaarId={listing.BazaarItemId} " +
                $"ItemInstanceId={listing.ItemInstanceId} ItemVNum={committedPlan.Commit.ItemVNum} " +
                $"Remaining={committedPlan.Commit.RemainingAmount} Sold={committedPlan.Commit.SoldAmount} " +
                $"Proceeds={committedPlan.Commit.Proceeds}");
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
                    "You need to wait a few seconds before using the bazaar again."));
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
            out BazaarRecollectPlan plan,
            out BazaarRecollectResult result)
        {
            plan = null;
            result = BazaarRecollectResult.StateChanged;

            var source = new ItemInstance(sourceDto);
            if (source.Item == null || source.Amount < 0 || listing.Amount < source.Amount)
            {
                return false;
            }

            short soldAmount = (short)(listing.Amount - source.Amount);
            long gross;
            long goldAfter;
            try
            {
                gross = checked(listing.Price * soldAmount);
                long tax = listing.MedalUsed ? 0 : gross / 10;
                long proceeds = gross - tax;
                goldAfter = checked(Session.Character.Gold + proceeds);

                if (goldAfter > GameConfiguration.MaxGold)
                {
                    result = BazaarRecollectResult.GoldLimit;
                    return false;
                }

                if (!TryBuildInventoryChanges(source,
                        out List<ItemInstance> itemsBefore,
                        out List<ItemInstance> itemsAfter))
                {
                    result = BazaarRecollectResult.NoInventorySpace;
                    return false;
                }

                var commit = new BazaarRecollectDTO
                {
                    OperationId = Guid.NewGuid(),
                    BazaarItemId = listing.BazaarItemId,
                    SellerCharacterId = Session.Character.CharacterId,
                    BazaarItemInstanceId = listing.ItemInstanceId,
                    ItemVNum = source.ItemVNum,
                    ListingAmount = listing.Amount,
                    RemainingAmount = source.Amount,
                    SoldAmount = soldAmount,
                    UnitPrice = listing.Price,
                    Tax = tax,
                    Proceeds = proceeds,
                    GoldBefore = Session.Character.Gold,
                    GoldAfter = goldAfter
                };
                commit.ItemsBefore.AddRange(itemsBefore);
                commit.ItemsAfter.AddRange(itemsAfter);

                plan = new BazaarRecollectPlan
                {
                    Commit = commit,
                    Listing = listing,
                    SourceBefore = source.DeepCopy(),
                    ItemsBefore = itemsBefore,
                    ItemsAfter = itemsAfter
                };
                result = BazaarRecollectResult.Success;
                return true;
            }
            catch (OverflowException)
            {
                result = BazaarRecollectResult.GoldLimit;
                return false;
            }
        }

        private bool TryBuildInventoryChanges(
            ItemInstance source,
            out List<ItemInstance> before,
            out List<ItemInstance> after)
        {
            before = new List<ItemInstance> { source.DeepCopy() };
            after = new List<ItemInstance>();

            var working = Session.Character.Inventory.GetAllItems()
                .Where(item => item != null && item.Id != source.Id)
                .Select(item => item.DeepCopy())
                .ToList();
            var changedBefore = new Dictionary<Guid, ItemInstance>();
            var changedAfter = new Dictionary<Guid, ItemInstance>();

            int remaining = source.Amount;
            InventoryType destinationType = source.Item.Type;
            bool stackable = destinationType == InventoryType.Main ||
                             destinationType == InventoryType.Etc;

            if (!stackable && remaining > 1)
            {
                return false;
            }

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

            if (remaining > 0)
            {
                short? freeSlot = FindFreeSlot(working, destinationType);
                if (!freeSlot.HasValue)
                {
                    return false;
                }

                ItemInstance restored = source.DeepCopy();
                restored.Type = destinationType;
                restored.Slot = freeSlot.Value;
                restored.CharacterId = Session.Character.CharacterId;
                restored.Amount = (short)remaining;
                working.Add(restored);
                changedAfter[restored.Id] = restored.DeepCopy();
            }

            before.AddRange(changedBefore.Values
                .OrderBy(item => item.Type)
                .ThenBy(item => item.Slot));
            after.AddRange(changedAfter.Values
                .OrderBy(item => item.Type)
                .ThenBy(item => item.Slot));
            return true;
        }

        private short? FindFreeSlot(IEnumerable<ItemInstance> items, InventoryType type)
        {
            int capacity = type == InventoryType.Miniland
                ? 50
                : Session.Character.Inventory.BackpackSize();
            for (short slot = 0; slot < capacity; slot++)
            {
                if (items.All(item => item.Type != type || item.Slot != slot))
                {
                    return slot;
                }
            }

            return null;
        }

        private void ApplyPlan(BazaarRecollectPlan plan)
        {
            foreach (ItemInstance before in plan.ItemsBefore)
            {
                Session.Character.Inventory.Remove(before.Id);
            }

            foreach (ItemInstance after in plan.ItemsAfter)
            {
                ItemInstance applied = after.DeepCopy();
                Session.Character.Inventory[applied.Id] = applied;
                string inventoryPacket = applied.GenerateInventoryAdd();
                if (!string.IsNullOrWhiteSpace(inventoryPacket))
                {
                    Session.SendPacket(inventoryPacket);
                }
            }

            Session.Character.Gold = plan.Commit.GoldAfter;
            Session.SendPacket(Session.Character.GenerateGold());
            Session.SendPacket(Session.Character.GenerateSay(
                string.Format(Language.Instance.GetMessageFromKey("REMOVE_FROM_BAZAAR"),
                    plan.Commit.Proceeds), 10));
            Session.SendPacket(UserInterfaceHelper.GenerateBazarRecollect(
                plan.Commit.UnitPrice,
                plan.Commit.SoldAmount,
                plan.Commit.ListingAmount,
                plan.Commit.Tax,
                plan.Commit.Proceeds,
                plan.SourceBefore.Item?.Name ?? "None"));

            Session.Character.BazaarItems.TryRemove(plan.Commit.BazaarItemId, out _);
        }

        private void RecordTrace(BazaarRecollectPlan plan)
        {
            try
            {
                var afterById = plan.ItemsAfter.ToDictionary(item => item.Id, item => item);
                int sequence = 0;
                foreach (ItemInstance before in plan.ItemsBefore)
                {
                    afterById.TryGetValue(before.Id, out ItemInstance after);
                    ItemTraceAction action = before.Id == plan.Commit.BazaarItemInstanceId
                        ? after == null ? ItemTraceAction.Deleted : ItemTraceAction.Transferred
                        : ItemTraceAction.StackChanged;

                    ItemTraceService.Instance.Record(
                        plan.Commit.OperationId,
                        sequence++,
                        action,
                        ItemTraceSource.Bazaar,
                        before,
                        after,
                        Session.Account?.AccountId,
                        Session.Character.CharacterId,
                        Session.Character.Name,
                        "Atomic bazaar listing recollection",
                        new
                        {
                            plan.Commit.BazaarItemId,
                            plan.Commit.RemainingAmount,
                            plan.Commit.SoldAmount,
                            plan.Commit.Proceeds
                        });
                }
            }
            catch (Exception exception)
            {
                Logger.LogUserEventError("BAZAAR_RECOLLECT_TRACE", Session.GenerateIdentity(),
                    $"Unable to record bazaar recollection {plan.Commit.OperationId}.", exception);
            }
        }

        private static void RemoveBazaarCache(long bazaarItemId)
        {
            try
            {
                BazaarClient.DeleteBazaarItem(new DeleteBazaarItemCommand { Id = bazaarItemId });
                BazaarClient.DeleteItemState(new DeleteStateCommand { Id = bazaarItemId });
            }
            catch (Exception exception)
            {
                Logger.Error($"Unable to remove bazaar cache entry {bazaarItemId}.", exception);
            }
        }

        private void SendFailure(
            BazaarRecollectResult result,
            BazaarItemDTO listing,
            ItemInstanceDTO source)
        {
            string name = source == null
                ? "None"
                : new ItemInstance(source).Item?.Name ?? "None";

            switch (result)
            {
                case BazaarRecollectResult.NoInventorySpace:
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                        Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE")));
                    break;

                case BazaarRecollectResult.GoldLimit:
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                        Language.Instance.GetMessageFromKey("MAX_GOLD")));
                    break;

                case BazaarRecollectResult.MissingSchema:
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                        "The bazaar recollection migration is missing. Please contact an administrator."));
                    break;

                default:
                    SendStateChanged();
                    break;
            }

            if (listing != null)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateBazarRecollect(
                    listing.Price, 0, listing.Amount, 0, 0, name));
            }
        }

        private void SendEmptyResult()
        {
            Session.SendPacket(UserInterfaceHelper.GenerateBazarRecollect(
                0, 0, 0, 0, 0, "None"));
        }

        private void SendStateChanged()
        {
            Session.SendPacket(UserInterfaceHelper.GenerateModal(
                Language.Instance.GetMessageFromKey("STATE_CHANGED"), 1));
        }

        private sealed class BazaarRecollectPlan
        {
            public BazaarRecollectDTO Commit { get; set; }

            public BazaarItemDTO Listing { get; set; }

            public ItemInstance SourceBefore { get; set; }

            public List<ItemInstance> ItemsBefore { get; set; }

            public List<ItemInstance> ItemsAfter { get; set; }
        }
    }
}
