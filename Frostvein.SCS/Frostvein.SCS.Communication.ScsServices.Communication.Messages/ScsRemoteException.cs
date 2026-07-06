using System;
using System.Runtime.Serialization;

namespace Frostvein.SCS.Communication.ScsServices.Communication.Messages
{
    [Serializable]
    public class ScsRemoteException : Exception
    {
        public ScsRemoteException()
        {
        }

        public ScsRemoteException(SerializationInfo serializationInfo, StreamingContext context)
          : base(serializationInfo, context)
        {
        }

        public ScsRemoteException(string message)
          : base(message)
        {
        }

        public ScsRemoteException(string message, Exception innerException)
          : base(message, innerException)
        {
        }
    }
}
