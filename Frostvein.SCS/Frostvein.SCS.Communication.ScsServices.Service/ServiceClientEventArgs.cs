using System;

namespace Frostvein.SCS.Communication.ScsServices.Service
{
    public class ServiceClientEventArgs : EventArgs
    {
        public IScsServiceClient Client { get; private set; }

        public ServiceClientEventArgs(IScsServiceClient client) => this.Client = client;
    }
}
