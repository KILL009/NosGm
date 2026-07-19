using Autofac;
using ChickenAPI.Events;
using ChickenAPI.Plugins;
using ChickenAPI.Plugins.Exceptions;
using ChickenAPI.Plugins.Modules;
using Game.Configuration;
using log4net;
using Frostvein.Configuration;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Packets.Packets.ServerPackets;
using Frostvein.Core;
using Frostvein.DAL.EF.Helpers;
using Frostvein.GameObject;
using Frostvein.GameObject._BCards;
using Frostvein.GameObject._Event;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject.Characters.Events;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Frostvein.Thread.System;
using Frostvein.Handler.PacketHandler.Command;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using Plugins.BasicImplementations.Algorithm;
using Plugins.BasicImplementations.Event;
using Plugins.BasicImplementations.Guri;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Frostvein.Domain;

namespace Frostvein.World
{
    public static class Program
    {
        #region Delegates

        public delegate bool EventHandler(CtrlType sig);

        #endregion

        #region Enums

        public enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        #endregion

        #region Classes

        public static class NativeMethods
        {
            #region Methods

            [DllImport("Kernel32")]
            internal static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

            #endregion
        }

        #endregion

        #region Members

        private static readonly ManualResetEvent _run = new ManualResetEvent(true);

        private static EventHandler _exitHandler;

        private static bool _ignoreTelemetry;
        private static bool _isDebug = false;
        private static int _port;
        private static DiscordGmBridge _discordGmBridge;

        #endregion

        #region Methods

      
        private static void PrintHeader()
        {
            Console.Title = $"Frostvein - World Server";
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

        public static async Task Main(string[] args)
        {

            // initialize Logger
            Logger.InitializeLogger(LogManager.GetLogger(typeof(Program)));

            var coreContainer = BuildCoreContainer();
            var gameBuilder = new ContainerBuilder();
            gameBuilder.RegisterInstance(coreContainer).As<IContainer>();
            gameBuilder.RegisterModule(new CoreContainerModule(coreContainer));
            //gameBuilder.Register(s => new GamePacketHandlersGamePlugin(coreContainer.Resolve<IPacketHandlerContainer<IGamePacketHandler>>(), coreContainer)).As<IGamePlugin>();
            //gameBuilder.Register(s => new CharScreenPacketHandlerGamePlugin(coreContainer.Resolve<IPacketHandlerContainer<ICharacterScreenPacketHandler>>(), coreContainer)).As<IGamePlugin>();
            //gameBuilder.Register(s => new CommandHandler(new SerilogLogger(), coreContainer)).As<ICommandContainer>().SingleInstance();
            //gameBuilder.Register(s => new CommandGlobalExecutorWrapper(s.Resolve<ICommandContainer>())).As<IGlobalCommandExecutor>().SingleInstance();
            //gameBuilder.Register(s => new EssentialsPlugin(s.Resolve<ICommandContainer>(), ServerManager.Instance)).As<IGamePlugin>().SingleInstance();
            gameBuilder.Register(s => new BasicEventPipelineAsync()).As<IEventPipeline>().SingleInstance();
            gameBuilder.Register(s => new GenericEventPlugin(s.Resolve<IEventPipeline>(), coreContainer)).As<IGamePlugin>().SingleInstance();
            gameBuilder.Register(s => new GuriPlugin(coreContainer.Resolve<IGuriHandlerContainer>(), coreContainer)).As<IGamePlugin>();
            var gameContainer = gameBuilder.Build();
            var plugins = gameContainer.Resolve<IEnumerable<IGamePlugin>>();
            if (plugins != null)
            {
                foreach (var gamePlugin in plugins)
                {
                    gamePlugin.OnEnable();
                }
            }

            PrintHeader();

            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var ignoreStartupMessages = false;
            _port = Convert.ToInt32(WorldPolicyConfiguration.WorldPort); /*GlacernonServerPort dann solte act 4 auch gehen */
            var portArgIndex = Array.FindIndex(args, s => s == "--port");
            if (portArgIndex != -1
                && args.Length >= portArgIndex + 1
                && int.TryParse(args[portArgIndex + 1], out _port))
            {
                Console.WriteLine("Port override: " + _port);
            }

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "--nomsg":
                        ignoreStartupMessages = true;
                        break;

                    case "--notelemetry":
                        _ignoreTelemetry = true;
                        break;
                }
            }

            // initialize api
            string authKey = ServerConfiguration.MasterAuthKey;
            if (CommunicationServiceClient.Instance.Authenticate(authKey))
            {
                Logger.Info("Master Server API Communication has been initialized");
            }

