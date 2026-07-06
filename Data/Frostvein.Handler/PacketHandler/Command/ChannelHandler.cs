using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.GameObject;
using Frostvein.GameObject.Characters.Events;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class ChannelHandler : IPacketHandler
    {
        #region Instantiation

        public ChannelHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Channel(ChannelPacket channelPacket)
        {
            var ports = CommunicationServiceClient.Instance.GetPorts();
            if (!ports.Any(x => x.Item1 == channelPacket.ChannelID))
            {
                MessageExtension.SendRed(Session, "Something went wrong");
                return;
            }
                
            //Session.Character.ChangeChannel()
            Session.Character.Event.EmitEvent(new PlayerChangeChannelEvent("134.255.221.71", ports.FirstOrDefault(x => x.Item1 == (int)channelPacket.ChannelID).Item2, 3));
        }

        #endregion
    }
}