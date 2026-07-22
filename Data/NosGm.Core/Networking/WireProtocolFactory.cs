using NosGm.Core.Networking.Communication.Scs.Communication.Protocols;

namespace NosGm.Core
{
    public class WireProtocolFactory<EncryptorT> : IScsWireProtocolFactory where EncryptorT : CryptographyBase
    {
        #region Methods

        public IScsWireProtocol CreateWireProtocol() => new WireProtocol();

        #endregion
    }
}