using System;
using System.Globalization;
using System.Text;

namespace NosGm.Master.Library.Interface
{
    /// <summary>
    /// Strict parser for the shared NoS0576/NoS0577 body observed in current Gameforge clients.
    /// It preserves and validates the two spaces before InstallationId and the vertical-tab byte
    /// between CountryId and ClientVersion.
    /// </summary>
    public static class GameforgeLoginPacketParser
    {
        public const int MaximumPacketLength = 8192;
        public const int MaximumTokenLength = 4096;
        public const byte MaximumCountryId = 8;

        public static bool TryParse(string rawPacket, out GameforgeLoginPayload payload, out string errorCode)
        {
            payload = null;
            errorCode = null;

            if (string.IsNullOrEmpty(rawPacket) || rawPacket.Length > MaximumPacketLength)
            {
                errorCode = "InvalidLength";
                return false;
            }

            if (rawPacket.IndexOf('\r') >= 0 || rawPacket.IndexOf('\n') >= 0 || rawPacket.IndexOf('\0') >= 0)
            {
                errorCode = "UnexpectedControlCharacter";
                return false;
            }

            int headerSeparator = rawPacket.IndexOf(' ');
            if (headerSeparator <= 0)
            {
                errorCode = "MissingHeaderSeparator";
                return false;
            }

            string header = rawPacket.Substring(0, headerSeparator);
            if (!string.Equals(header, "NoS0576", StringComparison.Ordinal) &&
                !string.Equals(header, "NoS0577", StringComparison.Ordinal))
            {
                errorCode = "UnsupportedHeader";
                return false;
            }

            int tokenStart = headerSeparator + 1;
            int doubleSpace = rawPacket.IndexOf("  ", tokenStart, StringComparison.Ordinal);
            if (doubleSpace < tokenStart)
            {
                errorCode = "MissingDoubleSpace";
                return false;
            }

            string authToken = rawPacket.Substring(tokenStart, doubleSpace - tokenStart);
            if (!IsSupportedAuthToken(authToken))
            {
                errorCode = "InvalidAuthToken";
                return false;
            }

            string tail = rawPacket.Substring(doubleSpace + 2);
            string[] parts = tail.Split(new[] { ' ' }, StringSplitOptions.None);
            if (parts.Length != 5)
            {
                errorCode = "InvalidFieldCount";
                return false;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                {
                    errorCode = "UnexpectedEmptyField";
                    return false;
                }
            }

            if (!Guid.TryParse(parts[0], out Guid installationId))
            {
                errorCode = "InvalidInstallationId";
                return false;
            }

            if (!IsHex(parts[1], 8, 8))
            {
                errorCode = "InvalidRandomHex";
                return false;
            }

            string countryAndVersion = parts[2];
            int verticalTabIndex = countryAndVersion.IndexOf('\v');
            if (verticalTabIndex <= 0 ||
                verticalTabIndex != countryAndVersion.LastIndexOf('\v') ||
                verticalTabIndex == countryAndVersion.Length - 1)
            {
                errorCode = "MissingVerticalTab";
                return false;
            }

            string countryText = countryAndVersion.Substring(0, verticalTabIndex);
            string versionText = countryAndVersion.Substring(verticalTabIndex + 1);
            if (!byte.TryParse(countryText, NumberStyles.None, CultureInfo.InvariantCulture, out byte countryId) ||
                countryId > MaximumCountryId)
            {
                errorCode = "InvalidCountryId";
                return false;
            }

            if (!Version.TryParse(versionText, out Version clientVersion) ||
                clientVersion.Build < 0 || clientVersion.Revision < 0)
            {
                errorCode = "InvalidClientVersion";
                return false;
            }

            if (!byte.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out byte unknownConstant) ||
                unknownConstant != 0)
            {
                errorCode = "InvalidConstant";
                return false;
            }

            if (!IsHex(parts[4], 32, 32))
            {
                errorCode = "InvalidClientMd5";
                return false;
            }

            payload = new GameforgeLoginPayload
            {
                Header = header,
                AuthToken = authToken,
                InstallationId = installationId,
                RandomHex = parts[1].ToUpperInvariant(),
                CountryId = countryId,
                ClientVersion = clientVersion,
                UnknownConstant = unknownConstant,
                ClientMd5 = parts[4].ToUpperInvariant()
            };
            return true;
        }

        public static bool IsSupportedAuthToken(string authToken)
        {
            return TryNormalizeAuthToken(authToken, out _);
        }

        /// <summary>
        /// Produces the stable lookup representation used by Master. A raw GUID and the
        /// ASCII-hex form used by older clients normalize to the same value. New opaque
        /// hexadecimal tokens remain supported as uppercase hexadecimal.
        /// </summary>
        public static bool TryNormalizeAuthToken(string authToken, out string normalizedToken)
        {
            normalizedToken = null;
            if (string.IsNullOrWhiteSpace(authToken) || authToken.Length > MaximumTokenLength)
            {
                return false;
            }

            if (Guid.TryParse(authToken, out Guid rawGuid))
            {
                normalizedToken = rawGuid.ToString("D");
                return true;
            }

            if (authToken.Length < 32 || authToken.Length % 2 != 0 ||
                !IsHex(authToken, authToken.Length, authToken.Length))
            {
                return false;
            }

            byte[] decodedBytes = new byte[authToken.Length / 2];
            for (int i = 0; i < decodedBytes.Length; i++)
            {
                decodedBytes[i] = byte.Parse(
                    authToken.Substring(i * 2, 2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture);
            }

            string decodedText = Encoding.ASCII.GetString(decodedBytes);
            if (Guid.TryParse(decodedText, out Guid decodedGuid))
            {
                normalizedToken = decodedGuid.ToString("D");
                return true;
            }

            normalizedToken = authToken.ToUpperInvariant();
            return true;
        }

        public static bool TryGetCulture(byte countryId, out string culture)
        {
            switch (countryId)
            {
                case 0:
                    culture = "en";
                    return true;
                case 1:
                    culture = "de";
                    return true;
                case 2:
                    culture = "fr";
                    return true;
                case 3:
                    culture = "it";
                    return true;
                case 4:
                    culture = "pl";
                    return true;
                case 5:
                    culture = "es";
                    return true;
                case 6:
                    culture = "cs";
                    return true;
                case 7:
                    culture = "ru";
                    return true;
                case 8:
                    culture = "tr";
                    return true;
                default:
                    culture = null;
                    return false;
            }
        }

        private static bool IsHex(string value, int minimumLength, int maximumLength)
        {
            if (value == null || value.Length < minimumLength || value.Length > maximumLength)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                bool valid = character >= '0' && character <= '9' ||
                             character >= 'a' && character <= 'f' ||
                             character >= 'A' && character <= 'F';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
