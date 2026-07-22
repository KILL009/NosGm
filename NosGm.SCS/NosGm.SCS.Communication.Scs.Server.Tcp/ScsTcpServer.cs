using NosGm.SCS.Communication.Scs.Communication.Channels;
using NosGm.SCS.Communication.Scs.Communication.Channels.Tcp;
using NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp;

namespace NosGm.SCS.Communication.Scs.Server.Tcp
{
    internal class ScsTcpServer : ScsServerBase
    {
        private readonly ScsTcpEndPoint _endPoint;

        public ScsTcpServer(ScsTcpEndPoint endPoint) => this._endPoint = endPoint;

        protected override IConnectionListener CreateConnectionListener()
        {
            return (IConnectionListener)new TcpConnectionListener(this._endPoint);
        }
    }
}
