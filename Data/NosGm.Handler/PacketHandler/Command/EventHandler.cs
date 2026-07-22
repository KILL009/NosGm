using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Event;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Plugin.Event;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Command
{
    public class EventHandler : IPacketHandler
    {
        #region Instantiation

        public EventHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void StartEvent(EventPacket eventPacket)
        {
            if (eventPacket != null)
            {
                if (eventPacket.LvlBracket >= 0)
                    GameEventHandler.GenerateEvent(eventPacket.EventType, eventPacket.LvlBracket);
                else
                    GameEventHandler.GenerateEvent(eventPacket.EventType);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(EventPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}