using System;

namespace Frostvein.SCS.Communication.ScsServices.Service
{
    public abstract class ScsService
    {
        [ThreadStatic]
        private static IScsServiceClient _currentClient;

        protected internal IScsServiceClient CurrentClient
        {
            get
            {
                return ScsService._currentClient != null ? ScsService._currentClient : throw new Exception("Client channel can not be obtained. CurrentClient property must be called by the thread which runs the service method.");
            }
            internal set => ScsService._currentClient = value;
        }
    }
}
