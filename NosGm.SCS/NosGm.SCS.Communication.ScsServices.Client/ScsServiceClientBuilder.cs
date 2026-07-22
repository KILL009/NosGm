using NosGm.SCS;
using NosGm.SCS.Communication.Scs.Communication.EndPoints;

namespace NosGm.SCS.Communication.ScsServices.Client
{
    public class ScsServiceClientBuilder
    {
        public static IScsServiceClient<T> CreateClient<T>(ScsEndPoint endpoint, object clientObject = null) where T : class
        {
            return Management.CheckLicence() ? (IScsServiceClient<T>)new ScsServiceClient<T>(endpoint.CreateClient(), clientObject) : (IScsServiceClient<T>)null;
        }

        public static IScsServiceClient<T> CreateClient<T>(string endpointAddress, object clientObject = null) where T : class
        {
            return Management.CheckLicence() ? ScsServiceClientBuilder.CreateClient<T>(ScsEndPoint.CreateEndPoint(endpointAddress), clientObject) : (IScsServiceClient<T>)null;
        }
    }
}
