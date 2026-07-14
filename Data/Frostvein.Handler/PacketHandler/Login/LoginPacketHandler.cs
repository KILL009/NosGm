using Frostvein.Configuration;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.DAL.EF;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Packets.Packets.CommandPackets;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Frostvein.Handler.BasicPacket.Login
{
    public class LoginPacketHandler : IPacketHandler
    {
        #region Members

        private readonly ClientSession _session;

        #endregion

        #region Instantiation

        public LoginPacketHandler(ClientSession session)
        {
            _session = session;
        }

        #endregion

        #region Methods

        private async Task<string> BuildServersPacketAsync(string username, byte regionType, int sessionId, bool ignoreUserName, long AccountID)
        {
            var channelpacket =
                CommunicationServiceClient.Instance.RetrieveRegisteredWorldServers(username, regionType, sessionId,
                    ignoreUserName, AccountID);
            

            if (channelpacket == null || !channelpacket.Contains(':'))
            {
                await Task.Run(() => Logger.Debug("Client has been removed. Reason: World Server not found"));
                _session.SendPacket($"failc {(byte)LoginFailType.CantConnect}");
            }

            return channelpacket;
        }

        public async Task VerifyLoginAsync(LoginPacket loginPacket)
        {
            if (loginPacket == null || loginPacket.Name == null || loginPacket.Password == null)
            {
                _session.PacketHandlerInterval?.Dispose();
                return;
            }


            UserDTO user = new UserDTO
            {
                Name = loginPacket.Name,
                Password = ServerConfiguration.UseOldCrypto
                    ? CryptographyBase.Sha512(LoginCryptography.GetPassword(loginPacket.Password)).ToUpper()
                    : loginPacket.Password
            };



            if (user == null || user.Name == null || user.Password == null)
            {
                _session.PacketHandlerInterval?.Dispose();
                return;
            }
            AccountDTO loadedAccount = DAOFactory.AccountDAO.LoadByName(user.Name);
            CharacterDTO characterDTO = new CharacterDTO();
            if (loadedAccount != null && loadedAccount.Name != user.Name)
            {
                _session.SendPacket($"failc {(byte)LoginFailType.WrongCaps}");
                Logger.Info("Session removed. Reason: Wrong Data");
                _session.PacketHandlerInterval?.Dispose();
                return;
            }

            if(ServerConfiguration.MaintenanceMode &&  (AuthorityType.GM > loadedAccount.Authority))
            {
                _session.SendPacket($"failc {(byte)LoginFailType.Maintenance}");
                _session.PacketHandlerInterval?.Dispose();
                return;
            }


            if (loadedAccount?.Password.ToUpper().Equals(user.Password) == true)
            {
                string ipAddress = _session.IpAddress;

                var version = ServerConfiguration.GameVersion;

                if (ServerConfiguration.GameVersionRequired)
                {
                    if (version != ServerConfiguration.GameVersion)
                    {
                        Logger.Log.Warn($"Client version: {loginPacket.ClientData}");
                        Logger.Log.Warn($"Required version: {version}");
                        _session.SendPacket($"failc {(byte)LoginFailType.OldClient}");
                        _session.PacketHandlerInterval?.Dispose();
                        return;
                    }
                }

                if (DAOFactory.PenaltyLogDAO.LoadByIp(ipAddress).Count() > 0)
                {
                    _session.SendPacket($"failc {(byte)LoginFailType.CantConnect}");
                    Logger.Info("Session removed. Reason: Cant connect");
                    _session.PacketHandlerInterval?.Dispose();
                    return;
                }

                if (CheckIsConnected(loadedAccount.AccountId))
                {
                    _session.SendPacket($"failc {(byte)LoginFailType.AlreadyConnected}");
                    Logger.Info("Session removed. Reason: Already connected");
                    _session.PacketHandlerInterval?.Dispose();
                    return;
                }

                //check if the account is connected
                if (!CommunicationServiceClient.Instance.IsAccountConnected(loadedAccount.AccountId))
                {
                    AuthorityType type = loadedAccount.Authority;
                    PenaltyLogDTO penalty = DAOFactory.PenaltyLogDAO.LoadByAccount(loadedAccount.AccountId)
                        .FirstOrDefault(s => s.DateEnd > DateTime.Now && s.Penalty == PenaltyType.Banned);
                    if (penalty != null)
                    {
                        _session.SendPacket($"failc {(byte)LoginFailType.Banned}");
                        Logger.Info("Session removed. Reason: Banned");
                        _session.PacketHandlerInterval?.Dispose();
                    }
                    else
                    {
                        switch (type)
                        {

                            case AuthorityType.Banned:
                                {
                                    _session.SendPacket($"failc {(byte)LoginFailType.Banned}");
                                    Logger.Info("Session removed. Reason: Banned");
                                    _session.PacketHandlerInterval?.Dispose();
                                }
                                break;

                            default:
                                {
                                    Logger.Info($"ClientData: {loginPacket.ClientData}");
                                    Logger.Info($"RegionType: {loginPacket.RegionType}");
                                  ;

                                    int newSessionId = SessionFactory.Instance.GenerateSessionId();
                                    Logger.Info($"{user.Name} connected | SessionID: {newSessionId}");
                                    try
                                    {
                                        Logger.Info($"REGISTER LOGIN");
                                        Logger.Info($"Account={loadedAccount.Name}");
                                        Logger.Info($"Session={newSessionId}");
                                        ipAddress = ipAddress.Substring(6, ipAddress.LastIndexOf(':') - 6);
                                        CommunicationServiceClient.Instance.RegisterAccountLogin(loadedAccount.AccountId, newSessionId, ipAddress);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("General Error SessionId: " + newSessionId, ex);
                                    }

                                    // That's wrong anymore
                                    var clientData = loginPacket.ClientData.Split('.');


                                    // crypto check
                                    //byte regionType = 0;
                                    //if (clientData.Length < 2)
                                    //{
                                    //    clientData = loginPacket.ClientDataOld.Split('.');
                                    //}
                                    //else
                                    //{
                                    //    regionType = byte.Parse(clientData[0].Split('\v')[0]);
                                    //}

                                    var ignoreUserName = short.TryParse(clientData[3], out var clientVersion)
                                                         && (clientVersion < 3075
                                                             || ConfigurationManager.AppSettings["UseOldCrypto"] == "true");
                                    _session.SendPacket(await BuildServersPacketAsync(user.Name, loginPacket.RegionType, newSessionId, ignoreUserName,
                                        loadedAccount.AccountId));
                                    Logger.Info(await BuildServersPacketAsync(
    user.Name,
    loginPacket.RegionType,
    newSessionId,
    ignoreUserName,
    loadedAccount.AccountId));
                                    Logger.Info($"RegionType = {loginPacket.RegionType}");
                                    _session.PacketHandlerInterval?.Dispose();
                                }
                                break;
                        }
                    }
                }
                else
                {
                    _session.SendPacket($"failc {(byte)LoginFailType.AlreadyConnected}");
                    if (!_session.HasSelectedCharacter)
                    {
                        _session.Disconnect();
                        CommunicationServiceClient.Instance.DisconnectAccount(loadedAccount.AccountId);
                        Logger.Info("Session removed. Reason: Already connected");
                        _session.PacketHandlerInterval?.Dispose();
                    }
                }
            }
            else
            {
                _session.SendPacket($"failc {(byte)LoginFailType.AccountOrPasswordWrong}");
                Logger.Info("Session removed. Reason: Wrong Data");
                _session.PacketHandlerInterval?.Dispose();
            }
        }

        private bool CheckIsConnected(long accId)
        {
            for (int i = 0; i < 20; i++)
            {
                if (CommunicationServiceClient.Instance.IsAccountConnected(accId))
                {
                    Task.Delay(200);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
