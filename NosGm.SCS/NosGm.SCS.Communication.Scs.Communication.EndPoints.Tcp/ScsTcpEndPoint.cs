using NosGm.SCS.Communication.Scs.Client;
using NosGm.SCS.Communication.Scs.Client.Tcp;
using NosGm.SCS.Communication.Scs.Server;
using NosGm.SCS.Communication.Scs.Server.Tcp;
using System;

namespace NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp
{
    public sealed class ScsTcpEndPoint : ScsEndPoint
    {
        public string IpAddress { get; set; }

        public int TcpPort { get; private set; }

        public ScsTcpEndPoint(int tcpPort) => this.TcpPort = tcpPort;

        public ScsTcpEndPoint(string ipAddress, int port)
        {
            this.IpAddress = ipAddress;
            this.TcpPort = port;
        }

        public ScsTcpEndPoint(string address)
        {
            string[] strArray = address.Trim().Split(':');
            this.IpAddress = strArray[0].Trim();
            this.TcpPort = Convert.ToInt32(strArray[1].Trim());
        }

        internal override IScsServer CreateServer() => (IScsServer)new ScsTcpServer(this);

        internal override IScsClient CreateClient() => (IScsClient)new ScsTcpClient(this);

        public override string ToString()
        {
            if (string.IsNullOrEmpty(this.IpAddress))
                return "tcp://" + (object)this.TcpPort;
            return "tcp://" + this.IpAddress + ":" + (object)this.TcpPort;
        }
    }
}