            // initialize DB
            if (!DataAccessHelper.Initialize())
            {
                Console.ReadKey();
                return;
            }

            EventEntity.InitializeEventPipeline(gameContainer.Resolve<IEventPipeline>());

            // initialilize maps
            EventServiceHandler.Initialize();

            // TODO: initialize ClientLinkManager initialize PacketSerialization
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

            PluginFacility.InitializeAll();
            AuditSpecialistData();

            try
            {
                AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
                _exitHandler += ExitHandler;
                NativeMethods.SetConsoleCtrlHandler(_exitHandler, true);
            }
            catch (Exception ex)
            {
                Logger.Error("General Error", ex);
            }

            NetworkManager<WorldCryptography> networkManager = null;

            string ipAddress = ServerConfiguration.IPAddress;

        portloop:
            try
            {
                networkManager = new NetworkManager<WorldCryptography>(ipAddress, _port, typeof(Act4Handler), typeof(LoginCryptography), true);
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10048)
                {
                    _port++;
                    Logger.Info("Port already in use! Incrementing...");
                    goto portloop;
                }

                Logger.Error("General Error", ex);
                Environment.Exit(ex.ErrorCode);
            }

            ServerManager.Instance.ServerGroup = WorldPolicyConfiguration.ServerGroup;
            Logger.Info($"[WORLD_POLICY] Group={WorldPolicyConfiguration.ServerGroup} Mode={WorldPolicyConfiguration.WorldMode} " +
                        $"NormalEXP={!WorldPolicyConfiguration.DisableNormalExperience} " +
                        $"HeroEXP={!WorldPolicyConfiguration.DisableHeroExperience} Port={_port}");
            int sessionLimit = ServerConfiguration.SessionLimit;
            var newChannelId = CommunicationServiceClient.Instance.RegisterWorldServer(new SerializableWorldServer(ServerManager.Instance.WorldId, ipAddress, _port, sessionLimit,ServerManager.Instance.ServerGroup));
            if (newChannelId.HasValue)
            {
                ServerManager.Instance.ChannelId = newChannelId.Value;
                MailServiceClient.Instance.Authenticate(authKey, ServerManager.Instance.WorldId);
                ConfigurationServiceClient.Instance.Authenticate(authKey, ServerManager.Instance.WorldId);
                ServerManager.Instance.Configuration = ConfigurationServiceClient.Instance.GetConfigurationObject();
                ServerManager.Instance.MallApi = new MallAPIHelper(ServerConfiguration.MallBaseURL);
                ServerManager.Instance.SynchronizeSheduling();
                _discordGmBridge = DiscordGmBridge.StartFromEnvironment();
                AntiSpamModule.Instance.RunBlacklistTask();
                if (ServerConfiguration.StartGlacernonAutomaticly)
                {
                    if (!ServerManager.Instance.IsAct4Online())
                    {
                        Process.Start("Frostvein.World.exe", $"--nomsg --port 5100");
                    }
                }
                if (ServerConfiguration.StartAllChannelsAutomaticly)
                {
                   
                }
            }
            else
            {
                Logger.Log.Error("Could not retrieve ChannelId from Web API.", new Exception());
                Console.ReadKey();
            }


          

