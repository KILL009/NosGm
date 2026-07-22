using NosGm.SCS;
using NosGm.SCS.Communication.Scs.Communication.EndPoints;
using NosGm.SCS.Communication.Scs.Server;

namespace NosGm.SCS.Communication.ScsServices.Service
{
    public static class ScsServiceBuilder
    {
        public static IScsServiceApplication CreateService(ScsEndPoint endPoint)
        {
            return Management.CheckLicence() ? (IScsServiceApplication)new ScsServiceApplication(ScsServerFactory.CreateServer(endPoint)) : (IScsServiceApplication)null;
        }
    }
}
