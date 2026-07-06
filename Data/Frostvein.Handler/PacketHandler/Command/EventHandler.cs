using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Event;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Plugin.Event;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Command
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