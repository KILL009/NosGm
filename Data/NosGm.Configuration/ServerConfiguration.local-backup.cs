using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace NosGm.Configuration
{
    public static class ServerConfiguration
    {
        private const int MinimumSecretLength = 32;
        private const int MaximumSecretLength = 4096;
        private static int _environmentOverridesApplied;

        public static string IPAddress = "127.0.0.1";
        public static string MasterServerPort = "4545";
        public static string WorldServerPort = "1337";
        public static string LoginServerPort = "4000";
        public static string GlacernonServerPort = "5100";

        public static string DatabaseConnection = "Data Source=localhost;Initial Catalog=NosGm;Integrated Security=true";

        public static bool GameVersionRequired = false;
        public static string GameVersion = "0.9.3.3255";
        public static string GameforgeClientMd5 = "gF6fr0AwMBpDsxheB1Itzf831hM4LKop";

        public static string Language = "uk";
        public static string ServerName = "Sumeria";
        public static string ServerGroup = "Sumeria";
        public static byte SessionLimit = 100;

        public static string MasterAuthKey = "NosGmServerMain2032NosGm";
        public static string AuthServiceKey = "b6DWVLLsoQhWdEynpRWV18Duu2oYNWpu";
        public static string GameforgeTicketIssuerKey = "";
        public static string GameforgeTicketConsumerKey = "";
        public static string MallAuthKey = "";
        public static string MallBaseURL = "";
        public static string MallAPIKey = "";

        public static bool AutoReboot = true;
        public static bool UseOldCrypto = false;
        public static bool LoginUsesPrehashedSha512 = true;
        public static bool EnableGameforgeTokenLogin = false;
        public static int GameforgeAuthTicketTtlSeconds = 120;
        public static int GameforgeWorldPermitTtlSeconds = 120;
        public static bool EnableLauncherAuthBridge = false;
        public static string LauncherAuthBridgePrefix = "http://127.0.0.1:8081/";
        public static int LauncherAuthBridgeAttemptWindowSeconds = 60;
        public static int LauncherAuthBridgeMaxAttemptsPerWindow = 10;
        public static bool StartAllRegionalLoginPorts = true;
        public static bool StartGlacernonAutomaticly = false;
        public static bool StartAllChannelsAutomaticly = true;
        public static bool AllChannelsStarted = false;
        public static bool MaintenanceMode = false;
        public static bool Port5100MaintenanceMode = false;

        static ServerConfiguration()
        {
            ApplyEnvironmentOverrides();
        }

        public static void ApplyEnvironmentOverrides()
        {
            if (Interlocked.Exchange(ref _environmentOverridesApplied, 1) != 0)
            {
                return;
            }

            MasterAuthKey = ReadSecret("NOSGM_MASTER_AUTH_KEY", MasterAuthKey);
            AuthServiceKey = ReadSecret("NOSGM_AUTH_SERVICE_KEY", AuthServiceKey);
            GameforgeTicketIssuerKey = ReadSecret("NOSGM_GAMEFORGE_TICKET_ISSUER_KEY", GameforgeTicketIssuerKey, true);
            GameforgeTicketConsumerKey = ReadSecret("NOSGM_GAMEFORGE_TICKET_CONSUMER_KEY", GameforgeTicketConsumerKey, true);

            EnableGameforgeTokenLogin = ReadBoolean(
                "NOSGM_ENABLE_GAMEFORGE_TOKEN_LOGIN",
                EnableGameforgeTokenLogin);
            EnableLauncherAuthBridge = ReadBoolean(
                "NOSGM_ENABLE_LAUNCHER_AUTH_BRIDGE",
                EnableLauncherAuthBridge);
            StartAllRegionalLoginPorts = ReadBoolean(
                "NOSGM_START_ALL_REGIONAL_LOGIN_PORTS",
                StartAllRegionalLoginPorts);

            LauncherAuthBridgePrefix = ReadListenerPrefix(
                "NOSGM_LAUNCHER_AUTH_BRIDGE_PREFIX",
                LauncherAuthBridgePrefix);
            GameforgeAuthTicketTtlSeconds = ReadInteger(
                "NOSGM_GAMEFORGE_AUTH_TICKET_TTL_SECONDS",
                GameforgeAuthTicketTtlSeconds,
                15,
                600);
            GameforgeWorldPermitTtlSeconds = ReadInteger(
                "NOSGM_GAMEFORGE_WORLD_PERMIT_TTL_SECONDS",
                GameforgeWorldPermitTtlSeconds,
                15,
                600);
            LauncherAuthBridgeAttemptWindowSeconds = ReadInteger(
                "NOSGM_LAUNCHER_AUTH_ATTEMPT_WINDOW_SECONDS",
                LauncherAuthBridgeAttemptWindowSeconds,
                10,
                600);
            LauncherAuthBridgeMaxAttemptsPerWindow = ReadInteger(
                "NOSGM_LAUNCHER_AUTH_MAX_ATTEMPTS_PER_WINDOW",
                LauncherAuthBridgeMaxAttemptsPerWindow,
                1,
                100);

            ValidateModernLoginConfiguration();
        }

        private static void ValidateModernLoginConfiguration()
        {
            if (EnableLauncherAuthBridge && !EnableGameforgeTokenLogin)
            {
                throw new InvalidOperationException(
                    "NOSGM_ENABLE_LAUNCHER_AUTH_BRIDGE requires NOSGM_ENABLE_GAMEFORGE_TOKEN_LOGIN=true.");
            }

            if (!EnableGameforgeTokenLogin)
            {
                return;
            }

            var secrets = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["NOSGM_MASTER_AUTH_KEY"] = MasterAuthKey,
                ["NOSGM_AUTH_SERVICE_KEY"] = AuthServiceKey,
                ["NOSGM_GAMEFORGE_TICKET_ISSUER_KEY"] = GameforgeTicketIssuerKey,
                ["NOSGM_GAMEFORGE_TICKET_CONSUMER_KEY"] = GameforgeTicketConsumerKey
            };

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> secret in secrets)
            {
                if (string.IsNullOrWhiteSpace(secret.Value) || secret.Value.Length < MinimumSecretLength)
                {
                    throw new InvalidOperationException(
                        secret.Key + " must contain at least " + MinimumSecretLength + " characters when modern Login is enabled.");
                }

                if (!seen.Add(secret.Value))
                {
                    throw new InvalidOperationException(
                        "Modern Login secrets must be distinct. Duplicate value detected at " + secret.Key + ".");
                }
            }
        }

        private static string ReadSecret(string name, string currentValue, bool allowEmpty = false)
        {
            string configuredValue = Environment.GetEnvironmentVariable(name);
            if (configuredValue == null)
            {
                return currentValue;
            }

            if (configuredValue.Length > MaximumSecretLength ||
                configuredValue.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0 ||
                (!allowEmpty && string.IsNullOrWhiteSpace(configuredValue)) ||
                !string.Equals(configuredValue, configuredValue.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(name + " contains an invalid secret value.");
            }

            return configuredValue;
        }

        private static bool ReadBoolean(string name, bool currentValue)
        {
            string configuredValue = Environment.GetEnvironmentVariable(name);
            if (configuredValue == null)
            {
                return currentValue;
            }

            if (bool.TryParse(configuredValue, out bool parsedValue))
            {
                return parsedValue;
            }

            if (configuredValue == "1")
            {
                return true;
            }

            if (configuredValue == "0")
            {
                return false;
            }

            throw new InvalidOperationException(name + " must be true, false, 1 or 0.");
        }

        private static int ReadInteger(string name, int currentValue, int minimum, int maximum)
        {
            string configuredValue = Environment.GetEnvironmentVariable(name);
            if (configuredValue == null)
            {
                return currentValue;
            }

            if (!int.TryParse(configuredValue, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedValue) ||
                parsedValue < minimum ||
                parsedValue > maximum)
            {
                throw new InvalidOperationException(
                    name + " must be an integer between " + minimum + " and " + maximum + ".");
            }

            return parsedValue;
        }

        private static string ReadListenerPrefix(string name, string currentValue)
        {
            string configuredValue = Environment.GetEnvironmentVariable(name);
            if (configuredValue == null)
            {
                return currentValue;
            }

            configuredValue = configuredValue.Trim();
            if (!Uri.TryCreate(configuredValue, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment) ||
                uri.AbsolutePath != "/" ||
                (uri.Scheme == Uri.UriSchemeHttp && !uri.IsLoopback))
            {
                throw new InvalidOperationException(
                    name + " must be an absolute HTTP(S) listener root. Plain HTTP is allowed only on loopback.");
            }

            return uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
                ? uri.AbsoluteUri
                : uri.AbsoluteUri + "/";
        }
    }
}
