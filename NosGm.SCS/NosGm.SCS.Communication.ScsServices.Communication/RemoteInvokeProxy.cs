using NosGm.SCS.Communication.Scs.Communication.Messages;
using NosGm.SCS.Communication.Scs.Communication.Messengers;
using NosGm.SCS.Communication.ScsServices.Communication.Messages;
using System;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace NosGm.SCS.Communication.ScsServices.Communication
{
    internal class RemoteInvokeProxy<TProxy, TMessenger> : RealProxy where TMessenger : IMessenger
    {
        private readonly RequestReplyMessenger<TMessenger> _clientMessenger;

        public RemoteInvokeProxy(RequestReplyMessenger<TMessenger> clientMessenger)
          : base(typeof(TProxy))
        {
            this._clientMessenger = clientMessenger;
        }

        public override IMessage Invoke(IMessage msg)
        {
            ScsRemoteInvokeReturnMessage invokeReturnMessage = null;

            if (!(msg is IMethodCallMessage mcm))
                return (IMessage)null;

            ScsRemoteInvokeMessage message = new ScsRemoteInvokeMessage()
            {
                ServiceClassName = typeof(TProxy).Name,
                MethodName = mcm.MethodName,
                Parameters = mcm.InArgs
            };

            invokeReturnMessage = null;

            if (message.ServiceClassName.EndsWith("Client"))
            {
                this._clientMessenger.SendMessage((IScsMessage)message);
            }
            else
            {
                var response = this._clientMessenger.SendMessageAndWaitForResponse((IScsMessage)message);
                if (response is ScsRemoteInvokeReturnMessage)
                {
                    invokeReturnMessage = (ScsRemoteInvokeReturnMessage)response;
                }
            }

            if (invokeReturnMessage == null)
            {
                return (IMessage)new ReturnMessage((object)null, (object[])null, 0, mcm.LogicalCallContext, mcm);
            }

            return invokeReturnMessage.RemoteException == null ?
                (IMessage)new ReturnMessage(invokeReturnMessage.ReturnValue, (object[])null, 0, mcm.LogicalCallContext, mcm) :
                (IMessage)new ReturnMessage((Exception)invokeReturnMessage.RemoteException, mcm);
        }
    }
}
