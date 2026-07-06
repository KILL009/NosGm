using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class FamilyFactionHandler : IPacketHandler
    {
        #region Instantiation

        public FamilyFactionHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void FamilyFaction(FamilyFactionPacket familyFactionPacket)
        {
            if (ServerManager.Instance.ChannelId == 51)
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED_ACT4"),
                        0));
                return;
            }
            if (Session.Character.LastFamilyAction.AddSeconds(120) > DateTime.Now)
            {
                Session.SendPacket("info You have to wait 2 Minutes before doing that again");
                return;
            }

            if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4ShipAngel
                || Session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4ShipDemon)
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED_ACT4SHIP"),
                        0));
                return;
            }

            if (familyFactionPacket != null)
            {
                //Session.AddLogsCmd(familyFactionPacket);
                if (string.IsNullOrEmpty(familyFactionPacket.FamilyName) && Session.Character.Family != null)
                {
                    Session.Character.Family.ChangeFaction(
                        Session.Character.Family.FamilyFaction == 1 ? (byte)2 : (byte)1, Session);
                    Session.Character.LastFamilyAction = DateTime.Now;
                    return;
                }

                var family =
                    ServerManager.Instance.FamilyList.FirstOrDefault(s => s.Name == familyFactionPacket.FamilyName);
                if (family != null)
                    family.ChangeFaction(family.FamilyFaction == 1 ? (byte)2 : (byte)1, Session);
                else
                    Session.SendPacket(Session.Character.GenerateSay("Family not found.", 10));
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(FamilyFactionPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}