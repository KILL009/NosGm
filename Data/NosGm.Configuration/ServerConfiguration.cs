namespace NosGm.Configuration
{
    public static class ServerConfiguration
    {
        public static string IPAddress = "127.0.0.1";
        public static string MasterServerPort = "4545";
        public static string WorldServerPort = "1337";
        public static string LoginServerPort = "4000";
        public static string GlacernonServerPort = "5100";

        public static string DatabaseConnection = "Data Source=localhost;Initial Catalog=NosGm;Integrated Security=true";

        public static bool GameVersionRequired = false;
        public static string GameVersion = "0.9.3.3254";

        public static string Language = "uk";
        public static string ServerName = "Sumeria";
        public static string ServerGroup = "Sumeria";
        public static byte SessionLimit = 100;

        public static string MasterAuthKey = "NosGmServerMain2032NosGm";
        public static string AuthServiceKey = "AuthServiceKey";
        public static string MallAuthKey = "";
        public static string MallBaseURL = "";
        public static string MallAPIKey = "";

        public static bool AutoReboot = true;
        public static bool UseOldCrypto = false;
        public static bool StartGlacernonAutomaticly = false;
        public static bool StartAllChannelsAutomaticly = true;
        public static bool AllChannelsStarted = false;
        public static bool MaintenanceMode = false;
        public static bool Port5100MaintenanceMode = false;
    }
}