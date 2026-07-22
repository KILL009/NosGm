using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.HttpClients;
using NosGm.GameObject.Modules.Bazaar.Commands;
using NosGm.GameObject.Networking;
using NosGm.Handler.World.Bazaar;
using System;

namespace NosGm.Handler.Bazaar
{
    public class ModPriceBazaarPacketHandling : IPacketHandler
    {
        private static readonly KeepAliveClient KeepAliveClient = KeepAliveClient.Instance;
        private static readonly BazaarHttpClient BazaarClient = BazaarHttpClient.Instance;

        public ModPriceBazaarPacketHandling(ClientSession session) => Session = session;

        private ClientSession Session { get; }

        public void ModPriceBazaar(CModPacket packet)
        {
            if (packet == null || Session?.Character == null || Session.Account == null)
            {
                return;
            }

            if (!CanUseBazaar() ||
                packet.BazaarId <= 0 ||
                packet.ItemVNum <= 0 ||
                packet.Amount <= 0 ||
                packet.Price <= 0)
            {
                return;
            }

            BazaarItemDTO listing = DAOFactory.BazaarItemDAO.LoadById(packet.BazaarId);
            if (listing == null || listing.SellerId != Session.Character.CharacterId)
            {
                SendStateChanged();
                return;
            }

            ItemInstanceDTO item = DAOFactory.ItemInstanceDAO.LoadById(listing.ItemInstanceId);
            if (item == null ||
                item.ItemVNum != packet.ItemVNum ||
                item.Amount != packet.Amount ||
                item.CharacterId != Session.Character.CharacterId)
            {
                SendStateChanged();
                return;
            }

            var request = new BazaarPriceChangeDTO
            {
                OperationId = Guid.NewGuid(),
                BazaarItemId = listing.BazaarItemId,
                SellerAccountId = Session.Account.AccountId,
                SellerCharacterId = Session.Character.CharacterId,
                BazaarItemInstanceId = listing.ItemInstanceId,
                ItemVNum = item.ItemVNum,
                Amount = item.Amount,
                ExpectedPrice = listing.Price,
                NewPrice = packet.Price,
                MaximumGold = ServerManager.Instance.Configuration.MaxGold
            };

            BazaarPriceChangeResult result = BazaarPriceChangeService.Instance.Commit(request);
            if (result != BazaarPriceChangeResult.Success &&
                result != BazaarPriceChangeResult.AlreadyCommitted)
            {
                SendFailure(result);
                return;
            }

            listing.Price = packet.Price;
            RefreshBazaarCache(listing);
            UpdatePersonalCache(listing);

            Session.SendPacket(Session.Character.GenerateSay(
                string.Format(Language.Instance.GetMessageFromKey("OBJECT_MOD_IN_BAZAAR"), listing.Price), 10));
            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                string.Format(Language.Instance.GetMessageFromKey("OBJECT_MOD_IN_BAZAAR"), listing.Price), 0));

            new RefreshPersonalListPacketHandler(Session)
                .RefreshPersonalBazarList(new CSListPacket());

            Logger.LogUserEvent("BAZAAR_MOD_COMMIT", Session.GenerateIdentity(),
                $"OperationId={request.OperationId} BazaarId={listing.BazaarItemId} " +
                $"ItemInstanceId={listing.ItemInstanceId} VNum={item.ItemVNum} Amount={item.Amount} " +
                $"OldPrice={request.ExpectedPrice} NewPrice={request.NewPrice}");
        }

        private bool CanUseBazaar()
        {
            if (DateTime.Now < Session.Character.BazaarActionTimer.LastModAction.AddSeconds(2))
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                    "You need to wait a few seconds before changing the price again."));
                return false;
            }

            Session.Character.BazaarActionTimer.LastModAction = DateTime.Now;

            if (ServerManager.Instance.InShutdown ||
                Session.Character.InExchangeOrTrade ||
                Session.Character.HasShopOpened)
            {
                return false;
            }

            if (!KeepAliveClient.IsBazaarOnline())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                    "The bazaar server is offline. Please inform a staff member."));
                return false;
            }

            if (!Session.Character.CanUseNosBazaar())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                    Language.Instance.GetMessageFromKey("INFO_BAZAAR")));
                return false;
            }

            return true;
        }

        private static void RefreshBazaarCache(BazaarItemDTO listing)
        {
            try
            {
                BazaarClient.InsertOrUpdateBazaar(new InsertOrUpdateBazaarItemCommand
                {
                    BazaarItem = listing,
                    RefreshOnly = true
                });
            }
            catch (Exception exception)
            {
                Logger.Error($"Unable to refresh bazaar cache for item {listing.BazaarItemId}.", exception);
            }
        }

        private void UpdatePersonalCache(BazaarItemDTO listing)
        {
            if (Session.Character.BazaarItems.ContainsKey(listing.BazaarItemId))
            {
                Session.Character.BazaarItems[listing.BazaarItemId] = listing;
            }
        }

        private void SendFailure(BazaarPriceChangeResult result)
        {
            switch (result)
            {
                case BazaarPriceChangeResult.InvalidPrice:
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey("PRICE_EXCEEDED"), 0));
                    break;

                case BazaarPriceChangeResult.MissingSchema:
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                        "The bazaar price database migration is missing. Please contact an administrator."));
                    break;

                default:
                    SendStateChanged();
                    break;
            }
        }

        private void SendStateChanged()
        {
            Session.SendPacket(UserInterfaceHelper.GenerateModal(
                Language.Instance.GetMessageFromKey("STATE_CHANGED"), 1));
        }
    }
}
