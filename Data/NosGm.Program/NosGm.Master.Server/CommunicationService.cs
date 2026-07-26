using NosGm.Configuration;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Data;
using NosGm.Master.Library.Interface;
using NosGm.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using NosGm.SCS.Communication.ScsServices.Service;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NosGm.Master.Server
{
    internal class CommunicationService : ScsService, ICommunicationService
    {
        private ClientSession Session { get; }

        public List<Fake> fakes;

        public CommunicationService()
        {
            fakes = new List<Fake>();
        }

        private const int NsTeSTPadding = 56;

        #region Methods

        public bool Authenticate(string authKey)
        {
            if (string.IsNullOrWhiteSpace(authKey)) return false;

            if (authKey == ServerConfiguration.MasterAuthKey)
            {
                MSManager.Instance.AuthentificatedClients.Add(CurrentClient.ClientId);
                return true;
            }

            return false;
        }

        public bool IsCharacterSaving(long characterId)
        {
            return false;
        }

        public void AddOrRemoveSavingCharacters(long characterId, bool add)
        {
            // if (add)
            // {
            //     MSManager.Instance.CharactersUnderSaveProcess[characterId] = DateTime.Now;
            // }
            // else if (!add)
            // {
            //     MSManager.Instance.CharactersUnderSaveProcess.Remove(characterId);
            // }
        }

        public void CheckForStuckAccountsAtSaving()
        {
            List<long> remove = new List<long>();
            foreach (var entry in MSManager.Instance.CharactersUnderSaveProcess)
            {
                if (entry.Value.AddMinutes(1) <= DateTime.Now)
                {
                    remove.Add(entry.Key);
                }
            }

            foreach (var key in remove)
            {
                MSManager.Instance.CharactersUnderSaveProcess.Remove(key);
            }
        }

        public void Cleanup()
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;

            MSManager.Instance.ConnectedAccounts.Clear();
            MSManager.Instance.WorldServers.Clear();
        }

        public void CleanupOutdatedSession()
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;

            var tmp = new AccountConnection[MSManager.Instance.ConnectedAccounts.Count + 20];
            lock (MSManager.Instance.ConnectedAccounts)
            {
                MSManager.Instance.ConnectedAccounts.CopyTo(tmp);
            }

            foreach (var account in tmp.Where(a => a?.LastPulse.AddMinutes(5) <= DateTime.Now))
                KickSession(account.AccountId, null);
        }

        public bool ConnectAccount(Guid worldId, long accountId, int sessionId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return false;

            var account =
                MSManager.Instance.ConnectedAccounts.Find(a =>
                    a.AccountId.Equals(accountId) && a.SessionId.Equals(sessionId));
            if (account != null)
                account.ConnectedWorld = MSManager.Instance.WorldServers.Find(w => w.Id.Equals(worldId));
            return account.ConnectedWorld != null;
        }

        public List<(int, int)> GetPorts()
        {
            return MSManager.Instance.WorldServers.Select(x => (x.ChannelId, x.Endpoint.TcpPort)).ToList();
        }

        public bool ConnectAccountCrossServer(Guid worldId, long accountId, int sessionId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return false;
            var account = MSManager.Instance.ConnectedAccounts
                .Where(a => a.AccountId.Equals(accountId) && a.SessionId.Equals(sessionId)).FirstOrDefault();
            if (account != null)
            {
                account.CanLoginCrossServer = false;
                account.OriginWorld = account.ConnectedWorld;
                account.ConnectedWorld = MSManager.Instance.WorldServers.Find(s => s.Id.Equals(worldId));
                if (account.ConnectedWorld != null) return true;
            }

            return false;
        }

        public bool ConnectCharacter(Guid worldId, long characterId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return false;

            //Multiple WorldGroups not yet supported by DAOFactory
            var accountId = DAOFactory.CharacterDAO.LoadById(characterId)?.AccountId ?? 0;

            var account = MSManager.Instance.ConnectedAccounts.Find(a =>
                a.AccountId.Equals(accountId) && a.ConnectedWorld.Id.Equals(worldId));

            if (account != null)
            {
                account.CharacterId = characterId;
                foreach (var world in MSManager.Instance.WorldServers.Where(w =>
                    w.WorldGroup.Equals(account.ConnectedWorld.WorldGroup)))
                    world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>()
                        .CharacterConnected(characterId);
                return true;
            }

            return false;
        }

        public void DisconnectAccount(long accountId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;
            if (MSManager.Instance.ConnectedAccounts.Any(s => s.AccountId.Equals(accountId) && s.CanLoginCrossServer))
            {
            }
            else
            {
                MSManager.Instance.ConnectedAccounts.RemoveAll(c => c.AccountId.Equals(accountId));
            }
        }

        public void DisconnectCharacter(Guid worldId, long characterId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;

            foreach (var account in MSManager.Instance.ConnectedAccounts.Where(c =>
                c.CharacterId.Equals(characterId) && c.ConnectedWorld.Id.Equals(worldId)))
            {
                foreach (var world in MSManager.Instance.WorldServers.Where(w =>
                    w.WorldGroup.Equals(account.ConnectedWorld.WorldGroup)))
                    world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>()
                        .CharacterDisconnected(characterId);
                if (!account.CanLoginCrossServer)
                {
                    account.CharacterId = 0;
                    account.ConnectedWorld = null;
                }
            }
        }

        public int? GetChannelIdByWorldId(Guid worldId)
        {
            return MSManager.Instance.WorldServers.Find(w => w.Id == worldId)?.ChannelId;
        }

        public int GetChannelStat(long accountid)
        {
            AccountConnection temp = MSManager.Instance.ConnectedAccounts.Where(x => x.AccountId == accountid).First();
            var sessions =
                            MSManager.Instance.ConnectedAccounts.CountLinq(a =>
                                a.ConnectedWorld?.ChannelId == temp.ConnectedWorld.ChannelId == true);
            if (fakes.Any(x => x.ChannelId == temp.ConnectedWorld.ChannelId))
            {
                sessions += fakes.Where(x => x.ChannelId == temp.ConnectedWorld.ChannelId).First().Amount;
            }
            return sessions;
        }

        public long[][] GetOnlineCharacters()
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return null;

            var connections = MSManager.Instance.ConnectedAccounts.Where(s => s.CharacterId != 0);

            var result = new long[connections.Count][];

            var i = 0;
            foreach (var acc in connections)
            {
                result[i] = new long[3];
                result[i][0] = acc.CharacterId;
                result[i][1] = acc.ConnectedWorld?.ChannelId ?? 0;
                result[i][2] = acc.SessionId;
                i++;
            }

            return result;
        }

        public int GetServerStat()
        {
            int amount = 0;
            try
            {
                var groups = new List<string>();
                foreach (var s in MSManager.Instance.WorldServers.Select(s => s.WorldGroup))
                    if (!groups.Contains(s))
                        groups.Add(s);
                var totalsessions = 0;
                foreach (var message in groups)
                {
                    //result.Add($"==={message}===");
                    var groupsessions = 0;
                    foreach (var world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(message)))
                    {
                        var sessions =
                            MSManager.Instance.ConnectedAccounts.CountLinq(a =>
                                a.ConnectedWorld?.Id.Equals(world.Id) == true);
                        if (fakes.Any(x => x.ChannelId == world.ChannelId))
                        {
                            sessions += fakes.Where(x => x.ChannelId == world.ChannelId).First().Amount;
                        }
                        groupsessions += sessions;
                    }
                    totalsessions += groupsessions;
                }
                amount = totalsessions;
            }
            catch (Exception ex)
            {
            }
            return amount;
        }

        public bool IsAccountConnected(long accountId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return false;

            return MSManager.Instance.ConnectedAccounts.Any(c =>
                c.AccountId == accountId && c.ConnectedWorld != null);
        }

        public bool IsAct4Online()
        {
            return MSManager.Instance.WorldServers.Any(w => w.Endpoint.IpAddress ==
                                                            ServerConfiguration.IPAddress &&
                                                            w.Endpoint.TcpPort == Convert.ToInt32(ServerConfiguration.GlacernonServerPort));
        }

        public bool IsChannel1Online(string worldGroup)
        {
            string port = "1337";
            return MSManager.Instance.WorldServers.Any(w => w.WorldGroup.Equals(worldGroup)
                                                           && w.Endpoint.IpAddress ==
                                                           ServerConfiguration.IPAddress &&
                                                           w.Endpoint.TcpPort == Convert.ToInt32(port));
        }

        public bool IsChannel2Online(string worldGroup)
        {
            string port = "1338";
            return MSManager.Instance.WorldServers.Any(w => w.WorldGroup.Equals(worldGroup)
                                                           && w.Endpoint.IpAddress ==
                                                           ServerConfiguration.IPAddress &&
                                                           w.Endpoint.TcpPort == Convert.ToInt32(port));
        }

        public bool IsChannel3Online(string worldGroup)
        {
            string port = "1339";
            return MSManager.Instance.WorldServers.Any(w => w.WorldGroup.Equals(worldGroup)
                                                           && w.Endpoint.IpAddress ==
                                                           ServerConfiguration.IPAddress &&
                                                           w.Endpoint.TcpPort == Convert.ToInt32(port));
        }

        public bool IsChannel4Online(string worldGroup)
        {
            string port = "1340";
            return MSManager.Instance.WorldServers.Any(w => w.WorldGroup.Equals(worldGroup)
                                                           && w.Endpoint.IpAddress ==
                                                           ServerConfiguration.IPAddress &&
                                                           w.Endpoint.TcpPort == Convert.ToInt32(port));
        }

        public bool IsChannel5Online(string worldGroup)
        {
            string port = "1341";
            return MSManager.Instance.WorldServers.Any(w => w.WorldGroup.Equals(worldGroup)
                                                           && w.Endpoint.IpAddress ==
                                                           ServerConfiguration.IPAddress &&
                                                           w.Endpoint.TcpPort == Convert.ToInt32(port));
        }

        public bool IsChannel6Online(string worldGroup)
        {
            string port = "1342";
            return MSManager.Instance.WorldServers.Any(w => w.WorldGroup.Equals(worldGroup)
                                                           && w.Endpoint.IpAddress ==
                                                           ServerConfiguration.IPAddress &&
                                                           w.Endpoint.TcpPort == Convert.ToInt32(port));
        }

        public bool IsChannel7Online(string worldGroup)
        {
            string port = "1343";
            return MSManager.Instance.WorldServers.Any(w => w.WorldGroup.Equals(worldGroup)
                                                           && w.Endpoint.IpAddress ==
                                                           ServerConfiguration.IPAddress &&
                                                           w.Endpoint.TcpPort == Convert.ToInt32(port));
        }

        public bool IsCharacterConnected(string worldGroup, long characterId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return false;

            return MSManager.Instance.ConnectedAccounts.Any(c =>
                c.ConnectedWorld != null && c.ConnectedWorld.WorldGroup == worldGroup && c.CharacterId == characterId);
        }

        public bool IsCrossServerLoginPermitted(long accountId, int sessionId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return false;

            return MSManager.Instance.ConnectedAccounts.Any(s =>
                s.AccountId.Equals(accountId) && s.SessionId.Equals(sessionId) && s.CanLoginCrossServer);
        }

        public bool IsLoginPermitted(long accountId, int sessionId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return false;

            return MSManager.Instance.ConnectedAccounts.Any(s =>
                s.AccountId.Equals(accountId) && s.SessionId.Equals(sessionId) && s.ConnectedWorld == null);
        }

        public void KickSession(long? accountId, int? sessionId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;

            foreach (var world in MSManager.Instance.WorldServers)
                world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>()
                    .KickSession(accountId, sessionId);
            if (accountId.HasValue)
                MSManager.Instance.ConnectedAccounts.RemoveAll(s => s.AccountId.Equals(accountId.Value));
            else if (sessionId.HasValue)
                MSManager.Instance.ConnectedAccounts.RemoveAll(s => s.SessionId.Equals(sessionId.Value));
        }

        public void PulseAccount(long accountId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;
            var account = MSManager.Instance.ConnectedAccounts.Find(a => a.AccountId.Equals(accountId));
            if (account != null) account.LastPulse = DateTime.Now;
        }

        public void RefreshPenalty(int penaltyId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;

            foreach (var world in MSManager.Instance.WorldServers)
                world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().UpdatePenaltyLog(penaltyId);
            foreach (var login in MSManager.Instance.LoginServers)
                login.GetClientProxy<ICommunicationClient>().UpdatePenaltyLog(penaltyId);
        }

        public void RegisterAccountLogin(long accountId, int sessionId, string ipAddress)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;
            MSManager.Instance.ConnectedAccounts.RemoveAll(a => a.AccountId.Equals(accountId));
            MSManager.Instance.ConnectedAccounts.Add(new AccountConnection(accountId, sessionId, ipAddress));
        }

        public void RegisterCrossServerAccountLogin(long accountId, int sessionId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;

            var account = MSManager.Instance.ConnectedAccounts
                .Where(a => a.AccountId.Equals(accountId) && a.SessionId.Equals(sessionId)).FirstOrDefault();

            if (account != null) account.CanLoginCrossServer = true;
        }

        public int? RegisterWorldServer(SerializableWorldServer worldServer)
        {

            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return null;
            var ws = new WorldServer(worldServer.Id,
                new ScsTcpEndPoint(worldServer.EndPointIP, worldServer.EndPointPort), worldServer.AccountLimit,
                worldServer.WorldGroup)

            {
                CommunicationServiceClient = CurrentClient,
                ChannelId = Enumerable.Range(1, 30).Except(MSManager.Instance.WorldServers
                    .Where(w => w.WorldGroup.Equals(worldServer.WorldGroup)).OrderBy(w => w.ChannelId)
                    .Select(w => w.ChannelId)).First()
            };
            if (worldServer.EndPointPort == Convert.ToInt32(ServerConfiguration.GlacernonServerPort)) ws.ChannelId = 51;
            MSManager.Instance.WorldServers.Add(ws);
            return ws.ChannelId;
           
        }

        public void Restart(string worldGroup, int time = 5)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;

            if (worldGroup == "*")
                foreach (var world in MSManager.Instance.WorldServers)
                    world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().Restart(time);
            else
                foreach (var world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(worldGroup)))
                    world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().Restart(time);
        }

        public long[][] RetrieveOnlineCharacters(long characterId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return null;

            var connections = MSManager.Instance.ConnectedAccounts.Where(s =>
                s.IpAddress == MSManager.Instance.ConnectedAccounts.Find(f => f.CharacterId == characterId)
                    ?.IpAddress && s.CharacterId != 0);

            var result = new long[connections.Count][];

            var i = 0;
            foreach (var acc in connections)
            {
                result[i] = new long[2];
                result[i][0] = acc.CharacterId;
                result[i][1] = acc.ConnectedWorld?.ChannelId ?? 0;
                i++;
            }

            return result;
        }

        public string RetrieveOriginWorld(long accountId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return null;

            var account = MSManager.Instance.ConnectedAccounts.Find(s => s.AccountId.Equals(accountId));
            if (account?.OriginWorld != null)
                return $"{account.OriginWorld.Endpoint.IpAddress}:{account.OriginWorld.Endpoint.TcpPort}";
            return null;
        }

        public string RetrieveRegisteredWorldServers(string username, byte regionType, int sessionId, bool ignoreUserName,
            long AccountId)
        {
           
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId)))
            {
                return null;
            }

            var visibleWorlds = MSManager.Instance.WorldServers
                .Where(w => w.ChannelId != 51)
                .OrderBy(w => w.WorldGroup)
                .ThenBy(w => w.ChannelId)
                .ToList();
            if (visibleWorlds.Count == 0)
            {
                return null;
            }

            var characters = DAOFactory.CharacterDAO.LoadByAccount(AccountId);

            var lastGroup = "";
            byte worldCount = 0;
            var International = "-99 0 -99 0 -99 0 -99 0 ";
            byte i = 0;
            foreach (var character in characters) i++;
            switch (i)
            {
                case 1:
                    International = "1 1 -99 0 -99 0 -99 0 ";
                    break;

                case 2:
                    International = "1 1 -99 1 -99 0 -99 0 ";
                    break;

                case 3:
                    International = "1 1 -99 1 -99 1 -99 0 ";
                    break;

                case 4:
                    International = "1 1 -99 1 -99 1 -99 1 ";
                    break;
            }
            // Cliente 0.9.3.3254
            // El padding del paquete NsTeST debe ser 56 pares "-99 0".
            // Cambiar este valor desplaza el SessionId y rompe
            // la selección del servidor.
         

        var OtherSv = string.Concat(
            Enumerable.Repeat("-99 0 ", NsTeSTPadding));


        //to access to chnanel
        //var channelPacket = $"NsTeST {regionType} {username} {International}{OtherSv}{sessionId} ";
        var channelPacket =
    $"NsTeST {regionType} {username} {International}{OtherSv}{sessionId} ";


            foreach (var world in visibleWorlds)
            {
if (lastGroup != world.WorldGroup)
                {
                    worldCount++;
                }

                lastGroup = world.WorldGroup;

                var currentlyConnectedAccounts =
                    MSManager.Instance.ConnectedAccounts.CountLinq(a => a.ConnectedWorld?.ChannelId == world.ChannelId);
                var channelcolor = (int)Math.Round((double)currentlyConnectedAccounts / world.AccountLimit * 20) + 1;

channelPacket +=
                    $"{world.Endpoint.IpAddress}:{world.Endpoint.TcpPort}:{channelcolor}:{worldCount}.{world.ChannelId}.{world.WorldGroup} ";
            }

            channelPacket += "-1:-1:-1:10000.10000.1";
            Logger.Info(
                $"World list generated | RegionType={regionType} Groups={worldCount} Channels={visibleWorlds.Count}");
            return channelPacket;
        }

        public IEnumerable<string> RetrieveServerStatistics(bool isStart)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return null;

            var result = new List<string>();

            try
            {
                var groups = new List<string>();
                foreach (var s in MSManager.Instance.WorldServers.Select(s => s.WorldGroup))
                    if (!groups.Contains(s))
                        groups.Add(s);
                var totalsessions = 0;
                foreach (var message in groups)
                {
                    //result.Add($"==={message}===");
                    var groupsessions = 0;
                    foreach (var world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(message)))
                    {
                        var sessions =
                            MSManager.Instance.ConnectedAccounts.CountLinq(a =>
                                a.ConnectedWorld?.Id.Equals(world.Id) == true);
                        if (fakes.Any(x => x.ChannelId == world.ChannelId))
                        {
                            sessions += fakes.Where(x => x.ChannelId == world.ChannelId).First().Amount;
                        }
                        if (!isStart) result.Add($"Channel {world.ChannelId}: {sessions} Sessions");
                        groupsessions += sessions;
                    }

                    if (!isStart) result.Add($"Group Total: {groupsessions} Sessions");
                    totalsessions += groupsessions;
                }

                //result.Add(isStart
                //    ? $"[Players counter]  {totalsessions} players online."
                //    : $"Players online: {totalsessions} active sessions");
            }
            catch (Exception ex)
            {
                Logger.LogEventError("RETRIEVE_EXCEPTION", "Error while retreiving server Statistics:", ex);
            }

            return result;
        }

        public void RunGlobalEvent(EventType eventType, byte value)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;

            foreach (var world in MSManager.Instance.WorldServers)
                world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>()
                    .RunGlobalEvent(eventType, value);
        }

        public int? SendMessageToCharacter(SCSCharacterMessage message)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return null;

            var sourceWorld = MSManager.Instance.WorldServers.Find(s => s.Id.Equals(message.SourceWorldId));
            if (message == null || message.Message == null) return null;

            switch (message.Type)
            {
                case MessageType.Family:
                case MessageType.FamilyChat:
                case MessageType.Broadcast:
                    if (sourceWorld == null) return null;
                    foreach (var world in MSManager.Instance.WorldServers.Where(w =>
                        w.WorldGroup.Equals(sourceWorld.WorldGroup)))
                        world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>()
                            .SendMessageToCharacter(message);
                    return -1;

                case MessageType.PrivateChat:
                    if (sourceWorld == null) return null;
                    if (message.DestinationCharacterId.HasValue)
                    {
                        var receiverAccount = MSManager.Instance.ConnectedAccounts.Find(a =>
                            a.CharacterId.Equals(message.DestinationCharacterId.Value));
                        if (receiverAccount?.ConnectedWorld != null)
                        {
                            if (sourceWorld.ChannelId == 51 && receiverAccount.ConnectedWorld.ChannelId == 51
                                                            && DAOFactory.CharacterDAO
                                                                .LoadById(message.SourceCharacterId).Faction
                                                            != DAOFactory.CharacterDAO
                                                                .LoadById((long)message.DestinationCharacterId)
                                                                .Faction)
                            {
                                var SenderAccount = MSManager.Instance.ConnectedAccounts.Find(a => a.CharacterId.Equals(message.SourceCharacterId));
                                message.Message = $"talk {message.DestinationCharacterId} " + Language.Instance.GetMessageFromKey("CANT_TALK_OPPOSITE_FACTION");
                                message.DestinationCharacterId = message.SourceCharacterId;
                                SenderAccount.ConnectedWorld.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().SendMessageToCharacter(message);
                                return -1;
                            }
                            receiverAccount.ConnectedWorld.CommunicationServiceClient
                                .GetClientProxy<ICommunicationClient>().SendMessageToCharacter(message);
                            return receiverAccount.ConnectedWorld.ChannelId;
                        }
                    }

                    break;

                case MessageType.Whisper:
                    if (sourceWorld == null) return null;
                    if (message.DestinationCharacterId.HasValue)
                    {
                        var receiverAccount = MSManager.Instance.ConnectedAccounts.Find(a =>
                            a.CharacterId.Equals(message.DestinationCharacterId.Value));
                        if (receiverAccount?.ConnectedWorld != null)
                        {
                            if (sourceWorld.ChannelId == 51 && receiverAccount.ConnectedWorld.ChannelId == 51
                                                            && DAOFactory.CharacterDAO
                                                                .LoadById(message.SourceCharacterId).Faction
                                                            != DAOFactory.CharacterDAO
                                                                .LoadById((long)message.DestinationCharacterId)
                                                                .Faction)
                            {
                                var SenderAccount = MSManager.Instance.ConnectedAccounts.Find(a =>
                                    a.CharacterId.Equals(message.SourceCharacterId));
                                message.Message =
                                    $"say 1 {message.SourceCharacterId} 11 {Language.Instance.GetMessageFromKey("CANT_TALK_OPPOSITE_FACTION")}";
                                message.DestinationCharacterId = message.SourceCharacterId;
                                message.Type = MessageType.Other;
                                receiverAccount.ConnectedWorld.CommunicationServiceClient
                                    .GetClientProxy<ICommunicationClient>().SendMessageToCharacter(message);
                                return -1;
                            }

                            receiverAccount.ConnectedWorld.CommunicationServiceClient
                                .GetClientProxy<ICommunicationClient>().SendMessageToCharacter(message);
                            return receiverAccount.ConnectedWorld.ChannelId;
                        }
                    }

                    break;

                case MessageType.WhisperSupport:
                case MessageType.WhisperGM:
                    if (message.DestinationCharacterId.HasValue)
                    {
                        var account = MSManager.Instance.ConnectedAccounts.Find(a =>
                            a.CharacterId.Equals(message.DestinationCharacterId.Value));
                        if (account?.ConnectedWorld != null)
                        {
                            account.ConnectedWorld.CommunicationServiceClient.GetClientProxy<ICommunicationClient>()
                                .SendMessageToCharacter(message);
                            return account.ConnectedWorld.ChannelId;
                        }
                    }

                    break;

                case MessageType.Shout:
                    foreach (var world in MSManager.Instance.WorldServers)
                        world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>()
                            .SendMessageToCharacter(message);
                    return -1;

                case MessageType.Other:
                    if (sourceWorld == null) return null;
                    var receiverAcc = MSManager.Instance.ConnectedAccounts.Find(a =>
                        a.CharacterId.Equals(message.DestinationCharacterId.Value));
                    if (receiverAcc?.ConnectedWorld != null)
                    {
                        receiverAcc.ConnectedWorld.CommunicationServiceClient.GetClientProxy<ICommunicationClient>()
                            .SendMessageToCharacter(message);
                        return receiverAcc.ConnectedWorld.ChannelId;
                    }

                    break;
            }

            return null;
        }


        public void Shutdown(string worldGroup)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;

            if (worldGroup == "*")
                foreach (var world in MSManager.Instance.WorldServers)
                    world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().Shutdown();
            else
                foreach (var world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(worldGroup)))
                    world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().Shutdown();
        }

        public void UnregisterWorldServer(Guid worldId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;
            if (worldId == null)
            {
                return;
            }
            MSManager.Instance.ConnectedAccounts.RemoveAll(a => a?.ConnectedWorld?.Id.Equals(worldId) == true);
            MSManager.Instance.WorldServers.RemoveAll(w => w.Id.Equals(worldId));
        }

        public void UpdateBazaar(string worldGroup, long bazaarItemId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;

            foreach (var world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(worldGroup)))
                world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().UpdateBazaar(bazaarItemId);
        }

        public void UpdateFamily(string worldGroup, long familyId, bool changeFaction)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;

            foreach (var world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(worldGroup)))
                world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>()
                    .UpdateFamily(familyId, changeFaction);
        }

        public void UpdateRelation(string worldGroup, long relationId)
        {
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId))) return;

            foreach (var world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(worldGroup)))
                world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().UpdateRelation(relationId);
        }

        public struct Fake
        {
            #region Members

            public int ChannelId, Amount;

            #endregion
        }

        #endregion
    }
}