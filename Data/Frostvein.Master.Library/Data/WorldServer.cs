using Frostvein.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using Frostvein.SCS.Communication.ScsServices.Service;
using System;

namespace Frostvein.Master.Library.Data
{
    public class WorldServer
    {
        #region Instantiation

        public WorldServer(Guid id, ScsTcpEndPoint endpoint, int accountLimit, string worldGroup)
        {
            Id = id;
            Endpoint = endpoint;
            AccountLimit = accountLimit;
            WorldGroup = worldGroup;
        }

        #endregion

        #region Properties

        public int AccountLimit { get; set; }

        public int ChannelId { get; set; }

        public IScsServiceClient CommunicationServiceClient { get; set; }

        public IScsServiceClient ConfigurationServiceClient { get; set; }

        public ScsTcpEndPoint Endpoint { get; set; }

        public Guid Id { get; set; }

        public IScsServiceClient MailServiceClient { get; set; }

        public SerializableWorldServer Serializable { get; }

        public string WorldGroup { get; set; }

        #endregion
    }
}