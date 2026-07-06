using Frostvein.SCS;
using Frostvein.SCS.Communication.Scs.Communication.EndPoints;
using Frostvein.SCS.Communication.Scs.Server;

namespace Frostvein.SCS.Communication.ScsServices.Service
{
    public static class ScsServiceBuilder
    {
        public static IScsServiceApplication CreateService(ScsEndPoint endPoint)
        {
            return Management.CheckLicence() ? (IScsServiceApplication)new ScsServiceApplication(ScsServerFactory.CreateServer(endPoint)) : (IScsServiceApplication)null;
        }
    }
}
