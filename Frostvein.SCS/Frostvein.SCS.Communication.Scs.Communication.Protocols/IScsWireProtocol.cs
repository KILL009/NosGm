using Frostvein.SCS.Communication.Scs.Communication.Messages;
using System.Collections.Generic;

namespace Frostvein.SCS.Communication.Scs.Communication.Protocols
{
    public interface IScsWireProtocol
    {
        byte[] GetBytes(IScsMessage message);

        IEnumerable<IScsMessage> CreateMessages(byte[] receivedBytes);

        void Reset();
    }
}
