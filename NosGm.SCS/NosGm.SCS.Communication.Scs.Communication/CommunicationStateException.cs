using System;
using System.Runtime.Serialization;

namespace NosGm.SCS.Communication.Scs.Communication
{
    [Serializable]
    public class CommunicationStateException : CommunicationException
    {
        public CommunicationStateException()
        {
        }

        public CommunicationStateException(
          SerializationInfo serializationInfo,
          StreamingContext context)
          : base(serializationInfo, context)
        {
        }

        public CommunicationStateException(string message)
          : base(message)
        {
        }

        public CommunicationStateException(string message, Exception innerException)
          : base(message, innerException)
        {
        }
    }
}
