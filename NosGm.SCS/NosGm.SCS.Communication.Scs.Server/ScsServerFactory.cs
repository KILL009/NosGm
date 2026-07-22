using NosGm.SCS.Communication.Scs.Communication.EndPoints;

namespace NosGm.SCS.Communication.Scs.Server
{
    public static class ScsServerFactory
    {
        public static IScsServer CreateServer(ScsEndPoint endPoint) => endPoint.CreateServer();
    }
}
