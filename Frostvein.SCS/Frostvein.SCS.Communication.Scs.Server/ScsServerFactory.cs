using Frostvein.SCS.Communication.Scs.Communication.EndPoints;

namespace Frostvein.SCS.Communication.Scs.Server
{
    public static class ScsServerFactory
    {
        public static IScsServer CreateServer(ScsEndPoint endPoint) => endPoint.CreateServer();
    }
}
