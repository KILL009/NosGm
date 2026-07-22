using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G750 : IGuriHandler
    {
        public long GuriEffectId => 750;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 750)
            {
                const short baseVnum = 1623;
                if (ServerManager.Instance.ChannelId == 51)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED_ACT4"), 0));
                    return;
                }
                if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4ShipAngel || Session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4ShipDemon)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED_ACT4SHIP"), 0));
                    return;
                }
                if (Enum.TryParse(e.Argument.ToString(), out FactionType faction) && Session.Character.Inventory.CountItem(baseVnum + (byte)faction) > 0)
                {
                    if ((byte)faction < 3) // Single family change
                    {
                        if (Session.Character.Faction == (FactionType)faction)
                        {
                            return;
                        }
                        if (Session.Character.Family != null)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("IN_FAMILY"), 0));
                            return;
                        }
                        Session.Character.Inventory.RemoveItemAmount(baseVnum + (byte)faction);
                        Session.Character.ChangeFaction((FactionType)faction);
                    }
                    else // Family faction change
                    {
                        faction -= 2;
                        if ((FactionType)Session.Character.Family.FamilyFaction == faction)
                        {
                            return;
                        }
                        if (Session.Character.Family == null)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_FAMILY"), 0));
                            return;
                        }
                        if (Session.Character.FamilyCharacter.Authority != FamilyAuthority.Head)
                        {
                            Session.SendPacket( UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_FAMILY_HEAD"), 0));
                            return;
                        }
                        if (Session.Character.Family.LastFactionChange > DateTime.Now.AddDays(-1).Ticks)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED"), 0));
                            return;
                        }

                        Session.Character.Inventory.RemoveItemAmount(baseVnum + (byte)faction + 2);
                        Session.Character.Family.ChangeFaction((byte)faction, Session);
                    }
                }
            }
        }
    }
}