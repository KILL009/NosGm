using NosGm.SCS.Communication.Scs.Communication.EndPoints;

namespace NosGm.SCS.Communication.Scs.Client
{
    public static class ScsClientFactory
    {
        public static IScsClient CreateClient(ScsEndPoint endpoint) => endpoint.CreateClient();

        public static IScsClient CreateClient(string endpointAddress)
        {
            return ScsClientFactory.CreateClient(ScsEndPoint.CreateEndPoint(endpointAddress));
        }
    }
}
