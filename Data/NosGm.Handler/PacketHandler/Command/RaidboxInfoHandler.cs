using NosGm.Extension.Extension.Command;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;

namespace NosGm.Handler.PacketHandler.Command
{
    public class RaidboxInfoHandler : IPacketHandler
    {
        #region Instantiation

        public RaidboxInfoHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void GenerateInfo(RaidboxInfoPacket raidboxInfo)
        {
            switch (raidboxInfo.Type)
            {
                case "Cuby":
                    #region Cuby
                    string String = $"Raidbox: Mother Cuby\n\n";
                    foreach (RaidboxDTO item in DAOFactory.RaidboxDAO.LoadByItemVNum(9833))
                    {
                        Item ite = ServerManager.GetItem(item.ItemGeneratedVNum);
                        String += $"x{item.ItemGeneratedAmount} - {ite.Name}\n";
                    }

                    Session.SendPacket(UserInterfaceHelper.GenerateModal(String, 1));
                    #endregion
                    break;

                default:
                    break;
            }
        }

        #endregion
    }
}