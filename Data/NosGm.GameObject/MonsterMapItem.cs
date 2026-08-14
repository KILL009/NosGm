using NosGm.Configuration;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Extension.Inventory;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace NosGm.GameObject
{
    public class MonsterMapItem : MapItem
    {
        #region Members

        private const int AutoLootInitialDelayMilliseconds = 10;
        private const int AutoLootLookupRetryMilliseconds = 10;
        private const int AutoLootMaximumLookupAttempts = 20;

        private static readonly ConcurrentQueue<MonsterMapItem> AutoLootQueue =
            new ConcurrentQueue<MonsterMapItem>();

        private static int _autoLootWorkerScheduled;
        private int _autoLootLookupAttempts;
        private int _autoLootRecipientResolved;

        #endregion

        #region Instantiation

        public MonsterMapItem(short x, short y, short itemVNum, int amount = 1, long ownerId = -1) : base(x, y)
        {
            ItemVNum = itemVNum;
            if (amount < 1000)
            {
                Amount = (short)amount;
            }

            GoldAmount = amount;
            OwnerId = ownerId;
            AutoLootEligible = WorldPolicyConfiguration.EnableAutoLoot
                               && ownerId > 0
                               && itemVNum != 1000;

            if (AutoLootEligible)
            {
                QueueAutoLoot(this);
            }
        }

        #endregion

        #region Properties

        public bool AutoLootEligible { get; }

        public sealed override short Amount { get; set; }

        public int GoldAmount { get; }

        public sealed override short ItemVNum { get; set; }

        public long? OwnerId { get; private set; }

        #endregion

        #region Methods

        public override ItemInstance GetItemInstance()
        {
            if (_itemInstance == null && OwnerId != null)
            {
                _itemInstance = Inventory.InstantiateItemInstance(ItemVNum, OwnerId.Value, Amount);
            }

            return _itemInstance;
        }

        public void Rarify(ClientSession session)
        {
            var instance = GetItemInstance();
            if (instance?.Item?.Type == InventoryType.Equipment &&
                (instance?.Item?.ItemType == ItemType.Weapon || instance?.Item?.ItemType == ItemType.Armor))
            {
                instance?.RarifyItem(session, RarifyMode.Drop, RarifyProtection.None);
            }
        }

        private static void QueueAutoLoot(MonsterMapItem item)
        {
            AutoLootQueue.Enqueue(item);
            ScheduleAutoLootWorker();
        }

        private static void ScheduleAutoLootWorker()
        {
            if (Interlocked.CompareExchange(ref _autoLootWorkerScheduled, 1, 0) != 0)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ => DrainAutoLootQueue());
        }

        private static void DrainAutoLootQueue()
        {
            try
            {
                Thread.Sleep(AutoLootInitialDelayMilliseconds);

                while (!AutoLootQueue.IsEmpty)
                {
                    int batchSize = Math.Max(1, AutoLootQueue.Count);
                    bool hasDeferredItems = false;

                    for (int index = 0; index < batchSize; index++)
                    {
                        if (!AutoLootQueue.TryDequeue(out MonsterMapItem item) || item == null)
                        {
                            break;
                        }

                        if (item.TryAutoLoot())
                        {
                            continue;
                        }

                        item._autoLootLookupAttempts++;
                        if (item._autoLootLookupAttempts < AutoLootMaximumLookupAttempts
                            && item.CreatedDate.AddSeconds(1) > DateTime.Now)
                        {
                            AutoLootQueue.Enqueue(item);
                            hasDeferredItems = true;
                        }
                    }

                    if (hasDeferredItems)
                    {
                        Thread.Sleep(AutoLootLookupRetryMilliseconds);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error("[AUTO_LOOT] Monster drop worker failed.", exception);
            }
            finally
            {
                Interlocked.Exchange(ref _autoLootWorkerScheduled, 0);
                if (!AutoLootQueue.IsEmpty)
                {
                    ScheduleAutoLootWorker();
                }
            }
        }

        private ClientSession ResolveAutoLootSession()
        {
            if (!OwnerId.HasValue || OwnerId.Value <= 0)
            {
                return null;
            }

            ClientSession originalOwnerSession =
                ServerManager.Instance.GetSessionByCharacterId(OwnerId.Value);

            if (originalOwnerSession?.Character != null
                && Volatile.Read(ref _autoLootRecipientResolved) == 0
                && Interlocked.CompareExchange(ref _autoLootRecipientResolved, 1, 0) == 0)
            {
                Group group = originalOwnerSession.Character.Group;
                if (group?.GroupType == GroupType.Group
                    && group.SharingMode == (byte)GroupSharingType.ByOrder)
                {
                    int attempts = Math.Max(1, group.SessionCount);
                    for (int index = 0; index < attempts; index++)
                    {
                        long? candidateId =
                            group.GetNextOrderedCharacterId(originalOwnerSession.Character);
                        if (!candidateId.HasValue || candidateId.Value <= 0)
                        {
                            break;
                        }

                        ClientSession candidate =
                            ServerManager.Instance.GetSessionByCharacterId(candidateId.Value);
                        if (candidate?.Character == null
                            || !candidate.HasSelectedCharacter
                            || !candidate.IsConnected
                            || candidate.IsDisposing
                            || candidate.Account?.IsLimited == true
                            || candidate.Character.IsSeal
                            || candidate.CurrentMapInstance == null
                            || !ReferenceEquals(
                                candidate.CurrentMapInstance,
                                originalOwnerSession.CurrentMapInstance))
                        {
                            continue;
                        }

                        OwnerId = candidate.Character.CharacterId;
                        break;
                    }
                }
            }

            return OwnerId.HasValue
                ? ServerManager.Instance.GetSessionByCharacterId(OwnerId.Value)
                : originalOwnerSession;
        }

        private bool TryAutoLoot()
        {
            if (!AutoLootEligible || !OwnerId.HasValue || OwnerId.Value <= 0)
            {
                return true;
            }

            ClientSession session = ResolveAutoLootSession();
            if (session?.Character == null
                || !session.HasSelectedCharacter
                || !session.IsConnected
                || session.IsDisposing
                || session.Account?.IsLimited == true
                || session.CurrentMapInstance == null)
            {
                return true;
            }

            MapInstance mapInstance = session.CurrentMapInstance;
            lock (mapInstance.DroppedList)
            {
                if (!mapInstance.DroppedList.ContainsKey(TransportId))
                {
                    return CreatedDate.AddMilliseconds(250) <= DateTime.Now;
                }

                MapItem currentItem = mapInstance.DroppedList[TransportId];
                if (!ReferenceEquals(currentItem, this))
                {
                    return true;
                }

                if (session.Character.IsSeal
                    || mapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance
                    && mapInstance.InstanceBag?.EndState != 0)
                {
                    return true;
                }

                try
                {
                    Rarify(null);

                    if (ItemVNum == 1097
                        && mapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance
                        && session.Character.Timespace?.InstanceBag != null)
                    {
                        session.Character.Timespace.InstanceBag.Point += new Random().Next(3, 7);
                        session.SendPacket(session.Character.Timespace.InstanceBag.GenerateScore());
                    }

                    if (ItemVNum == 1046)
                    {
                        return AutoLootGold(session, mapInstance);
                    }

                    ItemInstance itemInstance = GetItemInstance();
                    if (itemInstance?.Item == null)
                    {
                        return true;
                    }

                    if (itemInstance.Item.ItemType == ItemType.Map)
                    {
                        return AutoLootMapItem(session, mapInstance, itemInstance);
                    }

                    return AutoLootInventoryItem(session, mapInstance, itemInstance);
                }
                catch (Exception exception)
                {
                    Logger.Error(
                        $"[AUTO_LOOT] Failed for CharacterId={session.Character.CharacterId} ItemVNum={ItemVNum} TransportId={TransportId}.",
                        exception);
                    return true;
                }
            }
        }

        private bool AutoLootMapItem(
            ClientSession session,
            MapInstance mapInstance,
            ItemInstance itemInstance)
        {
            short amount = Amount;
            if (amount < 1)
            {
                return true;
            }

            session.Character.IncrementQuests(QuestType.Collect1, ItemVNum);
            session.Character.IncrementQuests(QuestType.Collect2, ItemVNum);
            session.Character.IncrementQuests(QuestType.Collect4, ItemVNum);

            if (itemInstance.Item.Effect == 71)
            {
                session.Character.SpPoint += itemInstance.Item.EffectValue;
                if (session.Character.SpPoint > 10000)
                {
                    session.Character.SpPoint = 10000;
                }

                session.SendPacket(UserInterfaceHelper.GenerateMsg(
                    string.Format(
                        Language.Instance.GetMessageFromKey("SP_POINTSADDED"),
                        itemInstance.Item.EffectValue),
                    0));
                session.SendPacket(session.Character.GenerateSpPoint());
            }

            if ((ItemVNum == 1086 || ItemVNum == 1087)
                && ServerManager.Instance.FlowerQuestId != null)
            {
                session.Character.AddQuest((long)ServerManager.Instance.FlowerQuestId);
            }

            if (ItemVNum == 1508)
            {
                int rnd = ServerManager.RandomNumber(0, 10);
                switch (rnd)
                {
                    case 10:
                    case 9:
                    case 8:
                        session.SendPacket("msg 4 Sadly, there was nothing in the Box.");
                        break;
                    case 7:
                        session.Character.MapBossReward(2282, 50);
                        break;
                    default:
                        session.Character.MapBossReward(1030, 50);
                        break;
                }
            }

            RemoveFromGround(session, mapInstance);
            return true;
        }

        private bool AutoLootInventoryItem(
            ClientSession session,
            MapInstance mapInstance,
            ItemInstance itemInstance)
        {
            short amount = Amount;
            if (amount <= 0)
            {
                return true;
            }

            lock (session.Character.Inventory)
            {
                ItemInstance inventoryItem = session.Character.Inventory
                    .AddToInventory(itemInstance)
                    .FirstOrDefault();

                if (inventoryItem == null)
                {
                    session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"),
                        0));
                    return true;
                }

                session.Character.IncrementQuests(QuestType.Collect1, ItemVNum);
                session.Character.IncrementQuests(QuestType.Collect2, ItemVNum);
                session.Character.IncrementQuests(QuestType.Collect4, ItemVNum);

                RemoveFromGround(session, mapInstance);

                session.SendPacket(session.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {inventoryItem.Item.Name} x{amount}",
                    12));

                if (mapInstance.MapInstanceType == MapInstanceType.LodInstance)
                {
                    mapInstance.Broadcast(session.Character.GenerateSay(
                        $"{string.Format(Language.Instance.GetMessageFromKey("ITEM_ACQUIRED_LOD"), session.Character.Name)}: {inventoryItem.Item.Name} x {amount}",
                        10));
                }

                Logger.LogUserEvent(
                    "CHARACTER_ITEM_GET",
                    session.GenerateIdentity(),
                    $"[AutoLoot]IIId: {inventoryItem.Id} ItemVNum: {inventoryItem.ItemVNum} Amount: {amount}");
            }

            return true;
        }

        private bool AutoLootGold(ClientSession session, MapInstance mapInstance)
        {
            long maxGold = GameConfiguration.MaxGold;
            double multiplier = 1 + session.Character.GetBuff(
                                    BCardType.CardType.Item,
                                    (byte)AdditionalTypes.Item.IncreaseEarnedGold)[0] / 100D;
            multiplier +=
                (session.Character.ShellEffectMain.FirstOrDefault(effect =>
                     effect.Effect == (byte)ShellWeaponEffectType.GainMoreGold)?.Value ?? 0) / 100D;

            if (session.Character.Gold + GoldAmount * multiplier <= maxGold)
            {
                int earnedGold = (int)(GoldAmount * multiplier);
                session.Character.Gold += earnedGold;

                Logger.LogUserEvent(
                    "CHARACTER_ITEM_GET",
                    session.GenerateIdentity(),
                    $"[AutoLoot]Gold: {earnedGold}");

                ItemInstance goldItem = GetItemInstance();
                string goldName = goldItem?.Item?.Name ?? "Gold";
                session.SendPacket(session.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {goldName} x{GoldAmount}{(multiplier > 1 ? $" + {earnedGold - GoldAmount}" : string.Empty)}",
                    12));
            }
            else
            {
                session.Character.Gold = maxGold;
                Logger.LogUserEvent(
                    "CHARACTER_ITEM_GET",
                    session.GenerateIdentity(),
                    "[AutoLoot][MaxGold]");
                session.SendPacket(UserInterfaceHelper.GenerateMsg(
                    Language.Instance.GetMessageFromKey("MAX_GOLD"),
                    0));
            }

            session.SendPacket(session.Character.GenerateGold());
            RemoveFromGround(session, mapInstance);
            return true;
        }

        private void RemoveFromGround(ClientSession session, MapInstance mapInstance)
        {
            mapInstance.DroppedList.Remove(TransportId);
            mapInstance.Broadcast(StaticPacketHelper.GenerateGet(
                1,
                (int)session.Character.CharacterId,
                TransportId));
        }

        #endregion
    }
}
