using Frostvein.Core.Networking.Communication.Scs.Communication.EndPoints.Tcp;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Frostvein.Core.Networking.Communication.Scs.Communication.Channels.Tcp
{
    /// <summary>
    ///     This class is used to listen and accept incoming TCP connection requests on a TCP port.
    /// </summary>
    public class TcpConnectionListener : ConnectionListenerBase
    {
        #region Instantiation

        /// <summary>
        ///     Creates a new TcpConnectionListener for given endpoint.
        /// </summary>
        /// <param name="endPoint">The endpoint address of the server to listen incoming connections</param>
        public TcpConnectionListener(ScsTcpEndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        #endregion

        #region Members

        /// <summary>
        ///     The endpoint address of the server to listen incoming connections.
        /// </summary>
        private readonly ScsTcpEndPoint _endPoint;

        /// <summary>
        ///     Server socket to listen incoming connection requests.
        /// </summary>
        private TcpListener _listenerSocket;

        /// <summary>
        ///     A flag to control thread's running
        /// </summary>
        private volatile bool _running;

        /// <summary>
        ///     The thread to listen socket
        /// </summary>
        private Thread _thread;

        #endregion

        #region Methods

        /// <summary>
        ///     Starts listening incoming connections.
        /// </summary>
        public override void Start()
        {
            StartSocket();
            _running = true;
            _thread = new Thread(ListenThread);
            _thread.Start();
        }

        /// <summary>
        ///     Stops listening incoming connections.
        /// </summary>
        public override void Stop()
        {
            _running = false;
            StopSocket();
        }

        /// <summary>
        ///     Entrance point of the thread. This method is used by the thread to listen incoming requests.
        /// </summary>
        private void ListenThread()
        {
            TcpCommunicationChannel tcpChannel = null;
            while (_running)
            {
                try
                {
                    var clientSocket = _listenerSocket.AcceptSocket();
                    if (clientSocket.Connected)
                    {
                        tcpChannel = new TcpCommunicationChannel(clientSocket);
                        OnCommunicationChannelConnected(tcpChannel);
                    }
                }
                catch (Exception)
                {
                    // Disconnect, wait for a while and connect again.
                    StopSocket();
                    Thread.Sleep(1000);
                    if (!_running)
                    {
                        return;
                    }

                    try
                    {
                        StartSocket();
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }
                }
            }

            tcpChannel.Dispose();
        }

        /// <summary>
        ///     Starts listening socket.
        /// </summary>
        private void StartSocket()
        {
            _listenerSocket = new TcpListener(_endPoint.IpAddress ?? IPAddress.Any, _endPoint.TcpPort);
            _listenerSocket.Start();
        }

        /// <summary>
        ///     Stops listening socket.
        /// </summary>
        private void StopSocket()
        {
            try
            {
                _listenerSocket.Stop();
            }
            catch (Exception)
            {
                // do nothing
            }
        }

        #endregion
    }
}