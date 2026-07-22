using NosGm.Extension.GameExtension.Packet;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Miniland
{
    public class UseobjPacketHandler : IPacketHandler
    {
        #region Instantiation

        public UseobjPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void UseMinilandObject(UseobjPacket packet)
        {
            var client =
                ServerManager.Instance.Sessions.FirstOrDefault(s =>
                    s.Character?.Miniland == Session.Character.MapInstance);
            var minilandObjectItem =
                client?.Character.Inventory.LoadBySlotAndType(packet.Slot, InventoryType.Miniland);
            if (minilandObjectItem != null)
            {
                var minilandObject =
                    client.Character.MinilandObjects.Find(s => s.ItemInstanceId == minilandObjectItem.Id);
                if (minilandObject != null)
                {
                    if (true) return; // Disable this when warehouses are working fine lol

                    if (!minilandObjectItem.Item.IsMinilandObject)
                    {
                        var game = (byte)(minilandObject.ItemInstance.Item.EquipmentSlot == 0
                            ? 4 + minilandObject.ItemInstance.ItemVNum % 10
                            : (int)minilandObject.ItemInstance.Item.EquipmentSlot / 3);
                        const bool full = false;
                        Session.SendPacket(
                            $"mlo_info {(client == Session ? 1 : 0)} {minilandObjectItem.ItemVNum} {packet.Slot} {Session.Character.MinilandPoint} {(minilandObjectItem.DurabilityPoint < 1000 ? 1 : 0)} {(full ? 1 : 0)} 0 {game.GetMinilandMaxPoint()[0]} {game.GetMinilandMaxPoint()[0] + 1} {game.GetMinilandMaxPoint()[1]} {game.GetMinilandMaxPoint()[1] + 1} {game.GetMinilandMaxPoint()[2]} {game.GetMinilandMaxPoint()[2] + 2} {game.GetMinilandMaxPoint()[3]} {game.GetMinilandMaxPoint()[3] + 1} {game.GetMinilandMaxPoint()[4]} {game.GetMinilandMaxPoint()[4] + 1} {game.GetMinilandMaxPoint()[5]}");
                    }
                    else
                    {
                        Session.SendPacket(Session.Character.GenerateStashAll());
                    }
                }
            }
        }

        #endregion
    }
}