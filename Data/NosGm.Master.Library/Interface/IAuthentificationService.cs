using NosGm.Data;
using NosGm.SCS.Communication.ScsServices.Service;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace NosGm.Master.Library.Interface
{
    [ScsService(Version = "1.2.0.0")]
    public interface IAuthentificationService
    {
        /// <summary>
        /// Authenticates a trusted NosGM service client.
        /// </summary>
        bool Authenticate(string authKey);

        AccountDTO ValidateAccount(string userName, string passHash);

        CharacterDTO ValidateAccountAndCharacter(string userName, string characterName, string passHash);

        /// <summary>
        /// Registers a short-lived, one-use ticket obtained by an external authentication bridge.
        /// The raw token is hashed immediately and is never stored by Master.
        /// </summary>
        bool RegisterGameforgeAuthTicket(
            string accountName,
            string authToken,
            string installationId,
            byte countryId);

        /// <summary>
        /// Atomically consumes a previously registered ticket. Returns the local account name
        /// only when token, installation and country all match.
        /// </summary>
        string ConsumeGameforgeAuthTicket(
            string authToken,
            string installationId,
            byte countryId);
    }

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
            if (string.IsNullOrWhiteSpace(authToken) || authToken.Length > MaximumTokenLength)
            {
                return false;
            }

            if (Guid.TryParse(authToken, out _))
            {
                return true;
            }

            return authToken.Length >= 32 && authToken.Length % 2 == 0 &&
                   IsHex(authToken, authToken.Length, authToken.Length);
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

    /// <summary>
    /// Thread-safe, in-memory ticket store. Tokens are kept only as SHA-256 lookup keys,
    /// consumed atomically, bound to InstallationId and CountryId, and expire quickly.
    /// </summary>
    public sealed class GameforgeAuthTicketStore
    {
        private sealed class Ticket
        {
            public string AccountName { get; set; }
            public Guid InstallationId { get; set; }
            public byte CountryId { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
        }

        private readonly ConcurrentDictionary<string, Ticket> _tickets =
            new ConcurrentDictionary<string, Ticket>(StringComparer.Ordinal);

        private GameforgeAuthTicketStore()
        {
        }

        public static GameforgeAuthTicketStore Instance { get; } = new GameforgeAuthTicketStore();

        public int Count => _tickets.Count;

        public bool TryIssue(
            string accountName,
            string authToken,
            Guid installationId,
            byte countryId,
            TimeSpan lifetime)
        {
            if (string.IsNullOrWhiteSpace(accountName) || accountName.Length > 64 ||
                accountName.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '\v' }) >= 0 ||
                !GameforgeLoginPacketParser.IsSupportedAuthToken(authToken) ||
                installationId == Guid.Empty ||
                countryId > GameforgeLoginPacketParser.MaximumCountryId ||
                lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromMinutes(10))
            {
                return false;
            }

            RemoveExpired(DateTime.UtcNow);
            string key = ComputeTokenKey(authToken);
            return _tickets.TryAdd(key, new Ticket
            {
                AccountName = accountName,
                InstallationId = installationId,
                CountryId = countryId,
                ExpiresAtUtc = DateTime.UtcNow.Add(lifetime)
            });
        }

        public bool TryConsume(
            string authToken,
            Guid installationId,
            byte countryId,
            out string accountName)
        {
            accountName = null;
            if (!GameforgeLoginPacketParser.IsSupportedAuthToken(authToken) ||
                installationId == Guid.Empty ||
                countryId > GameforgeLoginPacketParser.MaximumCountryId)
            {
                return false;
            }

            string key = ComputeTokenKey(authToken);
            if (!_tickets.TryRemove(key, out Ticket ticket))
            {
                return false;
            }

            if (ticket.ExpiresAtUtc <= DateTime.UtcNow ||
                ticket.InstallationId != installationId ||
                ticket.CountryId != countryId)
            {
                return false;
            }

            accountName = ticket.AccountName;
            return true;
        }

        public void Clear()
        {
            _tickets.Clear();
        }

        private static string ComputeTokenKey(string authToken)
        {
            string normalized;
            if (Guid.TryParse(authToken, out Guid guid))
            {
                normalized = guid.ToString("D");
            }
            else
            {
                normalized = authToken.ToUpperInvariant();
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                return Convert.ToBase64String(sha256.ComputeHash(Encoding.ASCII.GetBytes(normalized)));
            }
        }

        private void RemoveExpired(DateTime nowUtc)
        {
            foreach (var pair in _tickets)
            {
                if (pair.Value.ExpiresAtUtc <= nowUtc)
                {
                    _tickets.TryRemove(pair.Key, out _);
                }
            }
        }
    }
}
