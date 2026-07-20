using Frostvein.Configuration;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Extension.Extension.Packet
{
    public static class InvExt
    {
        public static void CloseExchange(this ClientSession session, ClientSession targetSession)
        {
            if (targetSession?.Character.ExchangeInfo != null)
            {
                targetSession.SendPacket("exc_close 0");
                targetSession.Character.ExchangeInfo = null;
                targetSession.Character.TradeRequests.Clear();
                targetSession.Character.IsExchanging = false;
            }

            if (session?.Character.ExchangeInfo != null)
            {
                session.SendPacket("exc_close 0");
                session.Character.ExchangeInfo = null;
                session.Character.TradeRequests.Clear();
                session.Character.IsExchanging = false;
            }
        }

        /// <summary>
        /// Plans the complete exchange without touching live inventories, persists every
        /// affected item plus both gold balances in one SQL transaction, then applies the
        /// already-committed plan to memory. Locks are always acquired by CharacterId.
        /// </summary>
        public static bool TryCommitExchange(
            this ClientSession sourceSession,
            ClientSession targetSession,
            out TradeCommitResult commitResult)
        {
            commitResult = TradeCommitResult.Error;
            if (sourceSession?.Character?.Inventory == null || targetSession?.Character?.Inventory == null)
            {
                return false;
            }

            var sourceCharacterId = sourceSession.Character.CharacterId;
            var targetCharacterId = targetSession.Character.CharacterId;
            var firstLock = sourceCharacterId < targetCharacterId
                ? (object)sourceSession.Character.Inventory
                : targetSession.Character.Inventory;
            var secondLock = sourceCharacterId < targetCharacterId
                ? (object)targetSession.Character.Inventory
                : sourceSession.Character.Inventory;

            lock (firstLock)
            {
                lock (secondLock)
                {
                    return TryCommitExchangeLocked(sourceSession, targetSession, out commitResult);
                }
            }
        }

        private static bool TryCommitExchangeLocked(
            ClientSession sourceSession,
            ClientSession targetSession,
            out TradeCommitResult commitResult)
        {
            commitResult = TradeCommitResult.Error;
            var source = sourceSession.Character;
            var target = targetSession.Character;
            var sourceInfo = source.ExchangeInfo;
            var targetInfo = target.ExchangeInfo;

            if (sourceInfo == null || targetInfo == null)
            {
                commitResult = TradeCommitResult.AlreadyCommitted;
                return false;
            }

            if (sourceInfo.CommitStarted || targetInfo.CommitStarted)
            {
                commitResult = TradeCommitResult.AlreadyCommitted;
                return false;
            }

            if (!sourceInfo.Validated || !targetInfo.Validated ||
                !sourceInfo.Confirmed || !targetInfo.Confirmed ||
                sourceInfo.TargetCharacterId != target.CharacterId ||
                targetInfo.TargetCharacterId != source.CharacterId ||
                sourceInfo.OperationId == Guid.Empty ||
                sourceInfo.OperationId != targetInfo.OperationId ||
                sourceSession.IsDisposing || targetSession.IsDisposing ||
                source.MapInstanceId != target.MapInstanceId)
            {
                commitResult = TradeCommitResult.Conflict;
                return false;
            }

            sourceInfo.CommitStarted = true;
            targetInfo.CommitStarted = true;
            source.IsExchanging = true;
            target.IsExchanging = true;

            try
            {
                if (!TryBuildPlan(sourceSession, targetSession, sourceInfo, targetInfo, out var plan))
                {
                    commitResult = TradeCommitResult.Conflict;
                    return false;
                }

                commitResult = TradeCommitService.Instance.Commit(plan.Commit);
                if (commitResult != TradeCommitResult.Success &&
                    commitResult != TradeCommitResult.AlreadyCommitted)
                {
                    return false;
                }

                ApplyPlan(sourceSession, targetSession, plan);
                RecordTrace(sourceSession, targetSession, plan);
                Logger.LogUserEvent("TRADE_COMMIT", sourceSession.GenerateIdentity(),
                    $"OperationId={plan.Commit.OperationId} Target={targetSession.GenerateIdentity()} Items={plan.AffectedIds.Count}");
                return true;
            }
            catch (Exception exception)
            {
                Logger.LogUserEventError("TRADE_COMMIT", sourceSession.GenerateIdentity(),
                    $"Atomic trade operation {sourceInfo.OperationId} failed.", exception);
                commitResult = TradeCommitResult.Error;
                return false;
            }
            finally
            {
                if (source.ExchangeInfo != null)
                {
                    source.ExchangeInfo.CommitStarted = false;
                }

                if (target.ExchangeInfo != null)
                {
                    target.ExchangeInfo.CommitStarted = false;
                }

                source.IsExchanging = false;
                target.IsExchanging = false;
            }
        }

        private static bool TryBuildPlan(
            ClientSession sourceSession,
            ClientSession targetSession,
            ExchangeInfo sourceInfo,
            ExchangeInfo targetInfo,
            out TradePlan plan)
        {
            plan = null;
            var source = sourceSession.Character;
            var target = targetSession.Character;

            if (sourceInfo.Gold < 0 || targetInfo.Gold < 0 ||
                sourceInfo.GoldBank < 0 || targetInfo.GoldBank < 0 ||
                sourceInfo.Gold > source.Gold || targetInfo.Gold > target.Gold ||
                sourceInfo.GoldBank > source.GoldBank || targetInfo.GoldBank > target.GoldBank)
            {
                return false;
            }

            var sourceGoldAfter = source.Gold - sourceInfo.Gold + targetInfo.Gold;
            var targetGoldAfter = target.Gold - targetInfo.Gold + sourceInfo.Gold;
            var sourceGoldBankAfter = source.GoldBank - sourceInfo.GoldBank + targetInfo.GoldBank;
            var targetGoldBankAfter = target.GoldBank - targetInfo.GoldBank + sourceInfo.GoldBank;
            if (sourceGoldAfter < 0 || targetGoldAfter < 0 ||
                sourceGoldBankAfter < 0 || targetGoldBankAfter < 0 ||
                sourceGoldAfter > GameConfiguration.MaxGold || targetGoldAfter > GameConfiguration.MaxGold ||
                sourceGoldBankAfter > InventoryConfigrationExtension.MaxGoldBank ||
                targetGoldBankAfter > InventoryConfigrationExtension.MaxGoldBank)
            {
                return false;
            }

            var sourceBefore = CloneInventory(source.Inventory);
            var targetBefore = CloneInventory(target.Inventory);
            var sourceAfter = CloneItems(sourceBefore);
            var targetAfter = CloneItems(targetBefore);

            if (!TryExtractOffers(sourceAfter, sourceInfo.ExchangeList, source.CharacterId, out var sourceChunks) ||
                !TryExtractOffers(targetAfter, targetInfo.ExchangeList, target.CharacterId, out var targetChunks))
            {
                return false;
            }

            if (!TryDistribute(sourceChunks, targetAfter, target.Inventory, target.CharacterId) ||
                !TryDistribute(targetChunks, sourceAfter, source.Inventory, source.CharacterId))
            {
                return false;
            }

            var beforeAll = sourceBefore.Concat(targetBefore).ToDictionary(item => item.Id, item => item);
            var afterAll = sourceAfter.Concat(targetAfter).ToDictionary(item => item.Id, item => item);
            var affectedIds = new HashSet<Guid>();
            foreach (var id in beforeAll.Keys.Union(afterAll.Keys))
            {
                beforeAll.TryGetValue(id, out var before);
                afterAll.TryGetValue(id, out var after);
                if (!SameTradeState(before, after))
                {
                    affectedIds.Add(id);
                }
            }

            var commit = new TradeCommitDTO
            {
                OperationId = sourceInfo.OperationId,
                FirstCharacterId = source.CharacterId,
                SecondCharacterId = target.CharacterId,
                FirstGoldBefore = source.Gold,
                FirstGoldAfter = sourceGoldAfter,
                FirstGoldBankBefore = source.GoldBank,
                FirstGoldBankAfter = sourceGoldBankAfter,
                SecondGoldBefore = target.Gold,
                SecondGoldAfter = targetGoldAfter,
                SecondGoldBankBefore = target.GoldBank,
                SecondGoldBankAfter = targetGoldBankAfter
            };

            foreach (var item in beforeAll.Values.Where(item => affectedIds.Contains(item.Id)))
            {
                commit.BeforeItems.Add(item);
            }

            foreach (var item in afterAll.Values.Where(item => affectedIds.Contains(item.Id)))
            {
                commit.AfterItems.Add(item);
            }

            plan = new TradePlan
            {
                Commit = commit,
                SourceBefore = sourceBefore,
                SourceAfter = sourceAfter,
                TargetBefore = targetBefore,
                TargetAfter = targetAfter,
                AffectedIds = affectedIds
            };
            return true;
        }

        private static List<ItemInstance> CloneInventory(Inventory inventory) =>
            inventory.GetAllItems().Where(item => item != null).Select(item => item.DeepCopy()).ToList();

        private static List<ItemInstance> CloneItems(IEnumerable<ItemInstance> items) =>
            items.Select(item => item.DeepCopy()).ToList();

        private static bool TryExtractOffers(
            List<ItemInstance> sourceItems,
            IEnumerable<ItemInstance> offeredItems,
            long sourceCharacterId,
            out List<TransferChunk> chunks)
        {
            chunks = new List<TransferChunk>();
            var seen = new HashSet<Guid>();
            foreach (var offered in offeredItems ?? Enumerable.Empty<ItemInstance>())
            {
                if (offered == null || offered.Id == Guid.Empty || offered.Amount <= 0 || !seen.Add(offered.Id))
                {
                    return false;
                }

                var live = sourceItems.FirstOrDefault(item => item.Id == offered.Id);
                if (live == null || live.CharacterId != sourceCharacterId ||
                    live.ItemVNum != offered.ItemVNum || live.Type != offered.Type || live.Slot != offered.Slot ||
                    live.Amount < offered.Amount || !CanTrade(live))
                {
                    return false;
                }

                var fullStack = live.Amount == offered.Amount;
                var chunk = live.DeepCopy();
                chunk.Amount = offered.Amount;
                chunks.Add(new TransferChunk { Item = chunk, PreserveId = fullStack });

                if (fullStack)
                {
                    sourceItems.Remove(live);
                }
                else
                {
                    live.Amount -= offered.Amount;
                }
            }

            return true;
        }

        private static bool TryDistribute(
            IEnumerable<TransferChunk> chunks,
            List<ItemInstance> targetItems,
            Inventory targetInventory,
            long targetCharacterId)
        {
            foreach (var chunk in chunks)
            {
                var remaining = chunk.Item.Amount;
                var type = chunk.Item.Type;
                var stackable = chunk.Item.Item != null &&
                                (chunk.Item.Item.Type == InventoryType.Main ||
                                 chunk.Item.Item.Type == InventoryType.Etc);
                var merged = false;

                if (stackable)
                {
                    foreach (var stack in targetItems
                                 .Where(item => item.Type == type && item.ItemVNum == chunk.Item.ItemVNum &&
                                                item.Amount > 0 && item.Amount < InventoryConfigrationExtension.MaxItemPerSlot)
                                 .OrderBy(item => item.Slot).ToList())
                    {
                        var capacity = InventoryConfigrationExtension.MaxItemPerSlot - stack.Amount;
                        var moved = Math.Min(capacity, remaining);
                        stack.Amount += (short)moved;
                        remaining -= (short)moved;
                        merged = true;
                        if (remaining <= 0)
                        {
                            break;
                        }
                    }
                }

                var firstCreated = true;
                while (remaining > 0)
                {
                    var slot = FindFreeSlot(targetItems, targetInventory, type);
                    if (!slot.HasValue)
                    {
                        return false;
                    }

                    var amount = stackable
                        ? (short)Math.Min(InventoryConfigrationExtension.MaxItemPerSlot, remaining)
                        : (short)1;
                    var destination = chunk.Item.DeepCopy();
                    destination.Id = chunk.PreserveId && !merged && firstCreated && amount == chunk.Item.Amount
                        ? chunk.Item.Id
                        : Guid.NewGuid();
                    destination.CharacterId = targetCharacterId;
                    destination.Type = type;
                    destination.Slot = slot.Value;
                    destination.Amount = amount;
                    targetItems.Add(destination);
                    remaining -= amount;
                    firstCreated = false;
                }
            }

            return true;
        }

        private static short? FindFreeSlot(List<ItemInstance> items, Inventory inventory, InventoryType type)
        {
            var capacity = type == InventoryType.Miniland ? 50 : inventory.BackpackSize();
            for (short slot = 0; slot < capacity; slot++)
            {
                if (items.All(item => item.Type != type || item.Slot != slot))
                {
                    return slot;
                }
            }

            return null;
        }

        private static bool CanTrade(ItemInstance item)
        {
            if (item?.Item == null || !item.Item.IsTradable ||
                item.Type == InventoryType.Bazaar || item.Type == InventoryType.FamilyWareHouse)
            {
                return false;
            }

            return !item.IsBound ||
                   (item.Item.Type == InventoryType.Equipment &&
                    (item.Item.ItemType == ItemType.Armor || item.Item.ItemType == ItemType.Weapon));
        }

        private static bool SameTradeState(ItemInstance before, ItemInstance after)
        {
            if (ReferenceEquals(before, after)) return true;
            if (before == null || after == null) return false;
            return before.Id == after.Id && before.CharacterId == after.CharacterId &&
                   before.ItemVNum == after.ItemVNum && before.Amount == after.Amount &&
                   before.Type == after.Type && before.Slot == after.Slot &&
                   before.EquipmentSerialId == after.EquipmentSerialId;
        }

        private static void ApplyPlan(ClientSession sourceSession, ClientSession targetSession, TradePlan plan)
        {
            foreach (var id in plan.AffectedIds)
            {
                sourceSession.Character.Inventory.Remove(id);
                targetSession.Character.Inventory.Remove(id);
            }

            foreach (var item in plan.SourceAfter.Where(item => plan.AffectedIds.Contains(item.Id)))
            {
                sourceSession.Character.Inventory[item.Id] = item.DeepCopy();
            }

            foreach (var item in plan.TargetAfter.Where(item => plan.AffectedIds.Contains(item.Id)))
            {
                targetSession.Character.Inventory[item.Id] = item.DeepCopy();
            }

            sourceSession.Character.Gold = plan.Commit.FirstGoldAfter;
            sourceSession.Character.GoldBank = plan.Commit.FirstGoldBankAfter;
            targetSession.Character.Gold = plan.Commit.SecondGoldAfter;
            targetSession.Character.GoldBank = plan.Commit.SecondGoldBankAfter;

            SendInventoryChanges(sourceSession, plan.SourceBefore, plan.SourceAfter, plan.AffectedIds);
            SendInventoryChanges(targetSession, plan.TargetBefore, plan.TargetAfter, plan.AffectedIds);
            sourceSession.SendPacket(sourceSession.Character.GenerateGold());
            targetSession.SendPacket(targetSession.Character.GenerateGold());
            sourceSession.SendPacket("exc_close 1");
            targetSession.SendPacket("exc_close 1");
            sourceSession.Character.ExchangeInfo = null;
            targetSession.Character.ExchangeInfo = null;
            sourceSession.Character.TradeRequests.Clear();
            targetSession.Character.TradeRequests.Clear();
        }

        private static void SendInventoryChanges(
            ClientSession session,
            IEnumerable<ItemInstance> before,
            IEnumerable<ItemInstance> after,
            HashSet<Guid> affectedIds)
        {
            var removedSlots = new HashSet<string>();
            foreach (var item in before.Where(item => affectedIds.Contains(item.Id)))
            {
                var key = ((int)item.Type) + ":" + item.Slot;
                if (removedSlots.Add(key))
                {
                    session.SendPacket(UserInterfaceHelper.Instance.GenerateInventoryRemove(item.Type, item.Slot));
                }
            }

            foreach (var item in after.Where(item => affectedIds.Contains(item.Id)).OrderBy(item => item.Type).ThenBy(item => item.Slot))
            {
                var packet = item.GenerateInventoryAdd();
                if (!string.IsNullOrWhiteSpace(packet))
                {
                    session.SendPacket(packet);
                }
            }
        }

        private static void RecordTrace(ClientSession sourceSession, ClientSession targetSession, TradePlan plan)
        {
            try
            {
                var before = plan.Commit.BeforeItems.ToDictionary(item => item.Id, item => item);
                var after = plan.Commit.AfterItems.ToDictionary(item => item.Id, item => item);
                var sequence = 0;
                foreach (var id in plan.AffectedIds.OrderBy(value => value))
                {
                    before.TryGetValue(id, out var beforeItem);
                    after.TryGetValue(id, out var afterItem);
                    var action = beforeItem == null
                        ? ItemTraceAction.Created
                        : afterItem == null || beforeItem.CharacterId != afterItem.CharacterId
                            ? ItemTraceAction.Transferred
                            : beforeItem.Amount != afterItem.Amount
                                ? ItemTraceAction.StackChanged
                                : ItemTraceAction.Updated;

                    ItemTraceService.Instance.Record(
                        plan.Commit.OperationId,
                        sequence++,
                        action,
                        ItemTraceSource.Trade,
                        beforeItem,
                        afterItem,
                        sourceSession.Account?.AccountId,
                        sourceSession.Character.CharacterId,
                        sourceSession.Character.Name,
                        "Atomic player trade",
                        new
                        {
                            SourceCharacterId = sourceSession.Character.CharacterId,
                            TargetCharacterId = targetSession.Character.CharacterId
                        });
                }
            }
            catch (Exception exception)
            {
                Logger.Error($"Trade {plan.Commit.OperationId} committed but item trace recording failed.", exception);
            }
        }

        private sealed class TransferChunk
        {
            public ItemInstance Item { get; set; }

            public bool PreserveId { get; set; }
        }

        private sealed class TradePlan
        {
            public TradeCommitDTO Commit { get; set; }

            public List<ItemInstance> SourceBefore { get; set; }

            public List<ItemInstance> SourceAfter { get; set; }

            public List<ItemInstance> TargetBefore { get; set; }

            public List<ItemInstance> TargetAfter { get; set; }

            public HashSet<Guid> AffectedIds { get; set; }
        }

        public static void ChangeSp(this ClientSession Session)
        {
            ItemInstance sp =
                Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
            ItemInstance fairy =
                Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
            if (sp != null)
            {
                if (Session.Character.GetReputationIco() < sp.Item.ReputationMinimum)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LOW_REP"),
                        0));
                    return;
                }

                if (fairy != null && sp.Item.Element != 0 && fairy.Item.Element != sp.Item.Element
                    && fairy.Item.Element != sp.Item.SecondaryElement)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("BAD_FAIRY"),
                        0));
                    return;
                }

                if (new int[] { 4494, 4495, 4496 }.Contains(sp.ItemVNum))
                {
                    if (Session.Character.Timespace == null)
                    {
                        return;
                    }
                    else if (ServerManager.Instance.TimeSpaces.Any(s => s.SpNeeded?[(byte)Session.Character.Class] == sp.ItemVNum))
                    {
                        if (Session.Character.Timespace.SpNeeded?[(byte)Session.Character.Class] != sp.ItemVNum)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                Session.Character.FishingSpotsMapId = 1;
                Session.Character.FishingSpotsMapX = ServerManager.RandomNumber<short>(78, 81);
                Session.Character.FishingSpotsMapY = ServerManager.RandomNumber<short>(114, 118);
                Session.Character.ChargeValue = 0;
                Session.Character.DisableBuffs(BuffType.All);
                Session.Character.EquipmentBCards.AddRange(sp.Item.BCards);
                Session.Character.LastTransform = DateTime.Now;
                Session.Character.UseSp = true;
                Session.Character.Morph = sp.Item.Morph;
                Session.Character.MorphUpgrade = sp.Upgrade;
                Session.Character.MorphUpgrade2 = sp.Design;
                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateCMode());
                Session.SendPacket(Session.Character.GenerateLev());
                Session.CurrentMapInstance?.Broadcast(
                    StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 196),
                    Session.Character.PositionX, Session.Character.PositionY);
                Session.CurrentMapInstance?.Broadcast(
                    UserInterfaceHelper.GenerateGuri(6, 1, Session.Character.CharacterId), Session.Character.PositionX,
                    Session.Character.PositionY);
                Session.SendPacket(Session.Character.GenerateSpPoint());
                Session.Character.LoadSpeed();
                Session.SendPacket(Session.Character.GenerateCond());
                Session.SendPacket(Session.Character.GenerateStat());
                Session.SendPackets(Session.Character.GenerateStatChar());
                CharacterHelper.AddSpecialistWingsBuff(Session);
                Session.Character.SkillsSp = new ThreadSafeSortedList<int, CharacterSkill>();
                Parallel.ForEach(ServerManager.GetAllSkill(), skill =>
                {
                    if (skill.UpgradeType == sp.Item.Morph && skill.SkillType == 1 && sp.SpLevel >= skill.LevelMinimum)
                    {
                        Session.Character.SkillsSp[skill.SkillVNum] = new CharacterSkill
                        {
                            SkillVNum = skill.SkillVNum,
                            CharacterId = Session.Character.CharacterId
                        };
                    }
                });
                Session.SendPacket(Session.Character.GenerateSki());
                Session.SendPackets(Session.Character.GenerateQuicklist());
                Session.Character.LoadPartnerSkills(true);
            }
        }
    }
}
