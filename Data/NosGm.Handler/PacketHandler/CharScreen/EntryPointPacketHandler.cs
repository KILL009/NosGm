using NosGm.Packets.Packets.ClientPackets;

using NosGm.Configuration;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Service;
using NosGm.Handler.Packets.CharScreenPackets;
using NosGm.Master.Library.Client;
using System;
using System.Linq;

namespace NosGm.Handler.BasicPacket.CharScreen
{
    internal class EntryPointPacketHandler : IPacketHandler
    {
        private static readonly object GameforgeAuthSync = new object();
        private static bool _authenticationServiceAuthenticated;

        public EntryPointPacketHandler(ClientSession session)
        {
            Session = session;
        }

        private ClientSession Session { get; }

        public void LoadCharacters(NosGmEntryPointPacket packet)
        {
            string[] loginPacketParts = string.IsNullOrWhiteSpace(packet?.PacketData)
                ? Array.Empty<string>()
                : packet.PacketData.Split(' ');
            bool isCrossServerLogin = false;
            LogEntryStage("ENTRY_PACKET_RECEIVED", $"Fields={loginPacketParts.Length}");

            if (Session.Account == null)
            {
                if (loginPacketParts.Length <= 3)
                {
                    RejectEntry("MALFORMED_ENTRY_PACKET");
                    return;
                }

                AccountDTO account;
                bool hasCrossServerMarker = string.Equals(loginPacketParts[3], "DAC", StringComparison.Ordinal);
                if (hasCrossServerMarker)
                {
                    if (loginPacketParts.Length <= 8 || !string.Equals(loginPacketParts[8], "CrossServerAuthenticate", StringComparison.Ordinal))
                    {
                        RejectEntry("MALFORMED_CROSS_SERVER_ENTRY");
                        return;
                    }
                    isCrossServerLogin = true;
                    account = LoadAccountByProtocolName(loginPacketParts[4]);
                }
                else
                {
                    if (loginPacketParts.Length <= 7)
                    {
                        RejectEntry("INCOMPLETE_ENTRY_PACKET");
                        return;
                    }
                    account = LoadAccountByProtocolName(loginPacketParts[3]);
                }

                if (account == null)
                {
                    RejectEntry("ACCOUNT_NOT_FOUND");
                    return;
                }
                LogEntryStage("ACCOUNT_RESOLVED", $"CrossServer={isCrossServerLogin}");

                bool hasRegisteredAccountLogin;
                try
                {
                    hasRegisteredAccountLogin = isCrossServerLogin
                        ? CommunicationServiceClient.Instance.IsCrossServerLoginPermitted(account.AccountId, Session.SessionId)
                        : CommunicationServiceClient.Instance.IsLoginPermitted(account.AccountId, Session.SessionId);
                }
                catch (Exception ex)
                {
                    RejectEntry("LOGIN_PERMISSION_CHECK_FAILED", ex);
                    return;
                }

                if (!hasRegisteredAccountLogin)
                {
                    RejectEntry("LOGIN_NOT_PERMITTED");
                    return;
                }
                LogEntryStage("LOGIN_PERMISSION_ACCEPTED", $"CrossServer={isCrossServerLogin}");

                bool isGameforgePasswordlessLogin = !isCrossServerLogin && string.Equals(loginPacketParts[7], "thisisgfmode", StringComparison.Ordinal);
                if (isGameforgePasswordlessLogin)
                {
                    LogEntryStage("GAMEFORGE_MODE_DETECTED");
                    if (!ServerConfiguration.EnableGameforgeTokenLogin || !EnsureAuthenticationServiceAuthenticated())
                    {
                        RejectEntry("GAMEFORGE_AUTH_SERVICE_UNAVAILABLE");
                        return;
                    }

                    bool permitValid;
                    try
                    {
                        permitValid = AuthentificationServiceClient.Instance.ConsumeGameforgeWorldPermit(account.AccountId, Session.SessionId, NormalizeRemoteIp(Session.IpAddress));
                    }
                    catch (Exception ex)
                    {
                        RejectEntry("GAMEFORGE_WORLD_PERMIT_CHECK_FAILED", ex);
                        return;
                    }

                    if (!permitValid)
                    {
                        RejectEntry("GAMEFORGE_WORLD_PERMIT_INVALID");
                        return;
                    }
                    LogEntryStage("GAMEFORGE_WORLD_PERMIT_ACCEPTED");
                }

                bool passwordValid = isCrossServerLogin || isGameforgePasswordlessLogin ||
                                      PasswordHashService.VerifyPassword(account.Password, loginPacketParts[7], true, out _);
                if (!passwordValid)
                {
                    RejectEntry("PASSWORD_REJECTED");
                    return;
                }
                LogEntryStage(
                    "CREDENTIALS_ACCEPTED",
                    $"Mode={(isCrossServerLogin ? "CrossServer" : isGameforgePasswordlessLogin ? "Gameforge" : "Password")}");

                Session.InitializeAccount(
                    new Account(account),
                    isCrossServerLogin,
                    isGameforgePasswordlessLogin);
                ServerManager.Instance.CharacterScreenSessions[Session.Account.AccountId] = Session;
                LogEntryStage("ACCOUNT_INITIALIZED", $"CrossServer={isCrossServerLogin}");
            }

            if (isCrossServerLogin)
            {
                if (!byte.TryParse(loginPacketParts[6], out byte slot))
                {
                    RejectEntry("INVALID_CROSS_SERVER_SLOT");
                    return;
                }
                new SelectCharacterPacketHandler(Session).SelectCharacter(new SelectPacket { Slot = slot });
                LogEntryStage("CROSS_SERVER_CHARACTER_SELECTED");
            }
            else
            {
                var characters = DAOFactory.CharacterDAO.LoadByAccount(Session.Account.AccountId).ToList();
                LogEntryStage("CHARACTER_LIST_LOADING", $"Characters={characters.Count}");
                Session.SendPacket("clist_start 0");

                foreach (CharacterDTO character in characters)
                {
                    var inventory = DAOFactory.ItemInstanceDAO.LoadByType(character.CharacterId, InventoryType.Wear);
                    ItemInstance[] equipment = new ItemInstance[17];
                    foreach (ItemInstanceDTO equipmentEntry in inventory)
                    {
                        ItemInstance currentInstance = new ItemInstance(equipmentEntry);
                        if (currentInstance != null)
                        {
                            equipment[(short)currentInstance.Item.EquipmentSlot] = currentInstance;
                        }
                    }

                    string petlist = "";
                    var mates = DAOFactory.MateDAO.LoadByCharacterId(character.CharacterId).ToList();
                    for (int i = 0; i < 26; i++)
                    {
                        petlist += (i != 0 ? "." : "") + (mates.Count > i ? $"{mates[i].Skin}.{mates[i].NpcMonsterVNum}" : "-1");
                    }

                    Session.SendPacket($"clist {character.Slot} {character.Name} 0 {(byte)character.Gender} {(byte)character.HairStyle} {(byte)character.HairColor} 0 {(byte)character.Class} {character.Level} {character.HeroLevel} {equipment[(byte)EquipmentType.Hat]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.Armor]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.WeaponSkin]?.ItemVNum ?? (equipment[(byte)EquipmentType.MainWeapon]?.ItemVNum ?? -1)}.{equipment[(byte)EquipmentType.SecondaryWeapon]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.Mask]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.Fairy]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.CostumeSuit]?.ItemVNum ?? -1}.{equipment[(byte)EquipmentType.CostumeHat]?.ItemVNum ?? -1} {character.JobLevel}  1 1 {petlist} {(equipment[(byte)EquipmentType.Hat]?.Item.IsColored == true ? equipment[(byte)EquipmentType.Hat].Design : 0)} 0");
                }
                Session.SendPacket("clist_end");
                LogEntryStage("CHARACTER_LIST_SENT", $"Characters={characters.Count}");
            }
        }

