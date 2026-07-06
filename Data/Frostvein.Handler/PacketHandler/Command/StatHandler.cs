using Frostvein.Configuration;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using System;
using System.Diagnostics;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class StatHandler : IPacketHandler
    {
        #region Instantiation

        public StatHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Stat(StatCommandPacket statCommandPacket)
        {

                Session.SendPacket(Session.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey("XP_RATE_NOW")}: {GameConfiguration.XPRate} ",
                    13));
                Session.SendPacket(Session.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey("DROP_RATE_NOW")}: {GameConfiguration.DropRate} ",
                    13));
                Session.SendPacket(Session.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey("GOLD_RATE_NOW")}: {GameConfiguration.GoldRate} ",
                    13));
                Session.SendPacket(Session.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey("GOLD_DROPRATE_NOW")}: {GameConfiguration.GoldDropRate} ",
                    13));
                Session.SendPacket(Session.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey("HERO_XPRATE_NOW")}: {GameConfiguration.HeroXPRate} ",
                    13));
                Session.SendPacket(Session.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey("FAIRYXP_RATE_NOW")}: {GameConfiguration.FairyXPRate} ",
                    13));
                Session.SendPacket(Session.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey("REPUTATION_RATE_NOW")}: {GameConfiguration.ReputationRate} ",
                    13));
                Session.SendPacket(Session.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey("SERVER_WORKING_TIME")}: {(Process.GetCurrentProcess().StartTime - DateTime.Now).ToString(@"d\ hh\:mm\:ss")} ",
                    13));
           
        }

        #endregion
    }
}