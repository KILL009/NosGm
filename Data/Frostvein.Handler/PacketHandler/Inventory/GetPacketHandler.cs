using Frostvein.Configuration;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class GetPacketHandler : IPacketHandler
    {
        #region Instantiation

        public GetPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void GetItem(GetPacket getPacket)
        {
            if (Session.Account.IsLimited)
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("LIMITED_ACCOUNT")));
                return;
            }

            if (getPacket == null || Session.Character.LastSkillUse.AddSeconds(1) > DateTime.Now
                                  || Session.Character.IsVehicled
                                  && Session.CurrentMapInstance?.MapInstanceType != MapInstanceType.EventGameInstance
                                  || !Session.HasCurrentMapInstance
                                  || Session.Character.IsSeal
                                  || Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance &&
                                  Session.CurrentMapInstance.InstanceBag.EndState != 0)
            {
                return;
            }

            if (getPacket.TransportId < 100000)
            {
                var button = Session.CurrentMapInstance.Buttons.Find(s => s.MapButtonId == getPacket.TransportId);
                if (button != null)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateDelay(2000, 1, $"#git^{button.MapButtonId}"));
                }
            }
            else
            {
                lock (Session.CurrentMapInstance.DroppedList)
                {
                    if (!Session.CurrentMapInstance.DroppedList.ContainsKey(getPacket.TransportId))
                    {
                        return;
                    }

                    var mapItem = Session.CurrentMapInstance.DroppedList[getPacket.TransportId];

                    if (mapItem != null)
                    {
                        if (mapItem.ItemVNum == 1000)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateDelay(2000, 1, $"#Br^{getPacket.TransportId}^{mapItem.PositionX}^{mapItem.PositionY}"));
                            mapItem.CanSpawn = true;
                            return;
                        }
                        var canpick = false;
                        switch (getPacket.PickerType)
                        {
                            case 1:
                                canpick = Session.Character.IsInRange(mapItem.PositionX, mapItem.PositionY, 8);
                                break;

                            case 2:
                                var mate = Session.Character.Mates.Find(s =>
                                    s.MateTransportId == getPacket.PickerId && s.CanPickUp);
                                if (mate != null)
                                {
                                    canpick = mate.IsInRange(mapItem.PositionX, mapItem.PositionY, 8);
                                }

                                break;
                        }

                        if (canpick && Session.HasCurrentMapInstance)
                        {
                            if (mapItem is MonsterMapItem item)
                            {
                                var monsterMapItem = item;
                                if (Session.CurrentMapInstance.MapInstanceType != MapInstanceType.LodInstance
                                    && monsterMapItem.OwnerId.HasValue && monsterMapItem.OwnerId.Value != -1)
                                {
                                    var group = ServerManager.Instance.Groups.Find(g =>
                                        g.IsMemberOfGroup(monsterMapItem.OwnerId.Value)
                                        && g.IsMemberOfGroup(Session.Character.CharacterId));
                                    if (item.CreatedDate.AddSeconds(30) > DateTime.Now
                                        && !(monsterMapItem.OwnerId == Session.Character.CharacterId
                                             || group?.SharingMode == (byte)GroupSharingType.Everyone))
                                    {
                                        Session.SendPacket(
                                            Session.Character.GenerateSay(
                                                Language.Instance.GetMessageFromKey("NOT_YOUR_ITEM"), 10));
                                        return;
                                    }
                                }

                                // initialize and rarify
                                item.Rarify(null);
                            }

                            if (mapItem.ItemVNum == 1097)
                            {
                                if (Session.CurrentMapInstance != null)
                                {
                                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance)
                                    {
                                        Session.Character.Timespace.InstanceBag.Point += new Random().Next(3, 7);
                                        Session.SendPacket(Session.Character.Timespace.InstanceBag.GenerateScore());
                                    }
                                }
                            }

                            if (mapItem.ItemVNum == 1000)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateDelay(2000, 1, $"#Br^{getPacket.TransportId}^{mapItem.PositionX}^{mapItem.PositionY}"));
                                mapItem.CanSpawn = true;
                                return;
                            }

                            if (mapItem.ItemVNum != 1046)
                            {
                                var mapItemInstance = mapItem.GetItemInstance();

                                if (mapItemInstance?.Item == null)
                                {
                                    return;
                                }

                                if (mapItemInstance.Item.ItemType == ItemType.Map)
                                {
                                    if (mapItem is MonsterMapItem)
                                    {
                                        Session.Character.IncrementQuests(QuestType.Collect1, mapItem.ItemVNum);
                                        Session.Character.IncrementQuests(QuestType.Collect2, mapItem.ItemVNum);
                                        Session.Character.IncrementQuests(QuestType.Collect4, mapItem.ItemVNum);
                                    }

                                    short amount = mapItem.Amount;
                                    if (amount < 1) // Spam X with another player could lead to dupe
                                    {
                                        return;
                                    }

                                    if (mapItemInstance.Item.Effect == 71)
                                    {
                                        Session.Character.SpPoint += mapItem.GetItemInstance().Item.EffectValue;
                                        if (Session.Character.SpPoint > 10000)
                                        {
                                            Session.Character.SpPoint = 10000;
                                        }

                                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                            string.Format(Language.Instance.GetMessageFromKey("SP_POINTSADDED"),
                                                mapItem.GetItemInstance().Item.EffectValue), 0));
                                        Session.SendPacket(Session.Character.GenerateSpPoint());
                                    }

                                    #region Flower Quest

                                    if (mapItem.ItemVNum == 1086 && ServerManager.Instance.FlowerQuestId != null)
                                    {
                                        Session.Character.AddQuest((long)ServerManager.Instance.FlowerQuestId);
                                    }

                                    #endregion

                                    #region Flower Quest 2

                                    if (mapItem.ItemVNum == 1087 && ServerManager.Instance.FlowerQuestId != null)
                                    {
                                        Session.Character.AddQuest((long)ServerManager.Instance.FlowerQuestId);
                                    }

                                    #endregion

                                    if (mapItem.ItemVNum == 1508)
                                    {
                                        int rnd = ServerManager.RandomNumber(0, 10);
                                        switch (rnd)
                                        {
                                            case 10: { Session.SendPacket("msg 4 Sadly, there was nothing in the Box."); } break;
                                            case 9: { Session.SendPacket("msg 4 Sadly, there was nothing in the Box."); } break;
                                            case 8: { Session.SendPacket("msg 4 Sadly, there was nothing in the Box."); } break;
                                            case 7: { Session.Character.MapBossReward(2282, 50); } break;
                                            case 6: { Session.Character.MapBossReward(1030, 50); } break;
                                            case 5: { Session.Character.MapBossReward(1030, 50); } break;
                                            case 4: { Session.Character.MapBossReward(1030, 50); } break;
                                            case 3: { Session.Character.MapBossReward(1030, 50); } break;
                                            case 2: { Session.Character.MapBossReward(1030, 50); } break;
                                            case 1: { Session.Character.MapBossReward(1030, 50); } break;
                                            case 0: { Session.Character.MapBossReward(1030, 50); } break;
                                        }
                                    }

                                    Session.CurrentMapInstance.DroppedList.Remove(getPacket.TransportId);

                                    Session.CurrentMapInstance?.Broadcast(
                                        StaticPacketHelper.GenerateGet(getPacket.PickerType, getPacket.PickerId,
                                            getPacket.TransportId));

                                    if (getPacket.PickerType == 2)
                                    {
                                        Session.CurrentMapInstance?.Broadcast(
                                                StaticPacketHelper.GenerateEff(UserType.Npc, getPacket.PickerId, 5004));
                                    }
                                }
                                else
                                {
                                    lock (Session.Character.Inventory)
                                    {
                                        long characterDropperId = 0;
                                        if (mapItemInstance.CharacterId > 0)
                                        {
                                            characterDropperId = mapItemInstance.CharacterId;
                                        }

                                        var amount = mapItem.Amount;

                                        if (amount == 0) // Possible Dupe
                                        {
                                            return;
                                        }

                                        var inv = Session.Character.Inventory.AddToInventory(mapItemInstance)
                                                         .FirstOrDefault();
                                        if (inv != null)
                                        {
                                            if (mapItem is MonsterMapItem)
                                            {
                                                Session.Character.IncrementQuests(QuestType.Collect1, mapItem.ItemVNum);
                                                Session.Character.IncrementQuests(QuestType.Collect2, mapItem.ItemVNum);
                                                Session.Character.IncrementQuests(QuestType.Collect4, mapItem.ItemVNum);
                                            }

                                            Session.CurrentMapInstance.DroppedList.Remove(getPacket.TransportId);

                                            Session.CurrentMapInstance?.Broadcast(
                                                StaticPacketHelper.GenerateGet(getPacket.PickerType, getPacket.PickerId,
                                                    getPacket.TransportId));

                                            if (getPacket.PickerType == 2)
                                            {
                                                Session.CurrentMapInstance?.Broadcast(
                                                    StaticPacketHelper.GenerateEff(UserType.Npc, getPacket.PickerId,
                                                        5004));
                                                Session.SendPacket(Session.Character.GenerateIcon(1, 1, inv.ItemVNum));
                                            }

                                            Session.SendPacket(Session.Character.GenerateSay(
                                                $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {inv.Item.Name} x{amount}",
                                                12));
                                            if (Session.CurrentMapInstance.MapInstanceType ==
                                                MapInstanceType.LodInstance)
                                            {
                                                Session.CurrentMapInstance?.Broadcast(
                                                        Session.Character.GenerateSay(
                                                                $"{string.Format(Language.Instance.GetMessageFromKey("ITEM_ACQUIRED_LOD"), Session.Character.Name)}: {inv.Item.Name} x {mapItem.Amount}",
                                                                10));
                                            }

                                           
                                            Logger.LogUserEvent("CHARACTER_ITEM_GET", Session.GenerateIdentity(),
                                                $"[GetItem]IIId: {inv.Id} ItemVNum: {inv.ItemVNum} Amount: {amount}");
                                        }
                                        else
                                        {
                                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                                Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // handle gold drop
                                var maxGold = GameConfiguration.MaxGold;

                                var multiplier = 1 + Session.Character.GetBuff(BCardType.CardType.Item,
                                                     (byte)AdditionalTypes.Item.IncreaseEarnedGold)[0] / 100D;
                                multiplier +=
                                    (Session.Character.ShellEffectMain.FirstOrDefault(s =>
                                         s.Effect == (byte)ShellWeaponEffectType.GainMoreGold)?.Value ?? 0) / 100D;

                                if (mapItem is MonsterMapItem droppedGold
                                    && Session.Character.Gold + droppedGold.GoldAmount * multiplier <= maxGold)
                                {
                                    if (getPacket.PickerType == 2)
                                    {
                                        Session.SendPacket(Session.Character.GenerateIcon(1, 1, 1046));
                                    }

                                    Session.Character.Gold += (int)(droppedGold.GoldAmount * multiplier);

                                    Logger.LogUserEvent("CHARACTER_ITEM_GET", Session.GenerateIdentity(),
                                        $"[GetItem]Gold: {(int)(droppedGold.GoldAmount * multiplier)})");

                                    Session.SendPacket(Session.Character.GenerateSay(
                                        $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {mapItem.GetItemInstance().Item.Name} x{droppedGold.GoldAmount}{(multiplier > 1 ? $" + {(int)(droppedGold.GoldAmount * multiplier) - droppedGold.GoldAmount}" : "")}",
                                        12));
                                }
                                else
                                {
                                    Session.Character.Gold = maxGold;
                                    Logger.LogUserEvent("CHARACTER_ITEM_GET", Session.GenerateIdentity(), "[MaxGold]");
                                    Session.SendPacket(
                                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MAX_GOLD"),
                                            0));
                                }

                                Session.SendPacket(Session.Character.GenerateGold());
                                Session.CurrentMapInstance.DroppedList.Remove(getPacket.TransportId);

                                Session.CurrentMapInstance?.Broadcast(
                                    StaticPacketHelper.GenerateGet(getPacket.PickerType, getPacket.PickerId,
                                        getPacket.TransportId));

                                if (getPacket.PickerType == 2)
                                {
                                    Session.CurrentMapInstance?.Broadcast(
                                            StaticPacketHelper.GenerateEff(UserType.Npc, getPacket.PickerId, 5004));
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}