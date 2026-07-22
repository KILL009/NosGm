using System;

namespace NosGm.SCS.Communication.Scs.Server
{
    public class ServerClientEventArgs : EventArgs
    {
        public IScsServerClient Client { get; private set; }

        public ServerClientEventArgs(IScsServerClient client) => this.Client = client;
    }
}
