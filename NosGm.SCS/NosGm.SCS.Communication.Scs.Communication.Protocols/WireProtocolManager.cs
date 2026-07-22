using NosGm.SCS.Communication.Scs.Communication.Protocols.BinarySerialization;

namespace NosGm.SCS.Communication.Scs.Communication.Protocols
{
    internal static class WireProtocolManager
    {
        public static IScsWireProtocolFactory GetDefaultWireProtocolFactory()
        {
            return (IScsWireProtocolFactory)new BinarySerializationProtocolFactory();
        }

        public static IScsWireProtocol GetDefaultWireProtocol()
        {
            return (IScsWireProtocol)new BinarySerializationProtocol();
        }
    }
}
