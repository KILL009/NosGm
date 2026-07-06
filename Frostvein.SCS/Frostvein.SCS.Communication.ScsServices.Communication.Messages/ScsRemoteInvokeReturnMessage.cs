using Frostvein.SCS.Communication.Scs.Communication.Messages;
using System;

namespace Frostvein.SCS.Communication.ScsServices.Communication.Messages
{
    [Serializable]
    public class ScsRemoteInvokeReturnMessage : ScsMessage
    {
        public object ReturnValue { get; set; }

        public ScsRemoteException RemoteException { get; set; }

        public override string ToString()
        {
            return string.Format("ScsRemoteInvokeReturnMessage: Returns {0}, Exception = {1}", this.ReturnValue, (object)this.RemoteException);
        }
    }
}
