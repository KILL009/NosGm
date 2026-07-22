using NosGm.Configuration;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Command
{
    public class ConfigurationHandler : IPacketHandler
    {
        #region Instantiation

        public ConfigurationHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Configuration(ConfigurationPacket configurationPacket)
        {
           if (configurationPacket?.Type != null)
            {
                switch (configurationPacket.Type)
                {
                    case "Bazaar":
                        if (GameConfiguration.BazaarEnabled)
                        {
                            GameConfiguration.BazaarEnabled = false;
                            MessageExtension.SendGrey(Session, "The Bazaar has been deactivated");
                        }
                        else
                        {
                            GameConfiguration.BazaarEnabled = true;
                            MessageExtension.SendGrey(Session, "The Bazaar has been activated");
                        }
                        break;
                }
            }
        }

        #endregion
    }
}