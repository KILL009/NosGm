using log4net;
using NosGm.Configuration;
using NosGm.Core;
using NosGm.DAL;
using NosGm.DAL.EF.Helpers;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.SCS.Communication.ScsServices.Service;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace NosGm.Master.Server
{
    internal static class Program
    {
        #region Members

        private static readonly ManualResetEvent _run = new ManualResetEvent(true);

        private static bool _isDebug;

        #endregion

        #region Methods

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
            string separator = new string('=', Console.WindowWidth);
            string logo = text.Split('\n').Select(s => string.Format("{0," + (Console.WindowWidth / 2 + s.Length / 2) + "}\n", s)).Aggregate("", (current, i) => current + i);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(separator + logo + separator);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Main(string[] args)
        {
            try
            {
                PrintHeader();
                // initialize Logger
                Logger.InitializeLogger(LogManager.GetLogger(typeof(Program)));

                int port = Convert.ToInt32(ServerConfiguration.MasterServerPort);

                // initialize DB
                if (!DataAccessHelper.Initialize())
                {
                    Console.ReadLine();
                    return;
                }

                Logger.Info("Master Server Config has been loaded");
               
                try
                {
                    // configure Services and Service Host
                    string ipAddress = ServerConfiguration.IPAddress;
                    var _server = ScsServiceBuilder.CreateService(new ScsTcpEndPoint(ipAddress, port));

                    _server.AddService<ICommunicationService, CommunicationService>(new CommunicationService());
                    _server.AddService<IConfigurationService, ConfigurationService>(new ConfigurationService());
                    _server.AddService<IMailService, MailService>(new MailService());
                    _server.AddService<IMallService, MallService>(new MallService());
                    _server.AddService<IAuthentificationService, AuthentificationService>(
                        new AuthentificationService());
                    _server.ClientConnected += OnClientConnected;
                    _server.ClientDisconnected += OnClientDisconnected;

                    _server.Start();
                    static void PrintHeader()
                    {
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
                    Console.Clear();
                    PrintHeader();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[{DateTime.Now}][INFO] Master Server started successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error("General Error Server", ex);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("General Error", ex);
                Console.ReadKey();
            }
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

        private static void OnClientDisconnected(object sender, ServiceClientEventArgs e) => Logger.Info($"ClientID: {e.Client.ClientId} disconnected");
        #endregion
    }
}