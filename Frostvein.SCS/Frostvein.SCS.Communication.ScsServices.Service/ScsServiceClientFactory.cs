using Frostvein.SCS.Communication.Scs.Communication.Messengers;
using Frostvein.SCS.Communication.Scs.Server;

namespace Frostvein.SCS.Communication.ScsServices.Service
{
    internal static class ScsServiceClientFactory
    {
        public static IScsServiceClient CreateServiceClient(
          IScsServerClient serverClient,
          RequestReplyMessenger<IScsServerClient> requestReplyMessenger)
        {
            return (IScsServiceClient)new ScsServiceClient(serverClient, requestReplyMessenger);
        }
    }
}
