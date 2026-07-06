using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class ClassPackHandler : IPacketHandler
    {
        #region Instantiation

        public ClassPackHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ClassPack(ClassPackPacket classPackPacket)
        {
            if (classPackPacket != null)
            {
                if (classPackPacket.Class < 1 || classPackPacket.Class > 3)
                {
                    Session.SendPacket(Session.Character.GenerateSay("Invalid class", 11));
                    Session.SendPacket(Session.Character.GenerateSay(ClassPackPacket.ReturnHelp(), 10));
                    return;
                }
                switch (classPackPacket.Class)
                {
                    case 1:
                        Session.Character.Inventory.AddNewToInventory(4075);
                        Session.Character.Inventory.AddNewToInventory(4076);
                        Session.Character.Inventory.AddNewToInventory(4129);
                        Session.Character.Inventory.AddNewToInventory(4130);
                        Session.Character.Inventory.AddNewToInventory(4131);
                        Session.Character.Inventory.AddNewToInventory(4132);
                        Session.Character.Inventory.AddNewToInventory(1685, 999);
                        Session.Character.Inventory.AddNewToInventory(1686, 999);
                        Session.Character.Inventory.AddNewToInventory(5087, 999);
                        Session.Character.Inventory.AddNewToInventory(5203, 999);
                        Session.Character.Inventory.AddNewToInventory(5372, 999);
                        Session.Character.Inventory.AddNewToInventory(5431, 999);
                        Session.Character.Inventory.AddNewToInventory(5432, 999);
                        Session.Character.Inventory.AddNewToInventory(5498, 999);
                        Session.Character.Inventory.AddNewToInventory(5499, 999);
                        Session.Character.Inventory.AddNewToInventory(5553, 999);
                        Session.Character.Inventory.AddNewToInventory(5560, 999);
                        Session.Character.Inventory.AddNewToInventory(5591, 999);
                        Session.Character.Inventory.AddNewToInventory(5837, 999);
                        Session.Character.Inventory.AddNewToInventory(4875, 1, Upgrade: 14);
                        Session.Character.Inventory.AddNewToInventory(4873, 1, Upgrade: 14);
                        Session.Character.Inventory.AddNewToInventory(1012, 999);
                        Session.Character.Inventory.AddNewToInventory(1012, 999);
                        Session.Character.Inventory.AddNewToInventory(1244, 999);
                        Session.Character.Inventory.AddNewToInventory(1244, 999);
                        Session.Character.Inventory.AddNewToInventory(2072, 999);
                        Session.Character.Inventory.AddNewToInventory(2071, 999);
                        Session.Character.Inventory.AddNewToInventory(2070, 999);
                        Session.Character.Inventory.AddNewToInventory(2160, 999);
                        Session.Character.Inventory.AddNewToInventory(4138);
                        Session.Character.Inventory.AddNewToInventory(4146);
                        Session.Character.Inventory.AddNewToInventory(4142);
                        Session.Character.Inventory.AddNewToInventory(4150);
                        Session.Character.Inventory.AddNewToInventory(4353);
                        Session.Character.Inventory.AddNewToInventory(4124);
                        Session.Character.Inventory.AddNewToInventory(4172);
                        Session.Character.Inventory.AddNewToInventory(4183);
                        Session.Character.Inventory.AddNewToInventory(4187);
                        Session.Character.Inventory.AddNewToInventory(4283);
                        Session.Character.Inventory.AddNewToInventory(4285);
                        Session.Character.Inventory.AddNewToInventory(4177);
                        Session.Character.Inventory.AddNewToInventory(4179);
                        Session.Character.Inventory.AddNewToInventory(4244);
                        Session.Character.Inventory.AddNewToInventory(4252);
                        Session.Character.Inventory.AddNewToInventory(4256);
                        Session.Character.Inventory.AddNewToInventory(4248);
                        Session.Character.Inventory.AddNewToInventory(3116);
                        Session.Character.Inventory.AddNewToInventory(1277, 999);
                        Session.Character.Inventory.AddNewToInventory(1274, 999);
                        Session.Character.Inventory.AddNewToInventory(1280, 999);
                        Session.Character.Inventory.AddNewToInventory(2419, 999);
                        Session.Character.Inventory.AddNewToInventory(1914);
                        Session.Character.Inventory.AddNewToInventory(1296, 999);
                        Session.Character.Inventory.AddNewToInventory(5916, 999);
                        Session.Character.Inventory.AddNewToInventory(3001);
                        Session.Character.Inventory.AddNewToInventory(3003);
                        Session.Character.Inventory.AddNewToInventory(4490);
                        Session.Character.Inventory.AddNewToInventory(4699);
                        Session.Character.Inventory.AddNewToInventory(4099, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(900, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(907, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(908, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4883, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4889, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4895, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4371);
                        Session.Character.Inventory.AddNewToInventory(4353);
                        Session.Character.Inventory.AddNewToInventory(4277);
                        Session.Character.Inventory.AddNewToInventory(4309);
                        Session.Character.Inventory.AddNewToInventory(4271);
                        Session.Character.Inventory.AddNewToInventory(901, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(902, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(909, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(910, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4500, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4497, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4493, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4489, 1, Upgrade: 15);
                        break;

                    case 2:
                        Session.Character.Inventory.AddNewToInventory(4075);
                        Session.Character.Inventory.AddNewToInventory(4076);
                        Session.Character.Inventory.AddNewToInventory(4129);
                        Session.Character.Inventory.AddNewToInventory(4130);
                        Session.Character.Inventory.AddNewToInventory(4131);
                        Session.Character.Inventory.AddNewToInventory(4132);
                        Session.Character.Inventory.AddNewToInventory(1685, 999);
                        Session.Character.Inventory.AddNewToInventory(1686, 999);
                        Session.Character.Inventory.AddNewToInventory(5087, 999);
                        Session.Character.Inventory.AddNewToInventory(5203, 999);
                        Session.Character.Inventory.AddNewToInventory(5372, 999);
                        Session.Character.Inventory.AddNewToInventory(5431, 999);
                        Session.Character.Inventory.AddNewToInventory(5432, 999);
                        Session.Character.Inventory.AddNewToInventory(5498, 999);
                        Session.Character.Inventory.AddNewToInventory(5499, 999);
                        Session.Character.Inventory.AddNewToInventory(5553, 999);
                        Session.Character.Inventory.AddNewToInventory(5560, 999);
                        Session.Character.Inventory.AddNewToInventory(5591, 999);
                        Session.Character.Inventory.AddNewToInventory(5837, 999);
                        Session.Character.Inventory.AddNewToInventory(4875, 1, Upgrade: 14);
                        Session.Character.Inventory.AddNewToInventory(4873, 1, Upgrade: 14);
                        Session.Character.Inventory.AddNewToInventory(1012, 999);
                        Session.Character.Inventory.AddNewToInventory(1012, 999);
                        Session.Character.Inventory.AddNewToInventory(1244, 999);
                        Session.Character.Inventory.AddNewToInventory(1244, 999);
                        Session.Character.Inventory.AddNewToInventory(2072, 999);
                        Session.Character.Inventory.AddNewToInventory(2071, 999);
                        Session.Character.Inventory.AddNewToInventory(2070, 999);
                        Session.Character.Inventory.AddNewToInventory(2160, 999);
                        Session.Character.Inventory.AddNewToInventory(4138);
                        Session.Character.Inventory.AddNewToInventory(4146);
                        Session.Character.Inventory.AddNewToInventory(4142);
                        Session.Character.Inventory.AddNewToInventory(4150);
                        Session.Character.Inventory.AddNewToInventory(4353);
                        Session.Character.Inventory.AddNewToInventory(4124);
                        Session.Character.Inventory.AddNewToInventory(4172);
                        Session.Character.Inventory.AddNewToInventory(4183);
                        Session.Character.Inventory.AddNewToInventory(4187);
                        Session.Character.Inventory.AddNewToInventory(4283);
                        Session.Character.Inventory.AddNewToInventory(4285);
                        Session.Character.Inventory.AddNewToInventory(4177);
                        Session.Character.Inventory.AddNewToInventory(4179);
                        Session.Character.Inventory.AddNewToInventory(4244);
                        Session.Character.Inventory.AddNewToInventory(4252);
                        Session.Character.Inventory.AddNewToInventory(4256);
                        Session.Character.Inventory.AddNewToInventory(4248);
                        Session.Character.Inventory.AddNewToInventory(3116);
                        Session.Character.Inventory.AddNewToInventory(1277, 999);
                        Session.Character.Inventory.AddNewToInventory(1274, 999);
                        Session.Character.Inventory.AddNewToInventory(1280, 999);
                        Session.Character.Inventory.AddNewToInventory(2419, 999);
                        Session.Character.Inventory.AddNewToInventory(1914);
                        Session.Character.Inventory.AddNewToInventory(1296, 999);
                        Session.Character.Inventory.AddNewToInventory(5916, 999);
                        Session.Character.Inventory.AddNewToInventory(3001);
                        Session.Character.Inventory.AddNewToInventory(3003);
                        Session.Character.Inventory.AddNewToInventory(4490);
                        Session.Character.Inventory.AddNewToInventory(4699);
                        Session.Character.Inventory.AddNewToInventory(4099, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(900, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(907, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(908, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4885, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4890, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4897, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4372);
                        Session.Character.Inventory.AddNewToInventory(4310);
                        Session.Character.Inventory.AddNewToInventory(4354);
                        Session.Character.Inventory.AddNewToInventory(4279);
                        Session.Character.Inventory.AddNewToInventory(4273);
                        Session.Character.Inventory.AddNewToInventory(903, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(904, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(911, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(912, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4501, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4498, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4488, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4492, 1, Upgrade: 15);
                        break;

                    case 3:
                        Session.Character.Inventory.AddNewToInventory(4075);
                        Session.Character.Inventory.AddNewToInventory(4076);
                        Session.Character.Inventory.AddNewToInventory(4129);
                        Session.Character.Inventory.AddNewToInventory(4130);
                        Session.Character.Inventory.AddNewToInventory(4131);
                        Session.Character.Inventory.AddNewToInventory(4132);
                        Session.Character.Inventory.AddNewToInventory(1685, 999);
                        Session.Character.Inventory.AddNewToInventory(1686, 999);
                        Session.Character.Inventory.AddNewToInventory(5087, 999);
                        Session.Character.Inventory.AddNewToInventory(5203, 999);
                        Session.Character.Inventory.AddNewToInventory(5372, 999);
                        Session.Character.Inventory.AddNewToInventory(5431, 999);
                        Session.Character.Inventory.AddNewToInventory(5432, 999);
                        Session.Character.Inventory.AddNewToInventory(5498, 999);
                        Session.Character.Inventory.AddNewToInventory(5499, 999);
                        Session.Character.Inventory.AddNewToInventory(5553, 999);
                        Session.Character.Inventory.AddNewToInventory(5560, 999);
                        Session.Character.Inventory.AddNewToInventory(5591, 999);
                        Session.Character.Inventory.AddNewToInventory(5837, 999);
                        Session.Character.Inventory.AddNewToInventory(4875, 1, Upgrade: 14);
                        Session.Character.Inventory.AddNewToInventory(4873, 1, Upgrade: 14);
                        Session.Character.Inventory.AddNewToInventory(1012, 999);
                        Session.Character.Inventory.AddNewToInventory(1012, 999);
                        Session.Character.Inventory.AddNewToInventory(1244, 999);
                        Session.Character.Inventory.AddNewToInventory(1244, 999);
                        Session.Character.Inventory.AddNewToInventory(2072, 999);
                        Session.Character.Inventory.AddNewToInventory(2071, 999);
                        Session.Character.Inventory.AddNewToInventory(2070, 999);
                        Session.Character.Inventory.AddNewToInventory(2160, 999);
                        Session.Character.Inventory.AddNewToInventory(4138);
                        Session.Character.Inventory.AddNewToInventory(4146);
                        Session.Character.Inventory.AddNewToInventory(4142);
                        Session.Character.Inventory.AddNewToInventory(4150);
                        Session.Character.Inventory.AddNewToInventory(4353);
                        Session.Character.Inventory.AddNewToInventory(4124);
                        Session.Character.Inventory.AddNewToInventory(4172);
                        Session.Character.Inventory.AddNewToInventory(4183);
                        Session.Character.Inventory.AddNewToInventory(4187);
                        Session.Character.Inventory.AddNewToInventory(4283);
                        Session.Character.Inventory.AddNewToInventory(4285);
                        Session.Character.Inventory.AddNewToInventory(4177);
                        Session.Character.Inventory.AddNewToInventory(4179);
                        Session.Character.Inventory.AddNewToInventory(4244);
                        Session.Character.Inventory.AddNewToInventory(4252);
                        Session.Character.Inventory.AddNewToInventory(4256);
                        Session.Character.Inventory.AddNewToInventory(4248);
                        Session.Character.Inventory.AddNewToInventory(3116);
                        Session.Character.Inventory.AddNewToInventory(1277, 999);
                        Session.Character.Inventory.AddNewToInventory(1274, 999);
                        Session.Character.Inventory.AddNewToInventory(1280, 999);
                        Session.Character.Inventory.AddNewToInventory(2419, 999);
                        Session.Character.Inventory.AddNewToInventory(1914);
                        Session.Character.Inventory.AddNewToInventory(1296, 999);
                        Session.Character.Inventory.AddNewToInventory(5916, 999);
                        Session.Character.Inventory.AddNewToInventory(3001);
                        Session.Character.Inventory.AddNewToInventory(3003);
                        Session.Character.Inventory.AddNewToInventory(4490);
                        Session.Character.Inventory.AddNewToInventory(4699);
                        Session.Character.Inventory.AddNewToInventory(4099, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(900, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(907, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(908, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4887, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4892, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4899, 1, null, 7, 10);
                        Session.Character.Inventory.AddNewToInventory(4311);
                        Session.Character.Inventory.AddNewToInventory(4373);
                        Session.Character.Inventory.AddNewToInventory(4281);
                        Session.Character.Inventory.AddNewToInventory(4355);
                        Session.Character.Inventory.AddNewToInventory(4275);
                        Session.Character.Inventory.AddNewToInventory(905, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(906, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(913, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(914, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4502, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4499, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4491, 1, Upgrade: 15);
                        Session.Character.Inventory.AddNewToInventory(4487, 1, Upgrade: 15);
                        break;
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ClassPackPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}