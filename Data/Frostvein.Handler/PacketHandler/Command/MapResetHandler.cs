using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class MapResetHandler : IPacketHandler
    {
        #region Instantiation

        public MapResetHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void MapReset(MapResetPacket mapResetPacket)
        {
            if (mapResetPacket != null)
            {
                if (Session.Character.IsChangingMapInstance) return;
                if (Session.CurrentMapInstance != null)
                {
                    //Session.AddLogsCmd(mapResetPacket);
                    var newMapInstance = ServerManager.ResetMapInstance(Session.CurrentMapInstance);

                    foreach (var sess in Session.CurrentMapInstance.Sessions)
                        ServerManager.Instance.ChangeMapInstance(sess.Character.CharacterId,
                            newMapInstance.MapInstanceId, sess.Character.PositionX, sess.Character.PositionY);
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(MapResetPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}