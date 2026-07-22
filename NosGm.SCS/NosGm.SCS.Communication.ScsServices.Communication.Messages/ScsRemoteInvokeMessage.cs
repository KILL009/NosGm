using NosGm.SCS.Communication.Scs.Communication.Messages;
using System;

namespace NosGm.SCS.Communication.ScsServices.Communication.Messages
{
    [Serializable]
    public class ScsRemoteInvokeMessage : ScsMessage
    {
        public string ServiceClassName { get; set; }

        public string MethodName { get; set; }

        public object[] Parameters { get; set; }

        public override string ToString()
        {
            return string.Format("ScsRemoteInvokeMessage: {0}.{1}(...)", (object)this.ServiceClassName, (object)this.MethodName);
        }
    }
}