        private void LogEntryStage(string stage, string detail = null)
        {
            string suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : " " + detail;
            Logger.Info($"[WORLD_ENTRY] Stage={stage} ClientId={Session.ClientId}{suffix}");
        }

        private void RejectEntry(string code, Exception exception = null)
        {
            Logger.Warn(
                $"[WORLD_ENTRY] Stage=REJECTED Code={code} ClientId={Session.ClientId}{FormatExceptionDetail(exception)}");
            Session.Disconnect();
        }

        private static string FormatExceptionDetail(Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            if (exception is Grpc.Core.RpcException rpcException)
            {
                string innerDetail = rpcException.InnerException == null
                    ? string.Empty
                    : $" InnerType={rpcException.InnerException.GetType().Name} InnerMessage={SanitizeLogValue(rpcException.InnerException.Message)}";
                return
                    $" ExceptionType=RpcException StatusCode={rpcException.StatusCode} Detail={SanitizeLogValue(rpcException.Status.Detail)}{innerDetail}";
            }

            return
                $" ExceptionType={exception.GetType().Name} Message={SanitizeLogValue(exception.Message)}";
        }

        private static string SanitizeLogValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            string normalized = value
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace('\t', ' ')
                .Trim();
            const int maximumLength = 320;
            return normalized.Length <= maximumLength
                ? normalized
                : normalized.Substring(0, maximumLength) + "...";
        }

        private static bool EnsureAuthenticationServiceAuthenticated()
        {
            if (_authenticationServiceAuthenticated) return true;
            lock (GameforgeAuthSync)
            {
                if (_authenticationServiceAuthenticated) return true;
                try
                {
                    _authenticationServiceAuthenticated = AuthentificationServiceClient.Instance.Authenticate(ServerConfiguration.AuthServiceKey);
                }
                catch (Exception ex)
                {
                    Logger.Error("Could not authenticate World against the authentication service", ex);
                    _authenticationServiceAuthenticated = false;
                }
                return _authenticationServiceAuthenticated;
            }
        }

        private static string NormalizeRemoteIp(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) return string.Empty;
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri uri) && !string.IsNullOrWhiteSpace(uri.Host)) return uri.Host;
            string value = endpoint.Trim();
            if (value.StartsWith("[", StringComparison.Ordinal))
            {
                int closingBracket = value.IndexOf(']');
                if (closingBracket > 1) return value.Substring(1, closingBracket - 1);
            }
            int lastColon = value.LastIndexOf(':');
            if (lastColon > 0 && value.IndexOf(':') == lastColon) return value.Substring(0, lastColon);
            return value;
        }

        private static AccountDTO LoadAccountByProtocolName(string protocolUsername)
        {
            AccountDTO account = DAOFactory.AccountDAO.LoadByName(protocolUsername);
            if (account != null) return account;
            if (!ClientRegionMap.TryStripProtocolPrefix(protocolUsername, out string accountName, out _)) return null;
            return DAOFactory.AccountDAO.LoadByName(accountName);
        }
    }
}
