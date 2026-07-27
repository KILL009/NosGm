using NosGm.Core;
using NosGm.Core.Networking.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.Core.Networking.Communication.Scs.Server;
using NosGm.Domain;
using NosGm.GameObject.Networking;
using NosGm.GameObject.NosGm.Thread.System;
using System;
using System.Collections.Generic;
using System.Linq;
using NosGm.LogServer.MongoDB;
using NosGm.Configuration;

namespace NosGm.GameObject
{
    public class NetworkManager<EncryptorT> : SessionManager where EncryptorT : CryptographyBase
    {
        #region Instantiation

        public NetworkManager(string ipAddress, int port, Type packetHandler, Type fallbackEncryptor,
            bool isWorldServer) : base(packetHandler, isWorldServer)
        {
            _listeningPort = port;
            _encryptor = (EncryptorT)Activator.CreateInstance(typeof(EncryptorT));

            if (fallbackEncryptor != null)
                _fallbackEncryptor = (CryptographyBase)Activator.CreateInstance(fallbackEncryptor);

            _server = ScsServerFactory.CreateServer(new ScsTcpEndPoint(ipAddress, port));

            // Register events of the server to be informed about clients
            _server.ClientConnected += OnServerClientConnected;
            _server.ClientDisconnected += OnServerClientDisconnected;
            _server.WireProtocolFactory = new WireProtocolFactory<EncryptorT>();

            // Start the server
            _server.Start();

            if (!isWorldServer)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                if (ClientRegionMap.TryResolveLoginPort(port, out byte regionType, out string culture))
                {
                    Console.WriteLine(
                        $"[{DateTime.Now}][INFO] Login listener started | Port={port} RegionType={regionType} Culture={culture}");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}][WARN] Login listener started on unsupported Port={port}");
                }

                Console.ForegroundColor = ConsoleColor.Green;
            }

            if (port == 5100)
            {
                Console.Title += " | Glacernon";
                Console.ForegroundColor = ConsoleColor.Blue;
                PlayerCountThread.PlayerCount = 0;
                Console.Title = $"NosGm - World Server [Channel {ServerManager.Instance.ChannelId} | Players online: {PlayerCountThread.PlayerCount}]";
                Console.WriteLine($"[{DateTime.Now}][INFO] World Server started successfully");
            }

            if (isWorldServer && port != 5100)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                PlayerCountThread.PlayerCount = 0;
                Console.Title = $"NosGm - World Server [Channel {ServerManager.Instance.ChannelId} | Players online: {PlayerCountThread.PlayerCount}]";
                Console.WriteLine($"[{DateTime.Now}][INFO] World Server started successfully");
            }
        }

        #endregion

        #region Properties

        private IDictionary<string, DateTime> ConnectionLog =>
            _connectionLog ?? (_connectionLog = new Dictionary<string, DateTime>());

        #endregion

        #region Members

        private readonly EncryptorT _encryptor;

        private readonly CryptographyBase _fallbackEncryptor;

        private readonly int _listeningPort;

        private readonly IScsServer _server;

        private IDictionary<string, DateTime> _connectionLog;

        #endregion

        #region Methods

        public override void StopServer()
        {
            _server.Stop();
            _server.ClientConnected -= OnServerClientConnected;
            _server.ClientDisconnected -= OnServerClientDisconnected;
        }

        protected override ClientSession IntializeNewSession(INetworkClient client)
        {
            if (!CheckGeneralLog(client))
            {
                client.Initialize(_fallbackEncryptor);
                client.SendPacket($"failc {LoginFailType.CantConnect}");
                Logger.Info($"{client.ClientId} has been removed. Reason: LoginFail | Cant connect");
                client.Disconnect();
                return null;
            }

            var session = new ClientSession(client, _listeningPort);
            session.Initialize(_encryptor, _packetHandler, IsWorldServer);

            return session;
        }

        private bool CheckGeneralLog(INetworkClient client)
        {
            if (!client.IpAddress.Contains("127.0.0.1") /*&& ServerManager.Instance.ChannelId != 51*/)
            {
                if (ConnectionLog.Count > 0)
                {
                    foreach (KeyValuePair<string, DateTime> item in ConnectionLog.Where(cl => cl.Key.Contains(client.IpAddress.Split(':')[1]) && (DateTime.UtcNow - cl.Value).TotalSeconds > 2).ToList())
                    {
                        ConnectionLog.Remove(item.Key);
                    }
                }

                if (ConnectionLog.Any(c => c.Key.Contains(client.IpAddress.Split(':')[1])))
                {
                    return false;
                }

                ConnectionLog.Add(client.IpAddress, DateTime.UtcNow);
                return true;
            }

            return true;
        }

        private void OnServerClientConnected(object sender, ServerClientEventArgs e)
        {
            AddSession(e.Client as NetworkClient);
        }

        private void OnServerClientDisconnected(object sender, ServerClientEventArgs e) => RemoveSession(e.Client as NetworkClient);

        #endregion
    }
}
