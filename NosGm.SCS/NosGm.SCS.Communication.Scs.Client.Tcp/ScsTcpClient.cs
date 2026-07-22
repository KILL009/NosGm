using NosGm.SCS.Communication.Scs.Communication.Channels;
using NosGm.SCS.Communication.Scs.Communication.Channels.Tcp;
using NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using System.Net;

namespace NosGm.SCS.Communication.Scs.Client.Tcp
{
    internal class ScsTcpClient : ScsClientBase
    {
        private readonly ScsTcpEndPoint _serverEndPoint;

        public ScsTcpClient(ScsTcpEndPoint serverEndPoint) => this._serverEndPoint = serverEndPoint;

        protected override ICommunicationChannel CreateCommunicationChannel()
        {
            return (ICommunicationChannel)new TcpCommunicationChannel(TcpHelper.ConnectToServer(!this.IsStringIp(this._serverEndPoint.IpAddress) ? (EndPoint)new DnsEndPoint(this._serverEndPoint.IpAddress, this._serverEndPoint.TcpPort) : (EndPoint)new IPEndPoint(IPAddress.Parse(this._serverEndPoint.IpAddress), this._serverEndPoint.TcpPort), this.ConnectTimeout));
        }

        private bool IsStringIp(string address)
        {
            IPAddress address1 = (IPAddress)null;
            return IPAddress.TryParse(address, out address1);
        }
    }
}
