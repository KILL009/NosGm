using log4net;
using NosGm.Configuration;
using NosGm.Core;
using NosGm.DAL.EF.Helpers;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.SCS.Communication.ScsServices.Service;
using System;
using System.Linq;

namespace NosGm.Master.Server
{
    internal static class Program
    {
        private static LauncherAuthBridge _launcherAuthBridge;

        private static void PrintHeader()
        {
            Console.Title = "NosGm - Master Server";
            const string text = @"

 ______ _____   ____   _____ _________      ________ _____ _   _ 
|  ____|  __ \ / __ \ / ____|__   __\ \    / /  ____|_   _| \ | |
| |__  | |__) | |  | | (___    | |   \ \  / /| |__    | | |  \| |
|  __| |  _  /| |  | |\___ \   | |    \ \/ / |  __|   | | | . ` |
| |    | | \ \| |__| |____) |  | |     \  /  | |____ _| |_| |\  |
|_|    |_|  \_\\____/|_____/   |_|      \/   |______|_____|_| \_|
                                                                                             
";
            string separator = new string('=', Math.Max(1, Console.WindowWidth));
            string logo = text.Split('\n')
                .Select(line => string.Format("{0," + (Console.WindowWidth / 2 + line.Length / 2) + "}\n", line))
                .Aggregate("", (current, line) => current + line);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(separator + logo + separator);
        }

        public static void Main(string[] args)
        {
            try
            {
                PrintHeader();
                Logger.InitializeLogger(LogManager.GetLogger(typeof(Program)));

                int port = Convert.ToInt32(ServerConfiguration.MasterServerPort);
                if (!DataAccessHelper.Initialize())
                {
                    Console.ReadLine();
                    return;
                }

                Logger.Info("Master Server Config has been loaded");
                StartCommunicationCallbackMirror();

                string ipAddress = ServerConfiguration.IPAddress;
                var server = ScsServiceBuilder.CreateService(new ScsTcpEndPoint(ipAddress, port));
                server.AddService<ICommunicationService, CommunicationService>(new CommunicationService());
                server.AddService<IConfigurationService, ConfigurationService>(new ConfigurationService());
                server.AddService<IMailService, MailService>(new MailService());
                server.AddService<IMallService, MallService>(new MallService());
                server.AddService<IAuthentificationService, AuthentificationService>(new AuthentificationService());
                server.ClientConnected += OnClientConnected;
                server.ClientDisconnected += OnClientDisconnected;
                server.Start();

                StartLauncherAuthBridge();
                AppDomain.CurrentDomain.ProcessExit += (_, __) => StopInfrastructure();
                Console.CancelKeyPress += (_, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    StopInfrastructure();
                    Environment.Exit(0);
                };

                Console.Clear();
                PrintHeader();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"[{DateTime.Now}][INFO] Master Server started successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("General Error", ex);
                StopInfrastructure();
                Console.ReadKey();
            }
        }

        private static void StartCommunicationCallbackMirror()
        {
            MasterCommunicationCallbackMirror.Instance.Start();
        }

        private static void StartLauncherAuthBridge()
        {
            if (!ServerConfiguration.EnableLauncherAuthBridge)
            {
                return;
            }

            if (!ServerConfiguration.EnableGameforgeTokenLogin)
            {
                throw new InvalidOperationException(
                    "EnableGameforgeTokenLogin must be true before the launcher authentication bridge can start.");
            }

            _launcherAuthBridge = new LauncherAuthBridge();
            _launcherAuthBridge.Start();
        }

        private static void StopInfrastructure()
        {
            MasterCommunicationCallbackMirror.Instance.Stop();
            StopLauncherAuthBridge();
        }

        private static void StopLauncherAuthBridge()
        {
            _launcherAuthBridge?.Dispose();
            _launcherAuthBridge = null;
        }

        private static void OnClientConnected(object sender, ServiceClientEventArgs e)
        {
            if (e.Client.ClientId == 1)
            {
                Console.WriteLine("[CONNECT] World Server successfully connected");
            }

            if (e.Client.ClientId == 2)
            {
                Console.WriteLine("[CONNECT] Login Server successfully connected");
            }
        }

        private static void OnClientDisconnected(object sender, ServiceClientEventArgs e)
        {
            Logger.Info($"ClientID: {e.Client.ClientId} disconnected");
        }
    }
}
