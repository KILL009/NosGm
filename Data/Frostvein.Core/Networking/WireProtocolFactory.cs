using Frostvein.Core.Networking.Communication.Scs.Communication.Protocols;

namespace Frostvein.Core
{
    public class WireProtocolFactory<EncryptorT> : IScsWireProtocolFactory where EncryptorT : CryptographyBase
    {
        #region Methods

        public IScsWireProtocol CreateWireProtocol() => new WireProtocol();

        #endregion
    }
}