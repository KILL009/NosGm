using MongoDB.Driver.Linq;
using Frostvein.Configuration;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Core.Networking.Communication.Scs.Server;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Characters.Events;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using System;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
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