using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.HttpClients;
using Frostvein.GameObject.Modules.Bazaar.Commands;
using Frostvein.GameObject.Modules.Bazaar.Queries;
using Frostvein.GameObject.Networking;
using Frostvein.Handler.World.Bazaar;
using System;
using System.Linq;

namespace Frostvein.Handler.Bazaar
{
    public class ModPriceBazaarPacketHandling : IPacketHandler
    {
        #region Instantiation

        public ModPriceBazaarPacketHandling(ClientSession session) => Session = session;

        private static readonly KeepAliveClient _keepAliveClient = KeepAliveClient.Instance;
        private static readonly BazaarHttpClient _bazaarClient = BazaarHttpClient.Instance;

        #endregion Instantiation

        #region Properties

        private ClientSession Session { get; }

        #endregion Properties

        #region Methods

        public void ModPriceBazaar(CModPacket cModPacket)
        {
            if (DateTime.Now < Session.Character.BazaarActionTimer.LastModAction.AddSeconds(2))
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo("You need to wait a few seconds before buying an item again."));
                return;
            }

            Session.Character.BazaarActionTimer.LastModAction = DateTime.Now;

            if (!_keepAliveClient.IsBazaarOnline())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo($"Uh oh, it looks like the bazaar server is offline ! Please inform a staff member about it as soon as possible !"));
                return;
            }

            if (_bazaarClient.GetItemState(new GetStateQuery { Id = cModPacket.BazaarId }))
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo($"An error occurred while trying to update this item."));
                return;
            }

            if (!Session.Character.CanUseNosBazaar())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INFO_BAZAAR")));
                return;
            }

            if (!_bazaarClient.SetItemState(new SetStateCommand { Id = cModPacket.BazaarId }))
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo("An error occurred while trying to get this item from the bazaar."));
                return;
            }

            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            if (ServerManager.Instance.InShutdown)
            {
                return;
            }

            BazaarItemDTO bz = _bazaarClient.GetBazaarItem(new GetBazaarItemQuery() { Id = cModPacket.BazaarId });

            if (bz != null)
            {
                var priceDifference = Math.Abs(bz.Price - cModPacket.Price);
                var canSell = cModPacket.Price * cModPacket.Amount <= 1000000000;
                if ((!canSell || priceDifference > 500000000) && Session.Character.Authority < AuthorityType.GM)
                {
                    foreach (var team in ServerManager.Instance.Sessions.Where(s => s.Account.Authority >= AuthorityType.ADMIN))
                    {
                        if (team.HasSelectedCharacter)
                        {
                            team.SendPacket(team.Character.GenerateSay($"User {Session.Character.Name} Might be trying to dupe, please be careful. (c_mod packet, bazaar)", 12));
                        }
                    }
                }

                if (!canSell)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo($"Total price must not exceed 1.000.000.000 gold."));
                    _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = cModPacket.BazaarId });
                    return;
                }

                if (bz.SellerId != Session.Character.CharacterId)
                {
                    _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = cModPacket.BazaarId });
                    return;
                }

                ItemInstance itemInstance = new ItemInstance(DAOFactory.ItemInstanceDAO.LoadById(bz.ItemInstanceId));
                if (bz.Amount != itemInstance.Amount)
                {
                    _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = cModPacket.BazaarId });
                    return;
                }

                if ((bz.DateStart.AddHours(bz.Duration).AddDays(bz.MedalUsed ? 30 : 7) - DateTime.Now).TotalMinutes <= 0)
                {
                    _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = cModPacket.BazaarId });
                    return;
                }

                if (cModPacket.Price <= 0)
                {
                    _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = cModPacket.BazaarId });
                    return;
                }
                long maxGold = ServerManager.Instance.Configuration.MaxGold;
                if (bz.Amount * cModPacket.Price > maxGold)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PRICE_EXCEEDED"), 0));
                    _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = cModPacket.BazaarId });
                    return;
                }

                StaticBonusDTO medal = Session.Character.StaticBonusList.Find(s =>
                    s.StaticBonusType == StaticBonusType.BazaarMedalGold
                    || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);
                if (cModPacket.Price >= (medal == null ? 1000000 : ServerManager.Instance.Configuration.MaxGold))
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PRICE_EXCEEDED"), 0));
                    _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = cModPacket.BazaarId });
                    return;
                }

                var item = ServerManager.GetItem(itemInstance.ItemVNum);

                bz.Price = cModPacket.Price;
                //bz.LastPriceUpdate = DateTime.Now;

                if (Session.Character.BazaarItems.ContainsKey(bz.BazaarItemId))
                {
                    Session.Character.BazaarItems[bz.BazaarItemId] = bz;
                }

                _bazaarClient.InsertOrUpdateBazaar(new InsertOrUpdateBazaarItemCommand() { BazaarItem = bz });

                Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("OBJECT_MOD_IN_BAZAAR"), bz.Price),
                    10));
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("OBJECT_MOD_IN_BAZAAR"), bz.Price),
                    0));

                Logger.LogUserEvent("BAZAAR_MOD", Session.GenerateIdentity(),
                    $"BazaarId: {bz.BazaarItemId}, IIId: {bz.ItemInstanceId} VNum: {itemInstance.ItemVNum} Amount: {bz.Amount} Price: {bz.Price} Time: {bz.Duration}");

                new RefreshPersonalListPacketHandler(Session).RefreshPersonalBazarList(new CSListPacket());
                _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = cModPacket.BazaarId });
            }
            else
            {
                _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = cModPacket.BazaarId });
            }

        }

        #endregion Methods
    }
}