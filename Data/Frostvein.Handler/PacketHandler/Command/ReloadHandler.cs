using Frostvein.Packets.Packets.CommandPackets;

using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using System.Threading.Tasks;
using Frostvein.GameObject.Plugin.Load;


namespace Frostvein.Handler.PacketHandler.Command
{
    public class ReloadHandler : IPacketHandler
    {
        #region Instantiation

        public ReloadHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Reload(ReloadPacket reloadPacket)
        {

            Parallel.ForEach(Session.CurrentMapInstance.Sessions, sess => sess.Character.AddBuff(new Buff(27, 10), sess.Character.BattleEntity));

            LoadService.Reload();
            MapInstance newMapInstance = ServerManager.ResetMapInstance(Session.CurrentMapInstance);

            Parallel.ForEach(Session.CurrentMapInstance.Sessions, sess => sess.Character.RemoveBuff(27));
            Parallel.ForEach(Session.CurrentMapInstance.Sessions, sess => sess.ReceivePacket("$Unstuck"));

            //LOGGER("[Reload] Reload successfull");
            Session.SendPacket(Session.Character.GenerateSay("[Reload]: Reload successfull", 10));
        }

        #endregion
    }
}