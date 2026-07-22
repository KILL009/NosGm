using MongoDB.Driver.Linq;
using NosGm.Configuration;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Core.Networking.Communication.Scs.Server;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using System;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Command
{
    public class UsercountHandler : IPacketHandler
    {
        #region Instantiation

        public UsercountHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        private const string Spacer = "-------------------";

        #endregion

        #region Methods

        public void Usercount(UsercountPacket usercountPacket)
        {
            //TODO: Add Usercount

            foreach (string message in CommunicationServiceClient.Instance.RetrieveServerStatistics(usercountPacket.isStart))
            {
                Session.SendPacket(Session.Character.GenerateSay($"{Spacer}\n{message}\n{Spacer}", 13));
            }
        }

        #endregion
    }
}