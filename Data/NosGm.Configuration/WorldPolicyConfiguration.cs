using System;
using System.Collections.Generic;

namespace NosGm.Configuration
{
    /// <summary>
    /// Per-process gameplay policy. Start each World Server process with its own
    /// environment variables to expose independent PvE/PvP or zero-EXP channels.
    /// </summary>
    public static class WorldPolicyConfiguration
    {
        public static readonly string WorldMode = GetWorldMode();
        public static readonly bool DisableNormalExperience =
            GetEnvironmentBoolean("NOSGM_DISABLE_NORMAL_EXP");
        public static readonly bool DisableHeroExperience =
            GetEnvironmentBoolean("NOSGM_DISABLE_HERO_EXP");
        public static readonly bool AllowDedicatedPvpInPveWorld =
            GetEnvironmentBoolean("NOSGM_PVE_ALLOW_INSTANCED_PVP", true);

        public static readonly string ServerGroup =
            GetEnvironmentString("NOSGM_SERVER_GROUP", ServerConfiguration.ServerGroup);
        public static readonly string ServerName =
            GetEnvironmentString("NOSGM_SERVER_NAME", ServerConfiguration.ServerName);
        public static readonly string WorldPort =
            GetEnvironmentString("NOSGM_WORLD_PORT", ServerConfiguration.WorldServerPort);

        private static readonly HashSet<short> PvpSafeMapIds =
            ParseMapIds(GetEnvironmentString("NOSGM_PVP_SAFE_MAP_IDS", "1,145"));

        public static bool IsPveWorld =>
            string.Equals(WorldMode, "PVE", StringComparison.OrdinalIgnoreCase);

        public static bool IsPvpWorld =>
            string.Equals(WorldMode, "PVP", StringComparison.OrdinalIgnoreCase);

        public static bool IsPvpSafeMap(short mapId) => PvpSafeMapIds.Contains(mapId);

        private static string GetWorldMode()
        {
            string value = GetEnvironmentString("NOSGM_WORLD_MODE", "STANDARD")
                .Trim().ToUpperInvariant();

            if (value == "PVE" || value == "PVP")
            {
                return value;
            }

            return "STANDARD";
        }

        private static string GetEnvironmentString(string name, string defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        private static bool GetEnvironmentBoolean(string name, bool defaultValue = false)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        private static HashSet<short> ParseMapIds(string value)
        {
            var result = new HashSet<short>();
            foreach (string entry in value.Split(','))
            {
                if (short.TryParse(entry.Trim(), out short mapId))
                {
                    result.Add(mapId);
                }
            }

            return result;
        }
    }
}
