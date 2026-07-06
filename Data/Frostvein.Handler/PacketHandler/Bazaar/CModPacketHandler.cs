using NosTale.Packets.Packets.ClientPackets;
using OpenNos.Core;
using OpenNos.DAL;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using System;
using System.Threading.Tasks;

namespace OpenNos.Handler.PacketHandler.Bazaar
{
    public class CModPacketHandler : IPacketHandler
    {
        #region Instantiation

        public CModPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ModPriceBazaarAsync(CModPacket cModPacket)
        {
            Session.SendPacket("info This Feature has been disabled");
            /*
            DateTime currentDate = DateTime.UtcNow;
           
            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            if (ServerManager.Instance.InShutdown)
            {
                return;
            }


            if (Session.Character.IsMuted())
            {
                return;
            }

            if (!Session.Character.CanUseNosBazaar())
            {
                return;
            }

            var bz = DAOFactory.BazaarItemDAO.LoadById(cModPacket.BazaarId);
            if (bz != null)
            {
                lock (bz)
                {
                    if (bz.SellerId != Session.Character.CharacterId)
                    {
                        return;
                    }

                    if (Session.Character.CharacterId != bz.SellerId)
                    {
                        return;
                    }

                    var itemInstance = new ItemInstance(DAOFactory.ItemInstanceDAO.LoadById(bz.ItemInstanceId));
                    if (itemInstance == null || bz.Amount != itemInstance.Amount)
                    {
                        return;
                    }

                    if ((bz.DateStart.AddHours(bz.Duration).AddDays(bz.MedalUsed ? 30 : 7) - DateTime.Now).TotalMinutes <= 0)
                    {
                        return;
                    }

                    if (cModPacket.Price <= 0)
                    {
                        return;
                    }

                    var medal = Session.Character.StaticBonusList.Find(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);
                    if (cModPacket.Price >= (medal == null ? 1000000 : GameConfiguration.MaxGold))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PRICE_EXCEEDED"), 0));
                        return;
                    }

                    bz.Price = cModPacket.Price;

                    DAOFactory.BazaarItemDAO.InsertOrUpdate(ref bz);
                    ServerManager.Instance.BazaarRefresh(bz.BazaarItemId);

                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("OBJECT_MOD_IN_BAZAAR"), bz.Price), 10));
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("OBJECT_MOD_IN_BAZAAR"), bz.Price), 0));

                    Logger.LogUserEvent("BAZAAR_MOD", Session.GenerateIdentity(),
                        $"BazaarId: {bz.BazaarItemId}, IIId: {bz.ItemInstanceId} VNum: {itemInstance.ItemVNum} Amount: {bz.Amount} Price: {bz.Price} Time: {bz.Duration}");
                    Logger.LogUserEvent("BAZAAR_BUY_PACKET", Session.GenerateIdentity(), $"Packet string: {cModPacket.OriginalContent.ToString()}");
                    new CSListPacketHandler(Session).RefreshPersonalBazarListAsync(new CSListPacket()).ConfigureAwait(false);
                }
            }
            */
        }

        #endregion
    }
}