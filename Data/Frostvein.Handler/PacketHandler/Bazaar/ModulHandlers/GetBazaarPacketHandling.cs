using Frostvein.Configuration;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.HttpClients;
using Frostvein.GameObject.Modules.Bazaar.Commands;
using Frostvein.GameObject.Modules.Bazaar.Queries;
using Frostvein.GameObject.Networking;
using Frostvein.Handler.World.Bazaar;
using System;

namespace Frostvein.Handler.Bazaar
{
    public class GetBazaarPacketHandling : IPacketHandler
    {
        #region Instantiation

        public GetBazaarPacketHandling(ClientSession session) => Session = session;

        private static readonly KeepAliveClient KeepAliveClient = KeepAliveClient.Instance;
        private static readonly BazaarHttpClient _bazaarClient = BazaarHttpClient.Instance;

        #endregion Instantiation

        #region Properties

        private ClientSession Session { get; }

        private bool CanUseBazaar(long bazaarId)
        {
            if (DateTime.Now < Session.Character.BazaarActionTimer.LastBuyAction.AddSeconds(2))
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo("You need to wait a few seconds before buying an item again."));
                return false;
            }

            Session.Character.BazaarActionTimer.LastBuyAction = DateTime.Now;

            if (!KeepAliveClient.IsBazaarOnline())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo("Uh oh, it looks like the bazaar server is offline ! Please inform a staff member about it as soon as possible !"));
                return false;
            }

            //if (_bazaarClient.GetItemState(new GetStateQuery { Id = bazaarId }))
            //{
            //    Session.SendPacket(UserInterfaceHelper.GenerateInfo("An error occurred while trying to update this item."));
            //    return false;
            //}

            return true;
        }

        #endregion Properties

        #region Methods

        public void GetBazaar(CScalcPacket packet)
        {
            if (!CanUseBazaar(packet.BazaarId))
            {
                return;
            }

            var bazaarItem = _bazaarClient.GetBazaarItem(new GetBazaarItemQuery() { Id = packet.BazaarId });

            //if (!_bazaarClient.SetItemState(new SetStateCommand { Id = packet.BazaarId }))
            //{
            //    Session.SendPacket(UserInterfaceHelper.GenerateInfo("An error occurred while trying to get this item from the bazaar."));
            //    return;
            //}

            if (Session.Character == null || Session.Character.InExchangeOrTrade)
            {
                return;
            }

            if (ServerManager.Instance.InShutdown)
            {
                return;
            }

            if (bazaarItem == null)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateBazarRecollect(0, 0, 0, 0, 0, "None"));
                _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = packet.BazaarId });
                return;
            }
            var bazaarItemInstance = DAOFactory.ItemInstanceDAO.LoadById(bazaarItem.ItemInstanceId);

            if (bazaarItemInstance == null)
            {
                _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = packet.BazaarId });
                return;
            }

            var itemInstance = new ItemInstance(bazaarItemInstance);

            if (bazaarItem.SellerId != Session.Character.CharacterId)
            {
                _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = packet.BazaarId });
                return;
            }

            if ((bazaarItem.DateStart.AddHours(bazaarItem.Duration).AddDays(bazaarItem.MedalUsed ? 30 : 7) - DateTime.Now).TotalMinutes <= 0)
            {
                _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = packet.BazaarId });
                return;
            }

            var soldAmount = bazaarItem.Amount - itemInstance.Amount;
            var taxes = bazaarItem.MedalUsed ? 0 : (long)(bazaarItem.Price * 0.10 * soldAmount);
            var price = (bazaarItem.Price * soldAmount) - taxes;

            var name = itemInstance.Item?.Name ?? "None";

            if (itemInstance.Amount != 0 && !Session.Character.Inventory.CanAddItem(itemInstance.ItemVNum))
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE")));
                Session.SendPacket(UserInterfaceHelper.GenerateBazarRecollect(bazaarItem.Price, 0, bazaarItem.Amount, 0, 0, name));
                _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = packet.BazaarId });
                return;
            }

            long Result = Session.Character.Gold + price;
            if (Result > GameConfiguration.MaxGold)
            {
                Session.SendPacket("msg 4 You have reached the Gold Limit");
                Session.SendPacket(UserInterfaceHelper.GenerateBazarRecollect(bazaarItem.Price, 0, bazaarItem.Amount, 0, 0, name));
                _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = packet.BazaarId });
                return;
            }

            Session.Character.Gold += price;
            Session.SendPacket(Session.Character.GenerateGold());
            Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("REMOVE_FROM_BAZAAR"), price), 10));

            // Edit this soo we dont generate new guid every single time we take
            // something out.
            if (itemInstance.Amount != 0)
            {
                var newItemInstance = itemInstance.DeepCopy();
                newItemInstance.Id = Guid.NewGuid();
                newItemInstance.Type = newItemInstance.Item.Type;

                newItemInstance.ShellEffects.AddRange(DAOFactory.ShellEffectDAO.LoadByEquipmentSerialId(itemInstance.Id));
                newItemInstance.ShellEffects.ForEach(s => s.EquipmentSerialId = newItemInstance.Id);
                newItemInstance.ShellEffects.ForEach(s => DAOFactory.ShellEffectDAO.InsertOrUpdate(s));

                Session.Character.Inventory.AddToInventory(newItemInstance);
            }

            Session.SendPacket(UserInterfaceHelper.GenerateBazarRecollect(bazaarItem.Price, soldAmount, bazaarItem.Amount, taxes, price, name));
            if (Session.Character.BazaarItems.ContainsKey(packet.BazaarId))
            {
                Session.Character.BazaarItems.TryRemove(packet.BazaarId, out _);
            }

            Logger.LogUserEvent("BAZAAR_REMOVE", Session.GenerateIdentity(), $"BazaarId: {packet.BazaarId}, IId: {itemInstance.Id} VNum: {itemInstance.ItemVNum} Amount: {bazaarItem.Amount} RemainingAmount: {itemInstance.Amount} Price: {bazaarItem.Price}");

            var item = ServerManager.GetItem(bazaarItemInstance.ItemVNum);

            if (_bazaarClient.GetBazaarItem(new GetBazaarItemQuery() { Id = bazaarItem.BazaarItemId }) != null)
            {
                _bazaarClient.DeleteBazaarItem(new DeleteBazaarItemCommand() { Id = bazaarItem.BazaarItemId });
            }

            DAOFactory.ItemInstanceDAO.Delete(itemInstance.Id);
            Session.Character.Inventory.RemoveItemFromInventory(itemInstance.Id, itemInstance.Amount);
            new RefreshPersonalListPacketHandler(Session).RefreshPersonalBazarList(new CSListPacket());
            _bazaarClient.DeleteItemState(new DeleteStateCommand { Id = packet.BazaarId });
            Session.SendPacket("rc_reg 1");
        }
    }
}

#endregion Methods
