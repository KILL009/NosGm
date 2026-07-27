using System;

namespace NosGm.Master.Library.Interface
{
    /// <summary>
    /// Parsed, non-secret metadata from a NoS0576 or NoS0577 login packet.
    /// AuthToken is kept only long enough to consume its one-time ticket and must never be logged.
    /// </summary>
    public sealed class GameforgeLoginPayload
    {
        public string Header { get; internal set; }

        public string AuthToken { get; internal set; }

        public Guid InstallationId { get; internal set; }

        public string RandomHex { get; internal set; }

        public byte CountryId { get; internal set; }

        public Version ClientVersion { get; internal set; }

        public byte UnknownConstant { get; internal set; }

        public string ClientMd5 { get; internal set; }
    }
}