// SPDX-License-Identifier: MIT

namespace NosGm.Configuration
{
    internal static class GameConfiguration
    {
        public static int XPRate = 5;
        public static int HeroXPRate = 5;
        public static int DropRate = 1;
        public static int FairyXPRate = 10;
        public static int GoldRate = 2;
        public static int ReputationRate = 1;
        public static int JobLevelRate = 20;
    }

    internal static class ServerConfiguration
    {
        public static bool MaintenanceMode;
    }
}

namespace NosGm.Core
{
    internal static class Logger
    {
        public static void Info(string message)
        {
        }

        public static void Warn(string message)
        {
        }
    }
}
