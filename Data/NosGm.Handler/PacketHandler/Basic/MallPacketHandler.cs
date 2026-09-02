using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.Packets.Packets.ClientPackets;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NosGm.Handler.PacketHandler.Basic
{
    /// <summary>
    /// Turns the built-in NosTale mall button into a short-lived signed NosMall login.
    /// HTTPS is mandatory outside loopback development.
    /// </summary>
    public sealed class MallPacketHandler : IPacketHandler
    {
        private const int TicketLifetimeSeconds = 90;
        private const int MinimumSecretLength = 32;

        public MallPacketHandler(ClientSession session)
        {
            Session = session;
        }

        public ClientSession Session { get; }

        public void OpenMall(MallPacket packet)
        {
            if (packet == null || Session?.Character == null)
            {
                return;
            }

            var baseUrlText = (Environment.GetEnvironmentVariable("NOSGM_SHOP_URL") ?? string.Empty).Trim().TrimEnd('/');
            var secret = Environment.GetEnvironmentVariable("NOSGM_SHOP_TICKET_SECRET") ?? string.Empty;

            Uri baseUri;
            if (!TryValidateShopUri(baseUrlText, out baseUri) || !IsStrongEnoughSecret(secret))
            {
                Logger.Error("NosMall is not configured. NOSGM_SHOP_URL must use HTTPS outside loopback and NOSGM_SHOP_TICKET_SECRET must contain at least 32 non-trivial characters.");
                Session.SendPacket(UserInterfaceHelper.GenerateMsg("NosMall is temporarily unavailable.", 0));
                return;
            }

            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(TicketLifetimeSeconds).ToUnixTimeSeconds();
            var nonceBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonceBytes);
            }

            var payload = string.Join("|",
                "v1",
                Session.Character.AccountId.ToString(CultureInfo.InvariantCulture),
                Session.Character.CharacterId.ToString(CultureInfo.InvariantCulture),
                expiresAt.ToString(CultureInfo.InvariantCulture),
                ToHex(nonceBytes));

            string signature;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                signature = Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            }

            var ticket = Base64Url(Encoding.UTF8.GetBytes(payload)) + "." + signature;
            var mallUri = new Uri(baseUri, "/auth/game?ticket=" + Uri.EscapeDataString(ticket));
            Session.SendPacket("mallurl " + mallUri.AbsoluteUri);
        }

        private static bool TryValidateShopUri(string value, out Uri uri)
        {
            uri = null;
            if (!Uri.TryCreate(value, UriKind.Absolute, out var candidate) || !string.IsNullOrEmpty(candidate.UserInfo))
            {
                return false;
            }

            var isHttps = string.Equals(candidate.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            var isLoopbackHttp = string.Equals(candidate.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                                 && (string.Equals(candidate.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(candidate.Host, "localhost", StringComparison.OrdinalIgnoreCase));
            if (!isHttps && !isLoopbackHttp)
            {
                return false;
            }

            uri = candidate;
            return true;
        }

        private static bool IsStrongEnoughSecret(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                   && value.Length >= MinimumSecretLength
                   && value.Length <= 512
                   && value.IndexOf("changeme", StringComparison.OrdinalIgnoreCase) < 0
                   && value.IndexOf("password", StringComparison.OrdinalIgnoreCase) < 0
                   && value.Distinct().Count() >= 12;
        }

        private static string Base64Url(byte[] value)
        {
            return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string ToHex(byte[] value)
        {
            var result = new StringBuilder(value.Length * 2);
            foreach (var b in value)
            {
                result.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }
    }
}
