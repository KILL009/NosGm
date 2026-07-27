using log4net;
using NosGm.Configuration;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Packets.Packets.ServerPackets;
using NosGm.Core;
using NosGm.DAL.EF.Helpers;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.Handler.BasicPacket.Login;
using NosGm.Master.Library.Client;
using System;
using System.Diagnostics;
using System.Linq;

namespace NosGm.Login
{
    public static class Program
    {
        private const int MinimumAuthServiceKeyLength = 32;

        private static bool _isDebug;

        private static int _port;

        private static void PrintHeader()
        {
            Console.Title = "NosGm - Login Server";
            const string text = @"

 ______ _____   ____   _____ _________      ________ _____ _   _ 
|  ____|  __ \ / __ \ / ____|__   __\ \    / /  ____|_   _| \ | |
| |__  | |__) | |  | | (___    | |   \ \  / /| |__    | | |  \| |
|  __| |  _  /| |  | |\___ \   | |    \ \/ / |  __|   | | | . ` |
| |    | | \ \| |__| |____) |  | |     \  /  | |____ _| |_| |\  |
|_|    |_|  \_\\____/|_____/   |_|      \/   |______|_____|_| \_|
                                                                                            
";
            string separator = new string('=', Console.WindowWidth);
            string logo = text.Split('\n')
                .Select(s => string.Format(
                    "{0," + (Console.WindowWidth / 2 + s.Length / 2) + "}\n",
                    s))
                .Aggregate("", (current, i) => current + i);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(separator + logo + separator);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Main(string[] args)
        {
            checked
            {
                try
                {
                    PrintHeader();
                    Logger.InitializeLogger(LogManager.GetLogger(typeof(Program)));

                    int port = Convert.ToInt32(ServerConfiguration.LoginServerPort);
                    int portArgIndex = Array.FindIndex(args, s => s == "--port");
                    if (portArgIndex >= 0 &&
                        portArgIndex + 1 < args.Length &&
                        int.TryParse(args[portArgIndex + 1], out int overriddenPort))
                    {
                        port = overriddenPort;
                        Console.WriteLine("Port override: " + port);
                    }

                    _port = port;

                    if (!CommunicationServiceClient.Instance.Authenticate(ServerConfiguration.MasterAuthKey))
                    {
                        throw new InvalidOperationException(
                            "Master communication authentication was rejected.");
                    }

                    if (ServerConfiguration.EnableGameforgeTokenLogin)
                    {
                        if (!IsSecureAuthServiceKey())
                        {
                            throw new InvalidOperationException(
                                "Gameforge token login requires a unique AuthServiceKey with at least 32 characters.");
                        }

                        if (!AuthentificationServiceClient.Instance.Authenticate(ServerConfiguration.AuthServiceKey))
                        {
                            throw new InvalidOperationException(
                                "Master authentication-ticket service rejected Login.");
                        }
                    }

                    Logger.Info(
                        $"Master services initialized | GameforgeTokenLogin={ServerConfiguration.EnableGameforgeTokenLogin}");

                    if (!DataAccessHelper.Initialize())
                    {
                        Console.ReadKey();
                        return;
                    }

                    Logger.Info(Language.Instance.GetMessageFromKey("CONFIG_LOADED"));

                    AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

                    PacketFactory.Initialize<WalkPacket>();
                    PacketFactory.Initialize<NosGmEntryPointPacket>();
                    PacketFactory.Initialize<UseSkillPacket>();
                    PacketFactory.Initialize<CBuyPacket>();
                    PacketFactory.Initialize<CreateFamilyPacket>();
                    PacketFactory.Initialize<BIPacket>();
                    PacketFactory.Initialize<SuctlPacket>();
                    PacketFactory.Initialize<AddObjPacket>();
                    PacketFactory.Initialize<BuyPacket>();
                    PacketFactory.Initialize<EscapePacket>();
                    PacketFactory.Initialize<CClosePacket>();
                    PacketFactory.Initialize<HelpPacket>();

                    var networkManager = new NetworkManager<LoginCryptography>(
                        ServerConfiguration.IPAddress,
                        port,
                        typeof(LoginPacketHandler),
                        typeof(LoginCryptography),
                        false);
                    AntiSpamModule.Instance.RunBlacklistTask();
                }
                catch (Exception ex)
                {
                    Logger.LogEventError("INITIALIZATION_EXCEPTION", "General Error", ex);
                    Console.ReadKey();
                }
            }
        }

        private static bool IsSecureAuthServiceKey()
        {
            string configuredKey = ServerConfiguration.AuthServiceKey;
            return !string.IsNullOrWhiteSpace(configuredKey) &&
                   configuredKey.Length >= MinimumAuthServiceKeyLength &&
                   !string.Equals(configuredKey, "AuthServiceKey", StringComparison.Ordinal);
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Log.Error("Crash", (Exception)e.ExceptionObject);
            Logger.Debug("Login Server crashed! Rebooting gracefully...");
            Process.Start("NosGm.Login.exe", $"--nomsg --port {_port}");
            Environment.Exit(1);
        }
    }
}
