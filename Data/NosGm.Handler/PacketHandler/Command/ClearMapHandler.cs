using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System.Linq;
using NosGm.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Command
{
    public class ClearMapHandler : IPacketHandler
    {
        #region Instantiation

        public ClearMapHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task ClearMap(ClearMapPacket clearMapPacket)
        {
            if (clearMapPacket != null && Session.HasCurrentMapInstance)
            {
                ////Session.AddLogsCmd(clearMapPacket);
                foreach (var monster in Session.CurrentMapInstance.Monsters.Where(s => s.ShouldRespawn != true))
                {
                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster,
                        monster.MapMonsterId));
                    monster.SetDeathStatement();
                    Session.CurrentMapInstance.RemoveMonster(monster);
                }

                foreach (var drop in Session.CurrentMapInstance.DroppedList.GetAllItems())
                {
                    Session.CurrentMapInstance.Broadcast(StaticPacketHelper.Out(UserType.Object, drop.TransportId));
                    Session.CurrentMapInstance.DroppedList.Remove(drop.TransportId);
                }

                    MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ClearMapPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}