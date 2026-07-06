using Frostvein.Configuration;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
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