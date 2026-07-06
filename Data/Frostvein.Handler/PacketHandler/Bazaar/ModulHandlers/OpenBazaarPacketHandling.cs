using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.HttpClients;
using System;

namespace Frostvein.Handler.Bazaar
{
    public class OpenBazaarPacketHandling : IPacketHandler
    {
        #region Instantiation

        public OpenBazaarPacketHandling(ClientSession session) => Session = session;

        private static readonly KeepAliveClient _keepAliveClient = KeepAliveClient.Instance;

        #endregion Instantiation

        #region Properties

        private ClientSession Session { get; }

        #endregion Properties

        #region Methods

        public void OpenBazaar(CSkillPacket cSkillPacket)
        {
            if (!_keepAliveClient.IsBazaarOnline())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo($"Uh oh, it looks like the bazaar server is offline ! Please inform a staff member about it as soon as possible !"));
                return;
            }

            StaticBonusDTO medal = Session.Character.StaticBonusList.Find(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);

            if (medal != null)
            {
                MedalType medalType = medal.StaticBonusType == StaticBonusType.BazaarMedalGold ? MedalType.Gold : MedalType.Silver;

                int time = (int)(medal.DateEnd - DateTime.Now).TotalHours;

                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOTICE_BAZAAR"), 0));
                Session.SendPacket($"wopen 32 {(byte)medalType} {time}");
            }
            else
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INFO_BAZAAR")));
            }

            #endregion Methods
        }
    }
}