            Console.Title = $"Frostvein - World Server [Channel {ServerManager.Instance.ChannelId} | {PlayerCountThread.PlayerCount} Players online]";
        }



        private static void AuditSpecialistData()
        {
            var specialistCards = ServerManager.Items
                .Where(item => item != null &&
                               item.EquipmentSlot == EquipmentType.Sp &&
                               item.Morph > 0)
                .ToList();

            var specialistMorphs = new HashSet<int>(
                specialistCards.Select(item => (int)item.Morph));

            var specialistSkills = ServerManager.GetAllSkill()
                .Where(skill => skill != null &&
                                specialistMorphs.Contains(skill.UpgradeType) &&
                                skill.SkillType == (byte)SkillType.CharacterSKill)
                .ToList();

            var morphsWithoutSkills = specialistMorphs
                .Where(morph => !specialistSkills.Any(skill => skill.UpgradeType == morph))
                .OrderBy(morph => morph)
                .ToList();

            var usedBCardTypes = specialistSkills
                .SelectMany(skill => skill.BCards ?? new List<BCard>())
                .Concat(specialistCards.SelectMany(item => item.BCards ?? new List<BCard>()))
                .Select(bcard => (BCardType.CardType)bcard.Type)
                .Distinct()
                .OrderBy(type => (byte)type)
                .ToList();

            var missingBCardTypes = usedBCardTypes
                .Where(type => !PluginFacility.HasBCardHandler(type))
                .ToList();

            short[] plus20BuffIds = { 942, 943, 944, 945, 946 };
            var missingPlus20BuffIds = plus20BuffIds
                .Where(cardId => ServerManager.GetCard(cardId) == null)
                .ToList();

            Logger.Info($"[SP_AUDIT] Cards={specialistCards.Count} Morphs={specialistMorphs.Count} " +
                        $"Skills={specialistSkills.Count} BCardTypes={usedBCardTypes.Count} " +
                        $"RegisteredHandlers={PluginFacility.RegisteredBCardTypes.Count}");

            if (morphsWithoutSkills.Count > 0)
            {
                Logger.Warn($"[SP_AUDIT] Specialist morphs without skills: {string.Join(",", morphsWithoutSkills)}");
            }

            if (missingBCardTypes.Count > 0)
            {
                Logger.Warn("[SP_AUDIT] Specialist BCard types without runtime handlers: " +
                            string.Join(", ", missingBCardTypes.Select(type => $"{(byte)type}:{type}")));
            }

            if (missingPlus20BuffIds.Count > 0)
            {
                Logger.Warn("[SP_AUDIT] Missing +20 elemental buff cards: " +
                            string.Join(",", missingPlus20BuffIds));
            }
        }

        private static IContainer BuildCoreContainer()
        {
            var pluginBuilder = new ContainerBuilder();
            /*pluginBuilder.RegisterType<SerilogLogger>().AsImplementedInterfaces();
            pluginBuilder.RegisterType<DatabasePlugin>().AsImplementedInterfaces().AsSelf();
            pluginBuilder.RegisterType<RedisMultilanguagePlugin>().AsImplementedInterfaces().AsSelf();
            pluginBuilder.RegisterType<GamePacketHandlersCorePlugin>().AsImplementedInterfaces().AsSelf();
            pluginBuilder.RegisterType<CharScreenPacketHandlerCorePlugin>().AsImplementedInterfaces().AsSelf();*/
            pluginBuilder.RegisterType<GenericEventPluginCore>().AsImplementedInterfaces().AsSelf();
            pluginBuilder.RegisterType<GuriPluginCore>().AsImplementedInterfaces().AsSelf();
            pluginBuilder.RegisterType<AlgorithmPluginCore>().AsImplementedInterfaces().AsSelf();
            var container = pluginBuilder.Build();

            var coreBuilder = new ContainerBuilder();
            foreach (var plugin in container.Resolve<IEnumerable<ICorePlugin>>())
            {
                try
                {
                    plugin.OnLoad(coreBuilder);
                }
                catch (PluginException e)
                {
                }
            }

            return coreBuilder.Build();
        }

        /// <summary>
        ///     List of all connected clients.
        /// </summary>
        private static readonly ThreadSafeSortedList<long, ClientSession> _sessions;

        public static IEnumerable<ClientSession> Sessions =>
            _sessions.Where(s => s.HasSelectedCharacter && !s.IsDisposing && s.IsConnected);

        private static bool ExitHandler(CtrlType sig)
        {
            foreach (var clients in ServerManager.Instance.Sessions)
            {
                MessageExtension.SendModal(clients, GameConfiguration.CrashReportMessage);
                clients.Character.Event.EmitEvent(new CharacterSaveEvent());
            }
            if (ServerManager.Instance.Sessions.Count() != 0)
            {
                ServerManager.Instance.SaveAll();
            }
            //Call Bazaar to remove outdated things

            //TODO: Review Unregister process since it mainly caused the crash
            CommunicationServiceClient.Instance.UnregisterWorldServer(ServerManager.Instance.WorldId);
            _discordGmBridge?.Dispose();
            Thread.Sleep(30000);
            return false;
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Process.Start("Frostvein.World.exe", $"--nomsg --port {_port}");
            ServerManager.Instance.InShutdown = true;
            if (e == null)
            {
                return;
            }
            foreach (var clients in ServerManager.Instance.Sessions)
            {
                MessageExtension.SendModal(clients, GameConfiguration.CrashReportMessage);
                clients.Character.Event.EmitEvent(new CharacterSaveEvent());
            }
            Exception exception = (Exception)e.ExceptionObject;
            LoggerService.LogServer.Logger.LogAsync($"World Server {ServerManager.Instance.WorldId} has been shut down. Reason: " + exception, LogType.ERROR);
            LoggerService.LogServer.Logger.LogAsync(exception.ToString() + " | " + exception.StackTrace, LogType.ERROR);
            CommunicationServiceClient.Instance.UnregisterWorldServer(ServerManager.Instance.WorldId);
            ServerManager.Instance.SaveAll();
            Environment.Exit(1);
        }

        #endregion
    }
}
