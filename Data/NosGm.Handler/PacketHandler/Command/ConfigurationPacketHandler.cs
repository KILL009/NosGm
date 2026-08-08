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
using NosGm.Master.Library.Client;
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
                switch (configurationPacket.Type.ToLowerInvariant())
                {
                    case "bazaar":
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

                    case "grpcpulse":
                    case "grpc-pulse":
                        string diagnostic;
                        Func<bool> isWorldIsolated = () =>
                            ServerManager.Instance.Sessions.Count(session =>
                                session != null &&
                                session.Character != null) == 1;
                        bool passed;
                        if (!isWorldIsolated())
                        {
                            diagnostic = "world-not-isolated";
                            passed = false;
                        }
                        else
                        {
                            passed = ConfigurationServiceClient.Instance
                                .TryRunGrpcAcceptancePulse(
                                    ServerManager.Instance.Configuration,
                                    isWorldIsolated,
                                    out diagnostic);
                        }
                        MessageExtension.SendGrey(
                            Session,
                            passed
                                ? "Configuration gRPC acceptance pulse passed; the original values were restored."
                                : "Configuration gRPC acceptance pulse failed closed: " +
                                  (diagnostic ?? "unknown") + ".");
                        Logger.LogUserEvent(
                            "CONFIG_GRPC_ACCEPTANCE_PULSE",
                            Session.GenerateIdentity(),
                            "Result=" + (passed ? "Pass" : "Rejected") +
                            " Diagnostic=" + (diagnostic ?? "unknown"));
                        break;

                    default:
                        MessageExtension.SendGrey(
                            Session,
                            ConfigurationPacket.ReturnHelp());
                        break;
                }
            }
        }

        #endregion
    }
}
