using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class BIPacketHandler : IPacketHandler
    {
        #region Instantiation

        public BIPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void AskToDelete(BIPacket bIPacket)
        {
            if (bIPacket != null)
            {
                switch (bIPacket.Option)
                {
                    case null:
                        Session.SendPacket(UserInterfaceHelper.GenerateDialog($"#b_i^{(byte)bIPacket.InventoryType}^{bIPacket.Slot}^1 #b_i^0^0^5 {Language.Instance.GetMessageFromKey("ASK_TO_DELETE")}"));
                        break;

                    case 1:
                        Session.SendPacket(UserInterfaceHelper.GenerateDialog($"#b_i^{(byte)bIPacket.InventoryType}^{bIPacket.Slot}^2 #b_i^{(byte)bIPacket.InventoryType}^{bIPacket.Slot}^5 {Language.Instance.GetMessageFromKey("SURE_TO_DELETE")}"));
                        break;

                    case 2:
                        if (Session.Character.InExchangeOrTrade || bIPacket.InventoryType == InventoryType.Bazaar)
                        {
                            return;
                        }

                        var delInstance = Session.Character.Inventory.LoadBySlotAndType(bIPacket.Slot, bIPacket.InventoryType);
                        Session.Character.DeleteItem(bIPacket.InventoryType, bIPacket.Slot);

                        if (delInstance != null)
                        {
                            Logger.LogUserEvent("ITEM_DELETE", Session.GenerateIdentity(),
                               $"[DeleteItem]IIId: {delInstance.Id} ItemVNum: {delInstance.ItemVNum} Amount: {delInstance.Amount} MapId: {Session.CurrentMapInstance?.Map.MapId} MapX: {Session.Character.PositionX} MapY: {Session.Character.PositionY}");
                        }

                        break;
                }
            }
        }

        #endregion
    }
}