using NosGm.SCS.Communication.Scs.Communication.Messengers;
using NosGm.SCS.Communication.Scs.Server;

namespace NosGm.SCS.Communication.ScsServices.Service
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
