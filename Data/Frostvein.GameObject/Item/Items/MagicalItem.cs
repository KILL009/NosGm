

using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;
using System.Reactive.Linq;


namespace Frostvein.GameObject
{
    public class MagicalItem : Item
    {
        #region Instantiation

        public MagicalItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ItemInstance inv, byte Option = 0,
            string[] packetsplit = null)
        {
            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.ArenaInstance)
            {
                session.SendPacket("msg 4 You cannot do that here");
                return;
            }
            if (session.Character.IsVehicled)
            {
                session.SendPacket(
                    session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_VEHICLED"), 10));
                return;
            }

            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance) return;

            switch (Effect)
            {
                // airwaves - eventitems
                case 0:
                    if (inv.Item.ItemType == ItemType.Shell)
                    {
                        if (!inv.ShellEffects.Any())
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("SHELL_MUST_BE_IDENTIFIED"), 0));
                            return;
                        }

                        if (packetsplit?.Length < 9) return;

                        if (!int.TryParse(packetsplit[6], out var requestType)) return;
                        if (!Enum.TryParse(packetsplit[8], out InventoryType eqType)) return;

                        if (inv.ShellEffects.Count != 0 && packetsplit?.Length > 9 &&
                            byte.TryParse(packetsplit[9], out var islot))
                        {
                            var wearable =
                                session.Character.Inventory.LoadBySlotAndType(islot, InventoryType.Equipment);
                            if (wearable != null &&
                                (wearable.Item.ItemType == ItemType.Weapon ||
                                 wearable.Item.ItemType == ItemType.Armor) &&
                                wearable.Item.LevelMinimum >= inv.Upgrade && wearable.Rare >= inv.Rare &&
                                !wearable.Item.IsHeroic)
                                switch (requestType)
                                {
                                    case 0:
                                        session.SendPacket(wearable.ShellEffects.Any()
                                            ? $"qna #u_i^1^{session.Character.CharacterId}^{(short)inv.Type}^{inv.Slot}^1^1^{(short)eqType}^{islot} {Language.Instance.GetMessageFromKey("ADD_SHELL_IN_SHELL")}"
                                            : $"qna #u_i^1^{session.Character.CharacterId}^{(short)inv.Type}^{inv.Slot}^1^1^{(short)eqType}^{islot} {Language.Instance.GetMessageFromKey("ADD_OPTION_ON_STUFF")}");
                                        return;

                                    case 1:

                                        if (inv.ShellEffects == null) return;
                                        if (wearable.BoundCharacterId != session.Character.CharacterId &&
                                            wearable.BoundCharacterId != null)
                                        {
                                            session.SendPacket(
                                                UserInterfaceHelper.GenerateMsg(
                                                    Language.Instance.GetMessageFromKey("NEED_FRAGANCE"), 0));
                                            return;
                                        }

                                        if (wearable.Rare < inv.Rare)
                                        {
                                            session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                                Language.Instance.GetMessageFromKey("SHELL_RARITY_TOO_HIGH"), 0));
                                            return;
                                        }

                                        if (wearable.Item.LevelMinimum < inv.Upgrade)
                                        {
                                            session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                                Language.Instance.GetMessageFromKey("SHELL_LEVEL_TOO_HIGH"), 0));
                                            return;
                                        }

                                        var weapon = false;
                                        if (inv.ItemVNum >= 565 && inv.ItemVNum <= 576 ||
                                            inv.ItemVNum >= 589 && inv.ItemVNum <= 598)
                                            weapon = true;
                                        else if (inv.ItemVNum >= 577 && inv.ItemVNum <= 588 ||
                                                 inv.ItemVNum >= 656 && inv.ItemVNum <= 664 || inv.ItemVNum == 599)
                                            weapon = false;
                                        else
                                            return;
                                        if (wearable.Item.ItemType == ItemType.Weapon && weapon || wearable.Item.ItemType == ItemType.Armor && !weapon)
                                        {
                                            wearable.BoundCharacterId = session.Character.CharacterId;
                                            wearable.ShellRarity = inv.Rare;
                                            wearable.ShellEffects.Clear();
                                            DAOFactory.ShellEffectDAO.DeleteByEquipmentSerialId(
                                                wearable.EquipmentSerialId);
                                            wearable.ShellEffects.AddRange(inv.ShellEffects);
                                            if (wearable.EquipmentSerialId == Guid.Empty)
                                                wearable.EquipmentSerialId = Guid.NewGuid();
                                            wearable.AddShellsFixed(inv.ShellEffects);
                                            session.Character.DeleteItemByItemInstanceId(inv.Id);
                                            session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                                Language.Instance.GetMessageFromKey("OPTION_SUCCESS"), 0));
                                        }

                                        break;
                                }
                        }

                        return;
                    }

                    if (ItemType == ItemType.Event)
                    {
                        session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player,
                            session.Character.CharacterId, EffectValue));
                        if (MappingHelper.GuriItemEffects.ContainsKey(EffectValue))
                            session.CurrentMapInstance?.Broadcast(
                                UserInterfaceHelper.GenerateGuri(19, 1, session.Character.CharacterId,
                                    MappingHelper.GuriItemEffects[EffectValue]), session.Character.PositionX,
                                session.Character.PositionY);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    
                    break;          

                //respawn objects
                case 1:
                    if (session.Character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance ||
                        session.Character.IsSeal)
                    {
                        session.SendPacket(
                            session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_USE_THAT"), 10));
                        return;
                    }

                    int type, secondaryType, inventoryType, slot;
                    if (packetsplit?.Length > 6 && int.TryParse(packetsplit[2], out type) &&
                        int.TryParse(packetsplit[3], out secondaryType) &&
                        int.TryParse(packetsplit[4], out inventoryType) && int.TryParse(packetsplit[5], out slot))
                    {
                        int packetType;
                        switch (EffectValue)
                        {
                            case 0:
                                if (inv.ItemVNum != 2070 && inv.ItemVNum != 2071 && inv.ItemVNum != 10010 || session.CurrentMapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.Act51 || m.MapTypeId == (short)MapTypeEnum.Act52))
                                {
                                    return;
                                }

                                if (inv.ItemVNum != 2070 && inv.ItemVNum != 2071 && inv.ItemVNum != 10010 || session.Character.MapId == 5404)
                                {
                                    session.SendPacket("msg 4 You can't use that item there.");
                                    return;
                                }
                                if (Option == 0)
                                {
                                    if (ServerManager.Instance.ChannelId == 51)
                                    {
                                        session.SendPacket(UserInterfaceHelper.GenerateDialog($"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^2 #u_i^{type}^{secondaryType}^{inventoryType}^{slot}^0 {Language.Instance.GetMessageFromKey("WANT_TO_GO_BASE")}"));
                                    }
                                    else
                                    {
                                        session.SendPacket(UserInterfaceHelper.GenerateDialog($"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^1 #u_i^{type}^{secondaryType}^{inventoryType}^{slot}^2 {Language.Instance.GetMessageFromKey("WANT_TO_SAVE_POSITION")}"));
                                    }
                                }
                                else if (int.TryParse(packetsplit[6], out packetType))
                                {
                                    switch (packetType)
                                    {
                                        case 1:
                                            session.SendPacket(UserInterfaceHelper.GenerateDelay(5000, 7, $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^3"));
                                            break;

                                        case 2:
                                            session.SendPacket(UserInterfaceHelper.GenerateDelay(5000, 7, $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^4"));
                                            break;

                                        case 3:
                                            session.Character.SetReturnPoint(session.Character.MapId, session.Character.MapX, session.Character.MapY);
                                            var respawn = session.Character.Respawn;
                                            if (respawn.DefaultX != 0 && respawn.DefaultY != 0 && respawn.DefaultMapId != 0)
                                            {
                                                var mapCell = new MapCell();
                                                for (var i = 0; i < 5; i++)
                                                {
                                                    mapCell.X = (short)(respawn.DefaultX + ServerManager.RandomNumber(-3, 3));

                                                    mapCell.Y = (short)(respawn.DefaultY + ServerManager.RandomNumber(-3, 3));

                                                    if (ServerManager.GetMapInstanceByMapId(respawn.DefaultMapId) is MapInstance GoToMap)

                                                        if (!GoToMap.Map.IsBlockedZone(mapCell.X, mapCell.Y))
                                                        {
                                                            break;
                                                        }

                                                }

                                                ServerManager.Instance.ChangeMap(session.Character.CharacterId, respawn.DefaultMapId, mapCell.X, mapCell.Y);

                                            }
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;

                                        case 4:
                                            var respawnObj = session.Character.Respawn;
                                            if (ServerManager.Instance.ChannelId == 51)
                                            {
                                                respawnObj.DefaultMapId = (short)(129 + session.Character.Faction);
                                                respawnObj.DefaultX = 41;
                                                respawnObj.DefaultY = 42;
                                            }

                                            if (respawnObj.DefaultX != 0 && respawnObj.DefaultY != 0 &&
                                                respawnObj.DefaultMapId != 0)
                                            {
                                                var mapCell = new MapCell();
                                                for (var i = 0; i < 5; i++)
                                                {
                                                    mapCell.X = (short)(respawnObj.DefaultX +
                                                                         ServerManager.RandomNumber(-3, 3));
                                                    mapCell.Y = (short)(respawnObj.DefaultY +
                                                                         ServerManager.RandomNumber(-3, 3));
                                                    if (ServerManager.GetMapInstanceByMapId(respawnObj.DefaultMapId) is
                                                        MapInstance GoToMap)
                                                        if (!GoToMap.Map.IsBlockedZone(mapCell.X, mapCell.Y))
                                                            break;
                                                }

                                                ServerManager.Instance.ChangeMap(session.Character.CharacterId,
                                                    respawnObj.DefaultMapId, mapCell.X, mapCell.Y);
                                            }

                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                    }
                                }

                                break;

                            case 1:
                                if (inv.ItemVNum != 2071 && inv.ItemVNum != 10011) return;
                                if (int.TryParse(packetsplit[6], out packetType))
                                {
                                    if (ServerManager.Instance.ChannelId == 51)
                                    {
                                        session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANNOT_USE"), 10));
                                        return;
                                    }

                                    var respawn = session.Character.Return;
                                    switch (packetType)
                                    {
                                        case 0:
                                            if (respawn.DefaultX != 0 && respawn.DefaultY != 0 &&
                                                respawn.DefaultMapId != 0)
                                                session.SendPacket(UserInterfaceHelper.GenerateRp(respawn.DefaultMapId,
                                                    respawn.DefaultX, respawn.DefaultY,
                                                    $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^1"));
                                            break;

                                        case 1:
                                            session.SendPacket(UserInterfaceHelper.GenerateDelay(5000, 7,
                                                $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^2"));
                                            break;

                                        case 2:
                                            if (respawn.DefaultX != 0 && respawn.DefaultY != 0 &&
                                                respawn.DefaultMapId != 0)
                                                ServerManager.Instance.ChangeMap(session.Character.CharacterId,
                                                    respawn.DefaultMapId, respawn.DefaultX, respawn.DefaultY);
                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                    }
                                }

                                break;

                            case 2:
                                if (inv.ItemVNum != 2072 && inv.ItemVNum != 10012) return;
                                if (Option == 0)
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateDelay(5000, 7,
                                        $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^1"));
                                }
                                else
                                {
                                    ServerManager.Instance.JoinMiniland(session, session);
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                }

                                break;

                            case 4:
                                if (inv.ItemVNum != 2188 ||
                                    session.Character.MapInstance.Map.MapTypes.Any(s =>
                                        s.MapTypeId == (short)MapTypeEnum.Act4)) return;
                                if (Option == 0)
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateDelay(5000, 7,
                                        $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^1"));
                                }
                                else
                                {
                                    ServerManager.Instance.ChangeMap(session.Character.CharacterId, 98, 28, 34);
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                }

                                break;

                            case 5:
                                if (inv.ItemVNum != 2311 || ServerManager.Instance.ChannelId != 51) return;
                                if (ServerManager.GetAllMapInstances().SelectMany(s => s.Monsters.ToList())
                                        .LastOrDefault(s => s.MonsterVNum == (short)session.Character.Faction + 964) is
                                    MapMonster flag)
                                {
                                    if (Option == 0)
                                    {
                                        session.SendPacket(UserInterfaceHelper.GenerateDelay(5000, 7,
                                            $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^1"));
                                    }
                                    else
                                    {
                                        ServerManager.Instance.ChangeMapInstance(session.Character.CharacterId,
                                            flag.MapInstance.MapInstanceId, flag.MapX, flag.MapY);
                                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                    }
                                }

                                break;

                            case 6:
                                if (inv.ItemVNum != 2384 || ServerManager.Instance.ChannelId == 51 ||
                                    session.CurrentMapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
                                    return;
                                if (Option == 0)
                                    session.SendPacket(UserInterfaceHelper.GenerateDialog(
                                        $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^1 #u_i^{type}^{secondaryType}^{inventoryType}^{slot}^2 {Language.Instance.GetMessageFromKey("WANT_TO_SAVE_POSITION")}"));
                                else if (int.TryParse(packetsplit[6], out packetType))
                                    switch (packetType)
                                    {
                                        case 1:
                                            session.SendPacket(UserInterfaceHelper.GenerateDelay(5000, 7,
                                                $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^3"));
                                            break;

                                        case 2:
                                            session.SendPacket(UserInterfaceHelper.GenerateDelay(5000, 7,
                                                $"#u_i^{type}^{secondaryType}^{inventoryType}^{slot}^4"));
                                            break;

                                        case 3:
                                            if (session.CurrentMapInstance.Map.MapTypes.Any(s =>
                                                s.MapTypeId == (short)MapTypeEnum.Act51 ||
                                                s.MapTypeId == (short)MapTypeEnum.Act52))
                                            {
                                                session.Character.SetReturnPoint(session.Character.MapId,
                                                    session.Character.MapX, session.Character.MapY);
                                                var respawn = session.Character.Respawn;
                                                if (respawn.DefaultX != 0 && respawn.DefaultY != 0 &&
                                                    respawn.DefaultMapId != 0)
                                                {
                                                    var mapCell = new MapCell();
                                                    for (var i = 0; i < 5; i++)
                                                    {
                                                        mapCell.X = (short)(respawn.DefaultX +
                                                                             ServerManager.RandomNumber(-3, 3));
                                                        mapCell.Y = (short)(respawn.DefaultY +
                                                                             ServerManager.RandomNumber(-3, 3));
                                                        if (ServerManager.GetMapInstanceByMapId(respawn.DefaultMapId) is
                                                            MapInstance GoToMap)
                                                            if (!GoToMap.Map.IsBlockedZone(mapCell.X, mapCell.Y))
                                                                break;
                                                    }

                                                    ServerManager.Instance.ChangeMap(session.Character.CharacterId,
                                                        respawn.DefaultMapId, mapCell.X, mapCell.Y);
                                                }

                                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            }
                                            else
                                            {
                                                goto case 4;
                                            }

                                            break;

                                        case 4:
                                            var mapInstanceBackup = session.CurrentMapInstance;
                                            session.CurrentMapInstance = ServerManager.GetMapInstanceByMapId(170);
                                            var respawnObj = session.Character.Respawn;
                                            session.CurrentMapInstance = mapInstanceBackup;
                                            if (ServerManager.Instance.ChannelId == 51)
                                            {
                                                respawnObj.DefaultMapId = (short)(129 + session.Character.Faction);
                                                respawnObj.DefaultX = 41;
                                                respawnObj.DefaultY = 42;
                                            }

                                            if (respawnObj.DefaultX != 0 && respawnObj.DefaultY != 0 &&
                                                respawnObj.DefaultMapId != 0)
                                            {
                                                var mapCell = new MapCell();
                                                for (var i = 0; i < 5; i++)
                                                {
                                                    mapCell.X = (short)(respawnObj.DefaultX +
                                                                         ServerManager.RandomNumber(-3, 3));
                                                    mapCell.Y = (short)(respawnObj.DefaultY +
                                                                         ServerManager.RandomNumber(-3, 3));
                                                    if (ServerManager.GetMapInstanceByMapId(respawnObj.DefaultMapId) is
                                                        MapInstance GoToMap)
                                                        if (!GoToMap.Map.IsBlockedZone(mapCell.X, mapCell.Y))
                                                            break;
                                                }

                                                ServerManager.Instance.ChangeMap(session.Character.CharacterId,
                                                    respawnObj.DefaultMapId, mapCell.X, mapCell.Y);
                                            }

                                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                            break;
                                    }

                                break;
                        }
                    }

                    break;

                // dyes or waxes
                case 10:
                case 11:
                    if (!session.Character.IsVehicled)
                    {
                        if (Effect == 10 || Effect == 13)
                        {
                            if (EffectValue == 99)
                            {
                                var nextValue = (byte)ServerManager.RandomNumber(0, 127);
                                session.Character.HairColor = Enum.IsDefined(typeof(HairColorType), nextValue) ? (HairColorType)nextValue : 0;
                            }
                            else
                            {
                                session.Character.HairColor = Enum.IsDefined(typeof(HairColorType), (byte)EffectValue) ? (HairColorType)EffectValue : 0;
                            }
                        }
                        else if (Effect == 11 || Effect == 15)
                        {
                            if (session.Character.Class == (byte)ClassType.Adventurer && EffectValue > 1)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("ADVENTURERS_CANT_USE"), 10));
                                return;
                            }

                            if (session.Character.Gender != (GenderType)Sex && (VNum < 2130 || VNum > 2162) &&
                                (VNum < 10025 || VNum > 10026))
                            {
                                session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANNOT_USE"),
                                        10));
                                return;
                            }

                            session.Character.HairStyle = Enum.IsDefined(typeof(HairStyleType), (byte)EffectValue)
                                ? (HairStyleType)EffectValue
                                : 0;
                        }
                        else
                        {
                            if (session.Character.Class == (byte)ClassType.Adventurer && EffectValue > 1)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("ADVENTURERS_CANT_USE"), 10));
                                return;
                            }

                            session.Character.HairStyle = Enum.IsDefined(typeof(HairStyleType), (byte)EffectValue)
                                ? (HairStyleType)EffectValue
                                : 0;
                        }

                        session.SendPacket(session.Character.GenerateEq());
                        session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateIn());
                        session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateGidx());
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }

                    break;

                // TS npcs health
                case 12:
                    if (EffectValue > 0)
                        if (session.Character.Timespace != null)
                        {
                            session.Character.MapInstance.GetBattleEntitiesInRange(
                                    new MapCell { X = session.Character.PositionX, Y = session.Character.PositionY }, 6)
                                .Where(s => (s.MapNpc != null || s.Mate != null && s.Mate.IsTemporalMate) &&
                                            !session.Character.BattleEntity.CanAttackEntity(s)).ToList()
                                .ForEach(s =>
                                {
                                    var health = EffectValue;
                                    if (s.Hp + EffectValue > s.HpMax) health = s.HpMax - s.Hp;
                                    s.Hp += health;
                                    session.Character.MapInstance.Broadcast(s.GenerateRc(health));
                                    s.Mate?.Owner.Session.SendPacket(s.Mate.GenerateStatInfo());
                                });
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                            session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player,
                                session.Character.CharacterId, 48));
                        }

                    break;

                // dignity restoration
                case 14:
                    if ((EffectValue == 100 || EffectValue == 200) && session.Character.Dignity < 100 &&
                        !session.Character.IsVehicled)
                    {
                        session.Character.Dignity += EffectValue;
                        if (session.Character.Dignity > 100) session.Character.Dignity = 100;
                        session.SendPacket(session.Character.GenerateFd());
                        session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player,
                            session.Character.CharacterId, 49 - (byte)session.Character.Faction));
                        session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateIn(InEffect: 1),
                            ReceiverType.AllExceptMe);
                        session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateGidx(),
                            ReceiverType.AllExceptMe);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }
                    else if (EffectValue == 2000 && session.Character.Dignity < 100 && !session.Character.IsVehicled)
                    {
                        session.Character.Dignity = 100;
                        session.SendPacket(session.Character.GenerateFd());
                        session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player,
                            session.Character.CharacterId, 49 - (byte)session.Character.Faction));
                        session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateIn(InEffect: 1),
                            ReceiverType.AllExceptMe);
                        session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateGidx(),
                            ReceiverType.AllExceptMe);
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }

                    break;

                // speakers
                case 15:
                    if (!session.Character.IsVehicled && Option == 0)
                        session.SendPacket(UserInterfaceHelper.GenerateGuri(10, 3, session.Character.CharacterId, 1));
                    break;

                // bubbles
                case 16:
                    if (!session.Character.IsVehicled && Option == 0)
                        session.SendPacket(UserInterfaceHelper.GenerateGuri(10, 4, session.Character.CharacterId, 1));
                    break;

                // wigs
                case 30:
                    if (!session.Character.IsVehicled)
                    {
                        var wig = session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Hat,
                            InventoryType.Wear);
                        if (wig != null)
                        {
                            wig.Design = (byte)ServerManager.RandomNumber(0, 15);
                            session.SendPacket(session.Character.GenerateEq());
                            session.SendPacket(session.Character.GenerateEquipment());
                            session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateIn());
                            session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateGidx());
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                        else
                        {
                            session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_WIG"), 0));
                        }
                    }

                    break;

                case 31:
                    if (!session.Character.IsVehicled && session.Character.HairStyle == HairStyleType.Hair7)
                    {
                        session.Character.HairStyle = HairStyleType.Hair8;
                        session.SendPacket(session.Character.GenerateEq());
                        session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateIn());
                        session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateGidx());
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);

                        // idk how it works yet but seems like all characters with this hairstyle have DarkPurple hair
                        //session.Character.HairColor = HairColorType.DarkPurple;
                    }

                    break;

                // Passive skills books
                case 99:
                    if (session.Character.HeroLevel >= EffectValue)
                    {
                        if (session.Character.AddSkill((short)(Sex + 1)))
                        {
                            session.SendPacket(session.Character.GenerateSki());
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                        }
                    }
                    else
                    {
                        session.SendPacket(
                            session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LOW_HERO_LVL"), 10));
                    }

                    break;

                case 300:
                    var shouldAnnounceLeader = false;

                    if (session.Character.Group != null && session.Character.Group.GroupType != GroupType.Group &&
                        session.Character.Group.IsLeader(session) &&
                        session.CurrentMapInstance.Portals.Any(s => s.Type == (short)PortalType.Raid))
                    {
                        var isLeaderInA7 = session.Character.MapId.CheckIfInRange(2628, 2644);
                        var delay = 0;
                        foreach (var sess in session.Character.Group.Sessions.GetAllItems())
                        {
                            if (isLeaderInA7)
                            {
                                if (!sess.Character.MapId.CheckIfInRange(2628, 2644))
                                {
                                    sess.SendPacket(sess.Character.GenerateSay("You couldn't be tp'd to the raidmap due to not being in A7 Maps.", 10));
                                    shouldAnnounceLeader = true;
                                    continue;
                                }
                            }

                            Observable.Timer(TimeSpan.FromMilliseconds(delay)).Subscribe(o =>
                            {
                                if (sess?.Character != null && session?.CurrentMapInstance != null &&
                                    session?.Character != null &&
                                    sess.Character != session.Character)
                                    ServerManager.Instance.TeleportOnRandomPlaceInMap(sess,
                                        session.CurrentMapInstance.MapInstanceId);
                            });
                            delay += 100;
                        }

                        if (shouldAnnounceLeader)
                        {
                            session.SendPacket(session.Character.GenerateSay("Not all the players were in A7, so them all couldn't be tp'd.", 10));
                        }

                        session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    }

                    break;

                default:
                    //LOGGER($"[HANDLER] Handler not found for: {GetType()} | {VNum} | {Effect} | {EffectValue}");
                    break;
            }
        }

        #endregion
    }
}