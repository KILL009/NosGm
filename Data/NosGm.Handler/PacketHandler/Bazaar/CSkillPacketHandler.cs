using NosTale.Configuration;
using NosTale.Packets.Packets.ClientPackets;
using OpenNos.Core;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenNos.Handler.PacketHandler.Bazaar
{
    public class CSkillPacketHandler : IPacketHandler
    {
        #region Instantiation

        public CSkillPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        public ServerManager Channel { get; set; }

        #endregion

        #region Methods

        public async Task OpenBazaarAsync(CSkillPacket cSkillPacket)
        {
            if (!GameConfiguration.BazaarEnabled)
            {
                Session.SendPacket("info The Bazaar Server is currently offline");
                return;
            }
            if (Session.Character.Level < 85)
            {
                Session.SendPacket("info You need to be at least Level 85\nYou need to have at least 90.000 Reputation");
                return;
            }
            if (Session.Character.Reputation < 90000)
            {
                Session.SendPacket("info You need to be at least Level 85\nYou need to have at least 90.000 Reputation");
                return;
            }

            if (Session.Character.Channel.ChannelId > 1)
            {
                Session.SendPacket("info The NosBazaar can only be accessed on Channel 1");
                return;
            }


            if (ServerManager.Instance.InShutdown)
            {
                return;
            }
            if (Session.Character == null || Session.Character.InExchangeOrTrade)
            {
                return;
            }
            if (Session.Character.IsMuted())
            {
                return;
            }
            SpinWait.SpinUntil(() => !ServerManager.Instance.InBazaarRefreshMode);

            var medal = Session.Character.StaticBonusList.Find(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);

            if (medal != null)
            {
                var medalType = medal.StaticBonusType == StaticBonusType.BazaarMedalGold ? MedalType.Gold : MedalType.Silver;

                var time = (int)(medal.DateEnd - DateTime.Now).TotalHours;

                await Session.SendPacketAsync(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOTICE_BAZAAR"), 0));
                await Session.SendPacketAsync($"wopen 32 {(byte)medalType} {time}");
            }
            else
            {
                await Session.SendPacketAsync(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INFO_BAZAAR")));
            }
        }

        #endregion
    }
}