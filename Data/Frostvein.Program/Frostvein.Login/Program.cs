using log4net;
using Frostvein.Configuration;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Packets.Packets.ServerPackets;
using Frostvein.Core;
using Frostvein.DAL.EF.Helpers;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Handler.BasicPacket.Login;
using Frostvein.Master.Library.Client;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;

namespace Frostvein.Login
{
    public static class Program
    {
        #region Members

        private static bool _isDebug;

        private static int _port;

        #endregion

        #region Methods

        private static void PrintHeader()
        {
            Console.Title = "Frostvein - Login Server";
            const string text = @"

 ______ _____   ____   _____ _________      ________ _____ _   _ 
|  ____|  __ \ / __ \ / ____|__   __\ \    / /  ____|_   _| \ | |
| |__  | |__) | |  | | (___    | |   \ \  / /| |__    | | |  \| |
|  __| |  _  /| |  | |\___ \   | |    \ \/ / |  __|   | | | . ` |
| |    | | \ \| |__| |____) |  | |     \  /  | |____ _| |_| |\  |
|_|    |_|  \_\\____/|_____/   |_|      \/   |______|_____|_| \_|
                                                                                           
";
            string separator = new string('=', Console.WindowWidth);
            string logo = text.Split('\n').Select(s => string.Format("{0," + (Console.WindowWidth / 2 + s.Length / 2) + "}\n", s)).Aggregate("", (current, i) => current + i);
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
                    // initialize Logger
                    Logger.InitializeLogger(LogManager.GetLogger(typeof(Program)));

                    int port = Convert.ToInt32(ServerConfiguration.LoginServerPort);
                    var portArgIndex = Array.FindIndex(args, s => s == "--port");
                    if (portArgIndex != -1
                        && args.Length >= portArgIndex + 1
                        && int.TryParse(args[portArgIndex + 1], out port))
                    {
                        Console.WriteLine("Port override: " + port);
                    }

                    _port = port;
                    // initialize api
                    if (CommunicationServiceClient.Instance.Authenticate(ServerConfiguration.MasterAuthKey));
                    {
                        Logger.Info("Master Server API Communication has been initialized");
                    }

                    // initialize DB
                    if (!DataAccessHelper.Initialize())
                    {
                        Console.ReadKey();
                        return;
                    }

                    Logger.Info(Language.Instance.GetMessageFromKey("CONFIG_LOADED"));

                    try
                    {
                        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("General Error", ex);
                    }

                    try
                    {
                        // initialize PacketSerialization
                        PacketFactory.Initialize<WalkPacket>();
                        PacketFactory.Initialize<WalkPacket>();
                        PacketFactory.Initialize<FrostveinEntryPointPacket>();
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

                        var networkManager = new NetworkManager<LoginCryptography>(ServerConfiguration.IPAddress, port,
                            typeof(LoginPacketHandler), typeof(LoginCryptography), false);
                        AntiSpamModule.Instance.RunBlacklistTask();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogEventError("INITIALIZATION_EXCEPTION", "General Error Server", ex);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogEventError("INITIALIZATION_EXCEPTION", "General Error", ex);
                    Console.ReadKey();
                }
            }
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Log.Error("Crash", (Exception)e.ExceptionObject);
            try
            {
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Login server crashed", ex);
            }

            Logger.Debug("Login Server crashed! Rebooting gracefully...");
            Process.Start("Frostvein.Login.exe", $"--nomsg --port {_port}");
            Environment.Exit(1);
        }

        #endregion
    }
}