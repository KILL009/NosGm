using Frostvein.Core;
using Frostvein.Core.Extensions;
using Frostvein.Domain;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Frostvein.GameObject.ItemThread
{
    public static class RaidSealThread
    {
        public static void Run(ClientSession session, ItemInstance inv)
        {
            var raidSeal = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Main);

            if (session.Character.LastRaidOpened.AddSeconds(10) > DateTime.Now)
            {
                session.SendPacket("info You have to wait 10 seconds before doing that again");
                return;
            }
            if (session.Character.MapInstance.MapInstanceType == MapInstanceType.ArenaInstance)
            {
                session.SendPacket(session.Character.GenerateSay("You can't do this.", 11));
                return;
            }

            if (raidSeal != null)
            {
                var raid = ServerManager.Instance.Raids.FirstOrDefault(s => s.Id == raidSeal.Item.EffectValue)?.Copy();

                if (raidSeal.Item.EffectValue == 24 && !ServerManager.Instance.IsAct6RaidErenia)
                {
                    MessageExtension.SendBubble(session, "This won't work unless the Audience with Erenia has started");
                    return;
                }

                if (raidSeal.Item.EffectValue == 23 && !ServerManager.Instance.IsAct6RaidZenas)
                {
                    MessageExtension.SendBubble(session, "This won't work unless the Audience with Zenas has started");
                    return;
                }

                if (raid != null)
                {
                    if (ServerManager.Instance.ChannelId == 51 || session.CurrentMapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
                    {
                        return;
                    }

                    if (ServerManager.Instance.IsCharacterMemberOfGroup(session.Character.CharacterId))
                    {
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RAID_OPEN_GROUP"), 12));
                        return;
                    }

                    if (session.Character.Level < raid.LevelMinimum)
                    {
                        session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RAID_LEVEL_INCORRECT"), 10));

                        return;
                    }

                    var group = new Group
                    {
                        GroupType = raid.IsGiantTeam ? GroupType.GiantTeam : raid.IsMediumTeam ? GroupType.MediumTeam : raid.IsTeam ? GroupType.Team : GroupType.BigTeam,
                        Raid = raid
                    };

                    switch (raid.Id)
                    {
                        //fernon
                        case 25:
                            group.GroupType = GroupType.BigTeam;
                            break;

                        case 34:
                        case 20:
                            group.GroupType = GroupType.GiantTeam;
                            break;
                    }

                    if (group.JoinGroup(session))
                    {
                        ServerManager.Instance.AddGroup(group);
                        session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        string.Format(Language.Instance.GetMessageFromKey("RAID_LEADER"),
                                session.Character.Name), 0));
                        session.SendPacket(session.Character.GenerateSay(
                        string.Format(Language.Instance.GetMessageFromKey("RAID_LEADER"),
                                session.Character.Name), 10));
                        session.SendPacket(session.Character.GenerateRaid(2));
                        session.SendPacket(session.Character.GenerateRaid(0));
                        session.SendPacket(session.Character.GenerateRaid(1));
                        session.SendPacket(group.GenerateRdlst());
                        session.Character.Inventory.RemoveItemFromInventory(raidSeal.Id);
                        session.Character.LastRaidOpened = DateTime.Now;
                    }
                }
            }

        }
    }
}
