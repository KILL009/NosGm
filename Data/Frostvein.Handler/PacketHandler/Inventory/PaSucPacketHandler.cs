using Frostvein.Core;
using Frostvein.GameObject;
using System.Threading.Tasks;
using Frostvein.GameObject.Packets.ClientPackets;
using Frostvein.Domain;
using Frostvein.Packets.Packets.ClientPackets;
using System;
using static System.Collections.Specialized.BitVector32;

namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class PaSucPacketHandler : IPacketHandler
    {
        #region Instantiation

        public PaSucPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ShowFairyEnchantment(PaSucPacket paSucPacket)
        {
            string[] packetsplit = null;
            if (byte.TryParse(packetsplit[9], out var islot))
            {
                var wearInstance = Session.Character.Inventory.LoadBySlotAndType(islot, InventoryType.Equipment);
                if (wearInstance.FairyLevel != 0)
                {
                    string packetAddition = "";
                    foreach (var enchantment in wearInstance.FairyEnchantments)
                    {
                        packetAddition += $"{enchantment.FirstData}.{enchantment.SecondData}.{enchantment.ThirdData} ";
                    }
                    Session.SendPacket($"pa_suc 1 {wearInstance.FairyLevel} {packetAddition}");
                }
            }
        }

        #endregion
    }
}