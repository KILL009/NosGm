using Frostvein.SCS.Communication.Scs.Communication.EndPoints;

namespace Frostvein.SCS.Communication.Scs.Client
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
