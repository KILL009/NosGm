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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;

namespace NosGm.Login
{
    public static class Program
    {
        private const int MinimumGameforgeKeyLength = 32;

        private static readonly List<NetworkManager<LoginCryptography>> NetworkManagers =
            new List<NetworkManager<LoginCryptography>>();

        private static string _restartArguments = "--nomsg";

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
                        if (!HasSecureDistinctGameforgeKeys())
                        {
                            throw new InvalidOperationException(
                                "Gameforge token login requires distinct issuer and consumer keys with at least 32 characters each.");
                        }

                        if (!AuthentificationServiceClient.Instance.Authenticate(
                                ServerConfiguration.GameforgeTicketConsumerKey))
                        {
                            throw new InvalidOperationException(
                                "Master authentication-ticket service rejected Login as a ticket consumer.");
                        }
                    }

                    Logger.Info(
                        $"Master services initialized | GameforgeTokenLogin={ServerConfiguration.EnableGameforgeTokenLogin}");
                    if (!TryGetPortOverride(args, out bool hasPortOverride, out int overridePort))
                    {
                        Console.ReadKey();
                        return;
                    }

                    IReadOnlyCollection<int> loginPorts;
                    if (hasPortOverride)
                    {
                        loginPorts = new[] { overridePort };
                        _restartArguments = $"--nomsg --port {overridePort}";
                        Console.WriteLine("Port override: " + overridePort);
                    }
                    else if (ServerConfiguration.StartAllRegionalLoginPorts)
                    {
                        loginPorts = Enumerable.Range(
                                ClientRegionMap.BaseLoginPort,
                                ClientRegionMap.RegionCount)
                            .ToArray();
                        _restartArguments = "--nomsg";
                    }
                    else
                    {
                        loginPorts = new[] { Convert.ToInt32(ServerConfiguration.LoginServerPort) };
                        _restartArguments = "--nomsg";
                    }

                    if (!CommunicationServiceClient.Instance.Authenticate(ServerConfiguration.MasterAuthKey))
                    {
                        Logger.Error("Master Server API authentication failed");
                        Console.ReadKey();
                        return;
                    }

                    Logger.Info("Master Server API Communication has been initialized");

                    if (!DataAccessHelper.Initialize())
                    {
                        Console.ReadKey();
                        return;
                    }

                    Logger.Info(Language.Instance.GetMessageFromKey("CONFIG_LOADED"));

                    AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
                    try
                    {
                        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("General Error", ex);
                    }

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
                    foreach (int port in loginPorts)
                    {
                        if (!ClientRegionMap.TryResolveLoginPort(
                                port,
                                out byte regionType,
                                out string culture))
                        {
                            Logger.Error(
                                $"Unsupported Login port {port}. Expected {ClientRegionMap.BaseLoginPort}-" +
                                $"{ClientRegionMap.BaseLoginPort + ClientRegionMap.RegionCount - 1}.");
                            StopLoginServers();
                            Console.ReadKey();
                            return;
                        }

                        try
                        {
                            NetworkManagers.Add(new NetworkManager<LoginCryptography>(
                                ServerConfiguration.IPAddress,
                                port,
                                typeof(LoginPacketHandler),
                                typeof(LoginCryptography),
                                false));
                        }
                        catch (SocketException ex)
                        {
                            Logger.Error(
                                $"Unable to start Login listener | Port={port} RegionType={regionType} Culture={culture}",
                                ex);
                            StopLoginServers();
                            Console.ReadKey();
                            return;
                        }
                    }

                    AntiSpamModule.Instance.RunBlacklistTask();

                    if (loginPorts.Count == 1 &&
                        ClientRegionMap.TryResolveLoginPort(
                            loginPorts.First(),
                            out byte singleRegionType,
                            out string singleCulture))
                    {
                        Console.Title =
                            $"NosGm - Login Server [{singleCulture.ToUpperInvariant()} | {loginPorts.First()} | Region {singleRegionType}]";
                    }
                    else
                    {
                        Console.Title =
                            $"NosGm - Login Server [{ClientRegionMap.BaseLoginPort}-" +
                            $"{ClientRegionMap.BaseLoginPort + ClientRegionMap.RegionCount - 1}]";
                    }

                    Logger.Info(
                        $"Regional Login listeners started | Count={loginPorts.Count} " +
                        $"Ports={string.Join(",", loginPorts)}");
                }
                catch (Exception ex)
                {
                    Logger.LogEventError("INITIALIZATION_EXCEPTION", "General Error", ex);
                    StopLoginServers();
                    Console.ReadKey();
                }
            }
        }

        private static bool HasSecureDistinctGameforgeKeys()
        {
            string issuerKey = ServerConfiguration.GameforgeTicketIssuerKey;
            string consumerKey = ServerConfiguration.GameforgeTicketConsumerKey;
            return IsSecureGameforgeKey(issuerKey) &&
                   IsSecureGameforgeKey(consumerKey) &&
                   !string.Equals(issuerKey, consumerKey, StringComparison.Ordinal) &&
                   !string.Equals(issuerKey, ServerConfiguration.AuthServiceKey, StringComparison.Ordinal) &&
                   !string.Equals(consumerKey, ServerConfiguration.AuthServiceKey, StringComparison.Ordinal);
        }

        private static bool IsSecureGameforgeKey(string configuredKey)
        {
            return !string.IsNullOrWhiteSpace(configuredKey) &&
                   configuredKey.Length >= MinimumGameforgeKeyLength;
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
        private static void StopLoginServers()
        {
            foreach (NetworkManager<LoginCryptography> networkManager in NetworkManagers)
            {
                try
                {
                    networkManager.StopServer();
                }
                catch (Exception ex)
                {
                    Logger.Error("Unable to stop a Login listener", ex);
                }
            }

            NetworkManagers.Clear();
        }

        private static bool TryGetPortOverride(string[] args, out bool hasPortOverride, out int port)
        {
            hasPortOverride = false;
            port = 0;

            int portArgIndex = Array.FindIndex(args, argument => argument == "--port");
            if (portArgIndex == -1)
            {
                return true;
            }

            if (args.Length <= portArgIndex + 1 ||
                !int.TryParse(args[portArgIndex + 1], out port))
            {
                Logger.Error("The --port option requires a numeric Login port.");
                return false;
            }

            hasPortOverride = true;
            return true;
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Log.Error("Crash", (Exception)e.ExceptionObject);
            Logger.Debug("Login Server crashed! Rebooting gracefully...");
            Process.Start("NosGm.Login.exe", _restartArguments);
            Environment.Exit(1);
        }
    }
}
