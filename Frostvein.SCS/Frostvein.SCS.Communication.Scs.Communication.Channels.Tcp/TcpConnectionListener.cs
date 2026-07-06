using Frostvein.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Frostvein.SCS.Communication.Scs.Communication.Channels.Tcp
{
    internal class TcpConnectionListener : ConnectionListenerBase
    {
        private readonly ScsTcpEndPoint _endPoint;
        private TcpListener _listenerSocket;
        private Thread _thread;
        private volatile bool _running;

        public TcpConnectionListener(ScsTcpEndPoint endPoint) => this._endPoint = endPoint;

        public override void Start()
        {
            this.StartSocket();
            this._running = true;
            this._thread = new Thread(new ThreadStart(this.DoListenAsThread));
            this._thread.Start();
        }

        public override void Stop()
        {
            this._running = false;
            this.StopSocket();
        }

        private void StartSocket()
        {
            this._listenerSocket = new TcpListener(IPAddress.Any, this._endPoint.TcpPort);
            this._listenerSocket.Start();
        }

        private void StopSocket()
        {
            try
            {
                this._listenerSocket.Stop();
            }
            catch
            {
            }
        }

        private void DoListenAsThread()
        {
            while (this._running)
            {
                try
                {
                    Socket clientSocket = this._listenerSocket.AcceptSocket();
                    if (clientSocket.Connected)
                        this.OnCommunicationChannelConnected((ICommunicationChannel)new TcpCommunicationChannel(clientSocket));
                }
                catch
                {
                    this.StopSocket();
                    Thread.Sleep(1000);
                    if (!this._running)
                        break;
                    try
                    {
                        this.StartSocket();
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
