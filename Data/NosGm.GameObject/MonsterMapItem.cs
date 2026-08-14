using NosGm.Configuration;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Extension.Inventory;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Linq;

namespace NosGm.GameObject
{
    public class MonsterMapItem : MapItem
    {
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

        /// <summary>
        /// Delivers an owned monster drop before it is registered in DroppedList.
        /// Returning true means the drop was completely handled and must never be
        /// broadcast as a ground item. Returning false preserves the classic ground
        /// fallback, for example when inventory space is unavailable.
        /// </summary>
        public bool TryDirectAutoLoot(MapInstance mapInstance, bool isQuestDrop = false)
        {
            if (!AutoLootEligible || mapInstance == null || !OwnerId.HasValue || OwnerId.Value <= 0)
            {
                return false;
            }

            ClientSession session = ResolveAutoLootSession(mapInstance, !isQuestDrop);
            if (session?.Character == null
                || !session.HasSelectedCharacter
                || !session.IsConnected
                || session.IsDisposing
                || session.Account?.IsLimited == true
                || session.Character.IsSeal
                || session.CurrentMapInstance == null
                || !ReferenceEquals(session.CurrentMapInstance, mapInstance)
                || mapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance
                && mapInstance.InstanceBag?.EndState != 0)
            {
                return false;
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
                    DirectAutoLootGold(session);
                    return true;
                }

                ItemInstance itemInstance = GetItemInstance();
                if (itemInstance?.Item == null)
                {
                    return false;
                }

                if (itemInstance.Item.ItemType == ItemType.Map)
                {
                    DirectAutoLootMapItem(session, itemInstance);
                    return true;
                }

                return DirectAutoLootInventoryItem(session, mapInstance, itemInstance);
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"[AUTO_LOOT] Direct delivery failed for CharacterId={session.Character.CharacterId} ItemVNum={ItemVNum} TransportId={TransportId}.",
                    exception);
                return false;
            }
        }

        private ClientSession ResolveAutoLootSession(MapInstance mapInstance, bool allowGroupRotation)
        {
            if (!OwnerId.HasValue || OwnerId.Value <= 0)
            {
                return null;
            }

            ClientSession originalOwnerSession =
                ServerManager.Instance.GetSessionByCharacterId(OwnerId.Value);
            if (originalOwnerSession?.Character == null
                || originalOwnerSession.CurrentMapInstance == null
                || !ReferenceEquals(originalOwnerSession.CurrentMapInstance, mapInstance))
            {
                return originalOwnerSession;
            }

            Group group = originalOwnerSession.Character.Group;
            if (allowGroupRotation
                && group?.GroupType == GroupType.Group
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
                        || !ReferenceEquals(candidate.CurrentMapInstance, mapInstance))
                    {
                        continue;
                    }

                    OwnerId = candidate.Character.CharacterId;
                    return candidate;
                }
            }

            return originalOwnerSession;
        }

        private void DirectAutoLootMapItem(ClientSession session, ItemInstance itemInstance)
        {
            short amount = Amount;
            if (amount < 1)
            {
                return;
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
        }

        private bool DirectAutoLootInventoryItem(
            ClientSession session,
            MapInstance mapInstance,
            ItemInstance itemInstance)
        {
            short amount = Amount;
            if (amount <= 0)
            {
                return false;
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
                    return false;
                }

                session.Character.IncrementQuests(QuestType.Collect1, ItemVNum);
                session.Character.IncrementQuests(QuestType.Collect2, ItemVNum);
                session.Character.IncrementQuests(QuestType.Collect4, ItemVNum);

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
                    $"[AutoLootDirect]IIId: {inventoryItem.Id} ItemVNum: {inventoryItem.ItemVNum} Amount: {amount}");
            }

            return true;
        }

        private void DirectAutoLootGold(ClientSession session)
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
                    $"[AutoLootDirect]Gold: {earnedGold}");

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
                    "[AutoLootDirect][MaxGold]");
                session.SendPacket(UserInterfaceHelper.GenerateMsg(
                    Language.Instance.GetMessageFromKey("MAX_GOLD"),
                    0));
            }

            session.SendPacket(session.Character.GenerateGold());
        }

        #endregion
    }
}
