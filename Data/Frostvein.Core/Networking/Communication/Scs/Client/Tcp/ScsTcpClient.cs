using Frostvein.Core.Networking.Communication.Scs.Communication.Channels;
using Frostvein.Core.Networking.Communication.Scs.Communication.Channels.Tcp;
using Frostvein.Core.Networking.Communication.Scs.Communication.EndPoints.Tcp;
using System.Net;
using System.Net.Sockets;

namespace Frostvein.Core.Networking.Communication.Scs.Client.Tcp
{
    /// <summary>
    ///     This class is used to communicate with server over TCP/IP protocol.
    /// </summary>
    public class ScsTcpClient : ScsClientBase
    {
        #region Instantiation

        /// <summary>
        ///     Creates a new ScsTcpClient object.
        /// </summary>
        /// <param name="serverEndPoint">The endpoint address to connect to the server</param>
        /// <param name="existingSocketInformation">The existing socket information.</param>
        public ScsTcpClient(ScsTcpEndPoint serverEndPoint, SocketInformation? existingSocketInformation)
        {
            _serverEndPoint = serverEndPoint;
            _existingSocketInformation = existingSocketInformation;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Creates a communication channel using ServerIpAddress and ServerPort.
        /// </summary>
        /// <returns>Ready communication channel to communicate</returns>
        protected override ICommunicationChannel CreateCommunicationChannel()
        {
            Socket socket;

            if (_existingSocketInformation.HasValue)
            {
                socket = new Socket(_existingSocketInformation.Value);
                _existingSocketInformation = null;
            }
            else
            {
                socket = TcpHelper.ConnectToServer(new IPEndPoint(_serverEndPoint.IpAddress, _serverEndPoint.TcpPort),
                    ConnectTimeout);
            }

            return new TcpCommunicationChannel(socket);
        }

        #endregion

        #region Members

        /// <summary>
        ///     The endpoint address of the server.
        /// </summary>
        private readonly ScsTcpEndPoint _serverEndPoint;

        /// <summary>
        ///     The existing socket information or <c>null</c>.
        /// </summary>
        private SocketInformation? _existingSocketInformation;

        #endregion
    }
}