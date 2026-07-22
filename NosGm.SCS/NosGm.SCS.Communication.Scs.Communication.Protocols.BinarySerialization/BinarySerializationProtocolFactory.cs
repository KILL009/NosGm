namespace NosGm.SCS.Communication.Scs.Communication.Protocols.BinarySerialization
{
    public class BinarySerializationProtocolFactory : IScsWireProtocolFactory
    {
        public IScsWireProtocol CreateWireProtocol()
        {
            return (IScsWireProtocol)new BinarySerializationProtocol();
        }
    }
}
