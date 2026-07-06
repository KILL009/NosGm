using NosTale.Configuration;
using NosTale.Packets.Packets.ClientPackets;
using OpenNos.Core;
using OpenNos.DAL;
using OpenNos.GameObject;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenNos.Handler.PacketHandler.Bazaar
{
    public class CScalcPacketHandler : IPacketHandler
    {
        #region Instantiation

        public CScalcPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void RefreshPersonalBazarList(CSListPacket csListPacket)
        {
            if (!GameConfiguration.BazaarEnabled)
            {
                Session.SendPacket("msg 4 Action blocked");
                Session.SendPacket(Session.Character.GenerateSay("The Bazaar-Server is currently offline.", 12));
                return;
            }
            if (ServerManager.Instance.InShutdown)
            {
                return;
            }

            if (!Session.Character.CanUseNosBazaar())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INFO_BAZAAR")));
                return;
            }
            SpinWait.SpinUntil(() => !ServerManager.Instance.InBazaarRefreshMode);
            Session.SendPacket(Session.Character.GenerateRCSList(csListPacket));
            Session.SendPacket("rc_reg 1");
        }

        public async Task GetBazaarAsync(CScalcPacket cScalcPacket)
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

            if (Session.Character == null || Session.Character.InExchangeOrTrade)
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

            SpinWait.SpinUntil(() => !ServerManager.Instance.InBazaarRefreshMode);

            var bazaarItemDTO = DAOFactory.BazaarItemDAO.LoadById(cScalcPacket.BazaarId);



            if (bazaarItemDTO != null)
            {
                var itemInstanceDTO = DAOFactory.ItemInstanceDAO.LoadById(bazaarItemDTO.ItemInstanceId);

                if (itemInstanceDTO == null)
                {
                    return;
                }

                var itemInstance = new ItemInstance(itemInstanceDTO);

                if (itemInstance == null)
                {
                    return;
                }

                if (bazaarItemDTO.DateStart.AddMinutes(10) >= DateTime.Now)
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateInfo("You have to wait at least 10 minutes after publishing the item to take it out from NosBazaar"));
                    return;
                }

                if (bazaarItemDTO.SellerId != Session.Character.CharacterId)
                {
                    return;
                }

                if ((bazaarItemDTO.DateStart.AddHours(bazaarItemDTO.Duration).AddDays(bazaarItemDTO.MedalUsed ? 30 : 7) - DateTime.Now).TotalMinutes <= 0)
                {
                    return;
                }

                var soldAmount = bazaarItemDTO.Amount - itemInstance.Amount;
                var taxes = bazaarItemDTO.MedalUsed ? 0 : (long)(bazaarItemDTO.Price * 0.10 * soldAmount);
                var price = bazaarItemDTO.Price * soldAmount - taxes;

                var name = itemInstance.Item?.Name ?? "None";

                if (itemInstance.Amount == 0 || Session.Character.Inventory.CanAddItem(itemInstance.ItemVNum))
                {
                    if (itemInstance.Amount != bazaarItemDTO.Amount - cScalcPacket.Amount)
                    {
                        await Session.SendPacketAsync(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EXPLOIT_LOGGED"), 0));
                        return;
                    }
                    if (Session.Character.Gold + price <= GameConfiguration.MaxGold)
                    {
                        Session.Character.Gold += price;
                        await Session.SendPacketAsync(Session.Character.GenerateGold());
                        await Session.SendPacketAsync(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("REMOVE_FROM_BAZAAR"), price), 10));

                        // Edit this soo we dont generate new guid every single time we take
                        // something out.
                        if (itemInstance.Amount != 0)
                        {
                            var newItemInstance = itemInstance.DeepCopy();
                            newItemInstance.Id = Guid.NewGuid();
                            newItemInstance.Type = newItemInstance.Item.Type;
                            Session.Character.Inventory.AddToInventory(newItemInstance);
                        }

                        Logger.LogUserEvent("BAZAAR_REMOVE", Session.GenerateIdentity(), $"BazaarId: {cScalcPacket.BazaarId}, IId: {itemInstance.Id} VNum: {itemInstance.ItemVNum} Amount: {bazaarItemDTO.Amount} RemainingAmount: {itemInstance.Amount} Price: {bazaarItemDTO.Price}");
                        Logger.LogUserEvent("BAZAAR_REMOVE_PACKET", Session.GenerateIdentity(), $"Packet string: {cScalcPacket.OriginalContent.ToString()}");
                        if (DAOFactory.BazaarItemDAO.LoadById(bazaarItemDTO.BazaarItemId) != null)
                        {
                            DAOFactory.BazaarItemDAO.Delete(bazaarItemDTO.BazaarItemId);
                        }

                        DAOFactory.ItemInstanceDAO.Delete(itemInstance.Id);

                        Session.Character.Inventory.RemoveItemFromInventory(itemInstance.Id, itemInstance.Amount);

                        ServerManager.Instance.BazaarRefresh(bazaarItemDTO.BazaarItemId);


                        await Session.SendPacketAsync(UserInterfaceHelper.GenerateBazarRecollect(bazaarItemDTO.Price, soldAmount,
                            bazaarItemDTO.Amount, taxes, price, name));
                        new CSListPacketHandler(Session).RefreshPersonalBazarListAsync(new CSListPacket());

                        Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o =>
                                       RefreshPersonalBazarList(new CSListPacket()));
                        RefreshPersonalBazarList(new CSListPacket());

                    }
                    else
                    {
                        await Session.SendPacketAsync(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MAX_GOLD"), 0));
                        await Session.SendPacketAsync($"rc_scalc 0 -1 -1 -1 -1 -1 -1");
                    }
                }
                else
                {
                    await Session.SendPacketAsync(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE")));
                    await Session.SendPacketAsync($"rc_scalc 0 -1 -1 -1 -1 -1 -1");
                }
            }
            else
            {
                await Session.SendPacketAsync($"rc_scalc 0 -1 -1 -1 -1 -1 -1");
            }
        }

        #endregion
    }
}